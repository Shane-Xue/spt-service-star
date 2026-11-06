using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EFT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ServiceStars
{
    // Separate component: animation continues while the synchronization task waits.
    internal sealed class PromotionOverlay : MonoBehaviour
    {
        private sealed class Promotion { internal string Weapon; internal string Template; internal long Stars; }
        private Promotion current;
        private string displayedLocale;
        private readonly Queue<Promotion> queue = new Queue<Promotion>();
        private CanvasGroup group;
        private RectTransform panel;
        private RectTransform medal;
        private TextMeshProUGUI weaponText, titleText, countText;
        private Texture2D texture;
        private Sprite sprite;
        private AudioSource audioSource;
        private AudioClip chime;
        private float started = -1f;
        private float duration;
        internal float HoldSeconds = 3.2f;
        internal float Volume = 0.18f;
        internal float VerticalPosition = 0.67f;

        internal void Enqueue(string weapon, long stars, string template)
        {
            queue.Enqueue(new Promotion { Weapon = weapon, Stars = stars, Template = template });
        }
        private void Build()
        {
            // No raycaster, no selectable elements and no input capture.
            var go = new GameObject("ServiceStars.Promotion", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            group = go.GetComponent<CanvasGroup>();
            group.alpha = 0;
            group.interactable = false;
            group.blocksRaycasts = false;
            panel = NewRect("Medal and inscription", go.transform, new Vector2(660, 390));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, VerticalPosition);
            panel.anchoredPosition = Vector2.zero;
            medal = NewRect("Bronze service star", panel, new Vector2(200, 200));
            medal.anchoredPosition = new Vector2(0, 65);
            var image = medal.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ServiceStars.service-star.png"))
            {
                if (stream == null) throw new InvalidDataException("Embedded service star artwork is missing.");
                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    if (!ImageConversion.LoadImage(texture, buffer.ToArray(), true)) throw new InvalidDataException("Invalid service star PNG.");
                }
            }
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
            image.sprite = sprite;
            weaponText = NewText("Weapon", -60, 26);
            titleText = NewText("Service Star", -102, 34);
            countText = NewText("Star count", -143, 20);
            countText.color = new Color(0.84f, 0.76f, 0.57f);
            var rule = NewRect("Inscription rule", panel, new Vector2(190, 1));
            rule.anchoredPosition = new Vector2(0, -79);
            var line = rule.gameObject.AddComponent<Image>();
            line.raycastTarget = false;
            line.color = new Color(0.85f, 0.81f, 0.68f, 0.45f);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0;
            // Original quiet struck-metal cue; no borrowed Battlefield audio.
            const int rate = 22050;
            var samples = new float[(int)(rate * 0.85f)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float attack = Mathf.Min(1, t / 0.012f);
                samples[i] = attack * (0.42f * Mathf.Sin(2 * Mathf.PI * 660 * t) * Mathf.Exp(-6 * t)
                    + 0.18f * Mathf.Sin(2 * Mathf.PI * 1326 * t) * Mathf.Exp(-9 * t)
                    + 0.07f * Mathf.Sin(2 * Mathf.PI * 2112 * t) * Mathf.Exp(-15 * t));
            }
            chime = AudioClip.Create("ServiceStars.MetalChime", samples.Length, 1, rate, false);
            chime.SetData(samples, 0);
        }
        private static RectTransform NewRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }
        private TextMeshProUGUI NewText(string name, float y, float size)
        {
            var rect = NewRect(name, panel, new Vector2(640, 46));
            rect.anchoredPosition = new Vector2(0, y);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.96f, 0.95f, 0.91f);
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color32(12, 14, 15, 230);
            text.raycastTarget = false;
            text.richText = false;
            text.enableWordWrapping = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.65f;
            text.fontSizeMax = size;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }
        private void Begin(Promotion promotion)
        {
            current = promotion;
            // Reuse a live localized UI font, including its Chinese fallbacks.
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            foreach (var candidate in FindObjectsOfType<TextMeshProUGUI>())
            {
                if (candidate.transform.IsChildOf(panel) || candidate.font == null) continue;
                font = candidate.font;
                if (candidate.text != null && ContainsChinese(candidate.text)) break;
            }
            weaponText.font = titleText.font = countText.font = font;
            RefreshLocale();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, VerticalPosition);
            started = Time.realtimeSinceStartup;
            duration = 0.45f + HoldSeconds + 0.65f;
            group.gameObject.SetActive(true);
            if (Volume > 0) audioSource.PlayOneShot(chime, Volume);
        }
        private void RefreshLocale()
        {
            displayedLocale = ServiceStarsPlugin.CurrentLocale;
            weaponText.text = String.IsNullOrEmpty(current.Template) ? current.Weapon : LocalizationExtensions.Localized(current.Template + " ShortName", null);
            titleText.text = ModLocale.Text(displayedLocale, "promotion");
            countText.text = ModLocale.Text(displayedLocale, "star_count", current.Stars);
        }
        private static bool ContainsChinese(string value)
        {
            foreach (char c in value) if (c >= '\u4e00' && c <= '\u9fff') return true;
            return false;
        }
        private void Update()
        {
            try
            {
                if (started < 0)
                {
                    if (queue.Count == 0) return;
                    if (group == null) Build();
                    Begin(queue.Dequeue());
                }
                float elapsed = Time.realtimeSinceStartup - started;
                if (displayedLocale != ServiceStarsPlugin.CurrentLocale) RefreshLocale();
                if (elapsed >= duration)
                {
                    group.alpha = 0;
                    group.gameObject.SetActive(false);
                    started = -1;
                    return;
                }
                float entrance = Mathf.Clamp01(elapsed / 0.45f);
                float ease = 1 - Mathf.Pow(1 - entrance, 3);
                group.alpha = Mathf.Min(entrance, Mathf.Clamp01((duration - elapsed) / 0.65f));
                medal.localScale = Vector3.one * Mathf.Lerp(1.16f, 1, ease);
                panel.anchoredPosition = new Vector2(0, Mathf.Lerp(-12, 0, ease));
            }
            catch (Exception ex)
            {
                queue.Clear();
                started = -1;
                if (group != null) { group.alpha = 0; Destroy(group.gameObject); }
                Debug.LogError("[ServiceStars] Promotion overlay: " + ex.Message);
            }
        }
        private void OnDestroy()
        {
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
            if (chime != null) Destroy(chime);
            if (audioSource != null) Destroy(audioSource);
        }
    }
}
