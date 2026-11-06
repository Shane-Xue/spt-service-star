using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ServiceStars;
using ServiceStars.Server;
using Newtonsoft.Json;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using SPTarkov.Server.Core.Models.Enums;

internal static class Program
{
    private static int checks;
    private const string Profile = "111111111111111111111111";
    private const string Other = "222222222222222222222222";
    private const string Gun = "333333333333333333333333";
    private const string Gun2 = "444444444444444444444444";
    private static string root = "";
    private static string runtimeRoot = "";
    public static int Main(string[] args)
    {
        string runtime = Path.GetFullPath(args[0]);
        runtimeRoot = runtime;
        AssemblyLoadContext.Default.Resolving += (_, name) => {
            string path = Path.Combine(runtime, name.Name + ".dll");
            if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, name.Name + ".dll");
            if (name.Name == "Newtonsoft.Json") path = Path.Combine(runtime, "..", "EscapeFromTarkov_Data", "Managed", "Newtonsoft.Json.dll");
            return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path)) : null;
        };
        root = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        try { Run().GetAwaiter().GetResult(); Console.WriteLine($"PASS: {checks} assertions; fixtures: {root}"); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Check(bool success, string name)
    {
        if (!success) throw new Exception("FAILED: " + name);
        checks++;
        Console.WriteLine("PASS " + name);
    }
    private static SyncRequest Request(string stream, long count) => new() { StreamId = stream, Counts = new() { [Gun] = count } };
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Run()
    {
        var store = new StarStore(root, 100, id => id == Gun || id == Gun2);
        string a = Guid.NewGuid().ToString("N"), b = Guid.NewGuid().ToString("N");
        var reply = store.Sync(Profile, Request(a, 99));
        Check(reply.Totals[Gun] == 99 && reply.KillsPerStar == 100, "initial counter / threshold");
        reply = store.Sync(Profile, Request(a, 100));
        Check(reply.Totals[Gun] / 100 == 1 && reply.Totals[Gun] % 100 == 0, "100 kills earns one star");
        Check(store.Sync(Profile, Request(a, 100)).Totals[Gun] == 100, "lost response replay is idempotent");
        Check(store.Sync(Profile, Request(a, 80)).Totals[Gun] == 100, "out of order request cannot subtract kills");
        Check(store.Sync(Profile, Request(b, 47)).Totals[Gun] == 147, "second client stream merges");
        var restarted = new StarStore(root, 100, _ => true);
        Check(restarted.Sync(Profile, Request(a, 100)).Totals[Gun] == 147, "server restart retains totals");
        Check(restarted.Sync(Other, Request(a, 2)).Totals[Gun] == 2, "profiles are isolated");
        var two = Request(a, 100); two.Counts[Gun2] = 12;
        reply = store.Sync(Profile, two);
        Check(reply.Totals[Gun] == 147 && reply.Totals[Gun2] == 12, "weapon models are isolated");
        Check(File.Exists(Path.Combine(root, Profile + ".json.bak")), "atomic replace retains previous backup");
        var invalid = Request(a, -1);
        bool rejected = false;
        try { store.Sync(Profile, invalid); } catch (ArgumentException) { rejected = true; }
        Check(rejected && store.Sync(Profile, Request(a, 100)).Totals[Gun] == 147, "negative counters rejected without mutation");
        rejected = false;
        try { store.Sync("../escape", Request(a, 100)); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "profile path traversal rejected");
        rejected = false;
        try { store.Sync(Profile, new SyncRequest { StreamId = "bad" }); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "invalid stream rejected");
        var removed = Request(a, 100); removed.Counts["555555555555555555555555"] = 9;
        reply = store.Sync(Profile, removed);
        Check(!reply.Acknowledged.ContainsKey("555555555555555555555555"), "removed weapon stays pending");
        var client = new ClientData { StreamId = a, Counts = new() { [Gun] = 101 },
            Totals = new() { [Gun] = 147 }, Acknowledged = new() { [Gun] = 100 } };
        Check(Ledger.Total(client, Gun) == 148, "new kill during request remains visible after response");
        client.Counts[Gun] = 102;
        reply = store.Sync(Profile, Request(a, 102));
        client.Totals = reply.Totals; client.Acknowledged = reply.Acknowledged;
        Check(Ledger.Total(client, Gun) == 149, "acknowledged counters are not displayed twice");
        client = JsonConvert.DeserializeObject<ClientData>(JsonConvert.SerializeObject(client))!;
        Check(Ledger.Total(client, Gun) == 149 && client.StreamId == a, "client cache round trip retains stream and counts");
        var json = new JsonUtil(Array.Empty<IJsonConverterRegistrator>());
        string payload = JsonConvert.SerializeObject(new { Protocol = 1, StreamId = a, Counts = new Dictionary<string, long> { [Gun] = 103 } });
        var body = json.Deserialize<StarRequest>(payload)!;
        Check(body.StreamId == a && body.Counts[Gun] == 103, "actual SPT JSON reader accepts client payload and inherited model");
        var router = new TestRouter(json, store);
        string result = (string)(await router.HandleStaticAsync("/service-stars/sync", payload, new MongoId(Profile), null, CancellationToken.None))!;
        var wire = JsonConvert.DeserializeObject<Reply>(result)!;
        Check(wire.Ok && wire.Protocol == 1 && wire.Totals[Gun] == 150 && wire.Acknowledged[Gun] == 103,
            "actual SPT static router dispatch / server JSON / client deserializer");
        var metadata = new Metadata();
        Check(metadata.ModGuid == "com.shane-xue.service-stars" && metadata.Version.ToString() == "1.2.1", "publication GUID and server version");
        Check(metadata.SptVersion.IsSatisfied(new SemanticVersioning.Version("4.1.6")), "server metadata accepts installed version");
        Check(typeof(StarRouter).GetCustomAttributes().Any(t => t.GetType().Name == "Injectable"), "server router is discoverable via DI");
        Parallel.For(104, 124, n => store.Sync(Profile, Request(a, n)));
        Check(store.Sync(Profile, Request(a, 100)).Totals[Gun] == 170, "concurrent updates converge to maximum");
        string corrupt = Path.Combine(root, Other + ".json");
        File.WriteAllText(corrupt, "{broken");
        rejected = false;
        try { store.Sync(Other, Request(a, 3)); } catch (System.Text.Json.JsonException) { rejected = true; }
        Check(rejected && File.ReadAllText(corrupt) == "{broken", "corrupt server data fails without resetting stars");
        // Use the installed game's category hierarchy, including mod-style
        // descendants, rather than a fabricated list of accepted item IDs.
        using var database = JsonDocument.Parse(File.ReadAllText(Path.Combine(runtimeRoot, "SPT_Data", "database", "templates", "items.json")));
        var parents = database.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetProperty("_parent").GetString() ?? "");
        bool IsA(MongoId template, MongoId category)
        {
            string current = template.ToString();
            for (int depth = 0; depth < 64; depth++)
            {
                if (current == category.ToString()) return true;
                if (!parents.TryGetValue(current, out current!)) return false;
            }
            return false;
        }
        var examples = new List<string>();
        foreach (var category in new[] { BaseClasses.ASSAULT_RIFLE, BaseClasses.THROW_WEAP, BaseClasses.KNIFE, BaseClasses.LAUNCHER })
        {
            string example = database.RootElement.EnumerateObject().First(p => p.Value.GetProperty("_type").GetString() == "Item" && IsA(new MongoId(p.Name), category)).Name;
            examples.Add(example);
            Check(WeaponCategories.Accepts(example, IsA), "installed item category accepted: " + category);
        }
        foreach (var category in new[] { BaseClasses.AMMO, BaseClasses.MED_KIT })
        {
            string example = database.RootElement.EnumerateObject().First(p => p.Value.GetProperty("_type").GetString() == "Item" && IsA(new MongoId(p.Name), category)).Name;
            Check(!WeaponCategories.Accepts(example, IsA), "ammo / medicine excluded: " + category);
        }
        const string custom = "666666666666666666666666";
        parents[custom] = BaseClasses.KNIFE.ToString();
        Check(WeaponCategories.Accepts(custom, IsA), "new modded melee template accepted without whitelist");
        var expanded = new StarStore(Path.Combine(root, "expanded"), 100, id => WeaponCategories.Accepts(id, IsA));
        var mixed = new SyncRequest { StreamId = a, Counts = examples.Append(custom).ToDictionary(id => id, _ => 100L) };
        var mixedReply = expanded.Sync(Profile, mixed);
        Check(mixedReply.Acknowledged.Count == 5 && expanded.Sync(Profile, mixed).Totals.Values.All(n => n == 100),
            "firearm / grenade / knife / underbarrel / custom counters save separately and replay safely");
        foreach (string cause in new[] { "Bullet", "Blunt", "Melee", "Explosion", "GrenadeFragment", "Landmine", "LightBleeding", "HeavyBleeding", "Flame", "HotGases", "ThermobaricExplosion", "Poison", "LethalToxin" })
            Check(CombatDamage.IsSupported(cause), "combat damage accepted: " + cause);
        foreach (string cause in new[] { "Fall", "Dehydration", "Exhaustion", "Medicine", "Environment", "Undefined" })
            Check(!CombatDamage.IsSupported(cause), "noncombat cause excluded: " + cause);
        Check(ModLocale.Resolve("ch") == "ch", "EFT Chinese locale resolved");
        Check(ModLocale.Resolve("zh-CN") == "ch" && ModLocale.Resolve("ZH_tw") == "ch", "Chinese locale aliases resolved");
        Check(ModLocale.Text("en", "promotion") == "SERVICE STAR", "English promotion locale");
        Check(ModLocale.Text("ch", "star_count", 3) == "第 3 颗", "Chinese formatted locale");
        Check(ModLocale.Text("fr", "promotion") == "SERVICE STAR", "untranslated locale falls back to English");
        Check(ModLocale.Text(null!, "promotion") == "SERVICE STAR", "locale unavailable during startup falls back to English");
        Check(ModLocale.Text("en", "progress", 3, 347, 47, 100, "").Contains("Next star 47/100"), "localized progress retains values");
    }
    private sealed class TestRouter(JsonUtil json, StarStore store) : StaticRouter(json,
        [new RouteAction<StarRequest>("/service-stars/sync", (url, body, session, output, ct) =>
            ValueTask.FromResult(System.Text.Json.JsonSerializer.Serialize(store.Sync(session.ToString(), body))))]);
}
