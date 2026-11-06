using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ServiceStars
{
    [HarmonyPatch]
    internal static class InspectPatch
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ItemSpecificationPanel), "Show");
            yield return AccessTools.Method(typeof(ItemSpecificationPanel), "OnItemExaminedEvent");
        }
        [HarmonyPostfix]
        private static void Postfix(ItemSpecificationPanel __instance, Item ____item,
            Transform ____charPanelContainer, TextMeshProUGUI ____masteringText, ItemInfoWindowLabels ____itemLabels)
        {
            try
            {
                StarLabel row = __instance.GetComponent<StarLabel>();
                Item weapon = ____item;
                if (row != null) row.Hide();
                if (!WeaponCredit.IsSupported(weapon) || !__instance.Examined || ServiceStarsPlugin.Instance == null) return;
                if (row == null) row = __instance.gameObject.AddComponent<StarLabel>();
                TMP_Text caption = AccessTools.Field(typeof(ItemInfoWindowLabels), "_caption").GetValue(____itemLabels) as TMP_Text;
                row.Show(weapon.StringTemplateId, ____charPanelContainer, caption != null ? caption : ____masteringText);
            }
            catch (Exception ex) { BepInEx.Logging.Logger.CreateLogSource("ServiceStars.UI").LogWarning(ex.Message); }
        }
    }

    internal sealed class StarLabel : MonoBehaviour
    {
        private TextMeshProUGUI text;
        private string template;
        private float refresh;
        internal void Show(string id, Transform parent, TMP_Text style)
        {
            template = id;
            if (text == null)
            {
                var go = new GameObject("ServiceStars", typeof(RectTransform), typeof(LayoutElement));
                go.transform.SetParent(parent, false);
                go.transform.SetSiblingIndex(0);
                text = go.AddComponent<CustomTextMeshProUGUI>();
                if (style != null) { text.font = style.font; text.fontSharedMaterial = style.fontSharedMaterial; }
                text.fontSize = 16;
                text.color = new Color(1f, 0.8f, 0.35f);
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.raycastTarget = false;
                text.enableWordWrapping = false;
                var layout = go.GetComponent<LayoutElement>();
                layout.minHeight = 48;
                layout.preferredHeight = 48;
                layout.flexibleWidth = 1;
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(350, 48);
            }
            text.gameObject.SetActive(true);
            Render();
        }
        internal void Hide() { if (text != null) text.gameObject.SetActive(false); template = null; }
        private void Update()
        {
            if (text != null && text.gameObject.activeSelf && Time.realtimeSinceStartup >= refresh) Render();
        }
        private void Render()
        {
            if (ServiceStarsPlugin.Instance != null && template != null)
                text.text = ServiceStarsPlugin.Instance.Label(template);
            refresh = Time.realtimeSinceStartup + 0.5f;
        }
        private void OnDisable() { Hide(); }
        private void OnDestroy() { if (text != null) Destroy(text.gameObject); }
    }
}
