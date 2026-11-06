using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using EFT.Ballistics;
using EFT.Communications;
using EFT.InventoryLogic;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;
using UnityEngine;

namespace ServiceStars
{
    [BepInPlugin(Id, "Service Stars", "1.2.1")]
    [BepInDependency("com.SPT.core", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class ServiceStarsPlugin : BaseUnityPlugin
    {
        public const string Id = "com.shane-xue.service-stars";
        internal static ServiceStarsPlugin Instance;
        private ClientData data;
        private string cachePath;
        private Task<string> sync;
        private float nextSync;
        private bool dirty;
        private bool connected;
        private object statistics;
        private readonly HashSet<string> victims = new HashSet<string>();
        private readonly Dictionary<string, string> names = new Dictionary<string, string>();
        private ConfigEntry<bool> notifications;
        private ConfigEntry<bool> preview;
        private ConfigEntry<float> holdSeconds, soundVolume, verticalPosition;
        private PromotionOverlay overlay;
        internal static string CurrentLocale
        {
            get { return LocalizationManager.Instance == null ? "en" : LocalizationManager.Instance.Culture; }
        }

        private void Awake()
        {
            Instance = this;
            notifications = Config.Bind("Display", "Promotion notifications", true, "Show a notification when a weapon earns a star.");
            preview = Config.Bind("Promotion", "Preview notification", false, "Toggle on to preview the central star award without changing statistics. 自动复位，不增加击杀。");
            holdSeconds = Config.Bind("Promotion", "Hold seconds", 3.2f, new ConfigDescription("Time to hold the award before fading.", new AcceptableValueRange<float>(1, 8)));
            soundVolume = Config.Bind("Promotion", "Sound volume", 0.18f, new ConfigDescription("Promotion chime volume; 0 mutes it.", new AcceptableValueRange<float>(0, 1)));
            verticalPosition = Config.Bind("Promotion", "Vertical position", 0.67f, new ConfigDescription("Screen height: 0 bottom, 1 top.", new AcceptableValueRange<float>(0.35f, 0.85f)));
            overlay = gameObject.AddComponent<PromotionOverlay>();
            try
            {
                if (String.IsNullOrEmpty(RequestHandler.SessionId) || String.IsNullOrEmpty(RequestHandler.Host))
                    throw new InvalidOperationException("No SPT server/session is available.");
                string identity = RequestHandler.Host.TrimEnd('/').ToLowerInvariant() + "\n" + RequestHandler.SessionId;
                string key;
                using (var sha = SHA256.Create())
                    key = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-", "").ToLowerInvariant();
                string dir = Path.Combine(BepInEx.Paths.ConfigPath, "ServiceStars");
                Directory.CreateDirectory(dir);
                cachePath = Path.Combine(dir, key + ".json");
                data = File.Exists(cachePath) ? JsonConvert.DeserializeObject<ClientData>(File.ReadAllText(cachePath)) : new ClientData();
                Guid stream;
                if (data == null || data.Version != 1 || !Guid.TryParseExact(data.StreamId, "N", out stream) ||
                    data.Counts == null || data.Totals == null || data.Acknowledged == null || data.KillsPerStar < 1 ||
                    data.Counts.Any(p => p.Value < 0)) throw new InvalidDataException("Invalid service stars cache; restore its .bak file.");
                SaveCache();
                new Harmony(Id).PatchAll(typeof(ServiceStarsPlugin).Assembly);
                Logger.LogInfo("Service Stars 1.2.1 loaded. Display language follows the game locale.");
            }
            catch (Exception ex)
            {
                data = null;
                Logger.LogError("Service Stars disabled: " + ex);
            }
        }

        private long Total(string id)
        {
            return Ledger.Total(data, id);
        }
        internal string Label(string id)
        {
            string locale = CurrentLocale;
            if (data == null) return ModLocale.Text(locale, "unavailable");
            long total = Total(id);
            int threshold = data.KillsPerStar;
            string status = connected && Ledger.Count(data.Counts, id) <= Ledger.Count(data.Acknowledged, id)
                ? "" : ModLocale.Text(locale, "pending");
            return ModLocale.Text(locale, "progress", total / threshold, total, total % threshold, threshold, status);
        }

        internal void Record(object manager, Player owner, DamageInfo damage, EDamageType lethalType, string victimId, string group)
        {
            if (data == null || owner == null || !owner.IsYourPlayer || owner.IsAI || owner.Side == EPlayerSide.Savage) return;
            // Use the weapon in the damage record; the weapon currently held may have changed.
            Item weapon = damage.Weapon;
            if (!WeaponCredit.IsSupported(weapon) || String.IsNullOrEmpty(victimId) || victimId == owner.ProfileId) return;
            if (!CombatDamage.IsSupported(lethalType.ToString())) return;
            if (!String.IsNullOrEmpty(group) && group == owner.GroupId) return;
            if (owner.GameWorld == null || owner.GameWorld.GetType().Name.IndexOf("Hideout", StringComparison.OrdinalIgnoreCase) >= 0) return;
            if (!System.Object.ReferenceEquals(statistics, manager))
            {
                statistics = manager;
                victims.Clear();
            }
            if (!victims.Add(victimId)) return;
            string id = weapon.StringTemplateId;
            long before = Total(id);
            data.Counts[id] = checked(Ledger.Count(data.Counts, id) + 1);
            names[id] = weapon.LocalizedShortName();
            dirty = true;
            // Persist before sending. If disk I/O fails, retain memory and retry from Update.
            TrySaveCache();
            if (notifications.Value && before / data.KillsPerStar < Total(id) / data.KillsPerStar)
                Notify(names[id], Total(id) / data.KillsPerStar, id);
            nextSync = Math.Min(nextSync, Time.realtimeSinceStartup + 1f);
        }

        private void Notify(string name, long stars, string template = null)
        {
            try
            {
                overlay.HoldSeconds = holdSeconds.Value;
                overlay.Volume = soundVolume.Value;
                overlay.VerticalPosition = verticalPosition.Value;
                overlay.Enqueue(name, stars, template);
            }
            catch (Exception ex) { Logger.LogWarning("Star notification failed: " + ex.Message); }
        }

        private void Update()
        {
            if (preview.Value)
            {
                preview.Value = false;
                Notify("AK-74M", 3);
            }
            if (data == null) return;
            if (sync != null)
            {
                if (!sync.IsCompleted) return;
                try
                {
                    Reply reply = JsonConvert.DeserializeObject<Reply>(sync.GetAwaiter().GetResult());
                    if (reply == null || !reply.Ok || reply.Protocol != 1 || reply.KillsPerStar < 1 ||
                        reply.Totals == null || reply.Acknowledged == null)
                        throw new InvalidDataException(reply == null ? "Empty service stars response." : reply.Error ?? "Incompatible service stars response.");
                    data.Totals = reply.Totals;
                    data.Acknowledged = reply.Acknowledged;
                    data.KillsPerStar = reply.KillsPerStar;
                    connected = true;
                    dirty = true;
                    TrySaveCache();
                    nextSync = Time.realtimeSinceStartup + 15f;
                }
                catch (Exception ex)
                {
                    if (connected || nextSync == 0) Logger.LogWarning("Service Stars sync unavailable; keeping local counters. " + ex.Message);
                    connected = false;
                    nextSync = Time.realtimeSinceStartup + 30f;
                }
                sync = null;
            }
            if (Time.realtimeSinceStartup < nextSync) return;
            if (dirty && !TrySaveCache()) { nextSync = Time.realtimeSinceStartup + 30f; return; }
            try
            {
                string payload = JsonConvert.SerializeObject(new { Protocol = 1, StreamId = data.StreamId,
                    Counts = new Dictionary<string, long>(data.Counts) });
                sync = RequestHandler.PostJsonAsync("/service-stars/sync", payload);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Cannot start Service Stars sync: " + ex.Message);
                connected = false;
                nextSync = Time.realtimeSinceStartup + 30f;
            }
        }
        private bool TrySaveCache()
        {
            try { SaveCache(); dirty = false; return true; }
            catch (Exception ex) { Logger.LogError("Service Stars cache write failed: " + ex.Message); return false; }
        }
        private void SaveCache()
        {
            string temp = cachePath + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            { fs.Write(bytes, 0, bytes.Length); fs.Flush(true); }
            if (File.Exists(cachePath)) File.Replace(temp, cachePath, cachePath + ".bak");
            else File.Move(temp, cachePath);
        }
        private void OnDestroy()
        {
            if (data != null && dirty) TrySaveCache();
            new Harmony(Id).UnpatchSelf();
            Instance = null;
        }
    }

    [HarmonyPatch(typeof(BaseStatisticsManager), "OnEnemyKill")]
    internal static class KillPatch
    {
        [HarmonyPostfix]
        private static void Postfix(BaseStatisticsManager __instance, Player ___Player,
            DamageInfo __0, EDamageType __1, string __6, string __8)
        {
            try
            {
                if (ServiceStarsPlugin.Instance != null)
                    ServiceStarsPlugin.Instance.Record(__instance, ___Player, __0, __1, __6, __8);
            }
            catch (Exception ex) { BepInEx.Logging.Logger.CreateLogSource("ServiceStars.Kill").LogError(ex); }
        }
    }
}
