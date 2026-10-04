using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace malafein.Valheim.SharedUI
{
    // uGUI building blocks for mod panels, styled through VanillaUI.
    internal static class UIBuilder
    {
        // Unity's default ScrollRect sensitivity (1) moves a list one pixel per wheel notch.
        public const float DefaultScrollSensitivity = 40f;

        public static RectTransform MakeChildRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static TextMeshProUGUI AddText(
            RectTransform rt,
            string text,
            TMP_FontAsset font,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            // TMP's Awake runs synchronously at AddComponent on an active
            // object and hunts for the project-default font (LiberationSans
            // SDF — not shipped with Valheim) before our assignment below
            // lands, logging a warning per element. Add the component on an
            // inactive object so Awake defers until the font is set.
            GameObject go = rt.gameObject;
            bool wasActive = go.activeSelf;
            if (wasActive) go.SetActive(false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = alignment;
            tmp.color = UIPalette.BodyTextColor;
            tmp.fontSize = fontSize;
            if (font != null) tmp.font = font;
            // Give body text the vanilla crisp outline (the font's default
            // material has none). Only when using the body font, so the outline
            // material's atlas matches; title/button callers override afterward.
            if (font != null && font == VanillaUI.BodyFont && VanillaUI.BodyMaterial != null)
                tmp.fontSharedMaterial = VanillaUI.BodyMaterial;

            if (wasActive) go.SetActive(true);
            return tmp;
        }

        // Build a vertically-scrolling list under `parent`. Returns the
        // Content RectTransform (where list rows go) via the return
        // value, and the outer GameObject (suitable for SetActive
        // toggling) via the out parameter. The Content has a
        // VerticalLayoutGroup + ContentSizeFitter so children stack and
        // the scroll region sizes itself to fit them. To change the wheel
        // speed later (e.g. from config), set the ScrollRect's
        // scrollSensitivity on scrollRoot.
        public static RectTransform BuildScrollableList(
            Transform parent,
            string name,
            out GameObject scrollRoot,
            float scrollSensitivity = DefaultScrollSensitivity)
        {
            var rootRt = MakeChildRect(parent, name);
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            scrollRoot = rootRt.gameObject;

            var scrollRect = scrollRoot.AddComponent<ScrollRect>();
            scrollRect.horizontal   = false;
            scrollRect.vertical     = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = scrollSensitivity;

            // Viewport clips the content. Use RectMask2D (rect-based
            // clipping) rather than Mask: stencil-based Mask + a zero-alpha
            // mask graphic culls TextMeshPro content instead of clipping it,
            // which left the whole list invisible even though rows were built.
            // RectMask2D needs no graphic and clips Image + TMP alike. A
            // transparent raycast-target Image stays so drags over empty
            // space still scroll the list.
            // Reserve a gutter on the right for the scrollbar so list rows
            // don't sit under it.
            const float scrollbarWidth = 12f;
            const float scrollbarGap   = 4f;

            var viewportRt = MakeChildRect(rootRt, "Viewport");
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = new Vector2(-(scrollbarWidth + scrollbarGap), 0f);
            var viewportImage = viewportRt.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0);
            viewportRt.gameObject.AddComponent<RectMask2D>();

            // Content — list rows stack vertically; height fits content.
            var content = MakeChildRect(viewportRt, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot     = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                = 4;
            vlg.padding                = new RectOffset(5, 5, 5, 5);
            vlg.childAlignment         = TextAnchor.UpperCenter;
            // Control child sizes explicitly: rows take their preferred height
            // (so text rows grow with content) and fill the width.
            vlg.childControlHeight     = true;
            vlg.childControlWidth      = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth  = true;

            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRt;
            scrollRect.content  = content;

            // Visible vanilla scrollbar on the right. If the template can't be
            // resolved the list still scrolls (wheel + drag), just without a bar.
            var scrollbar = VanillaUI.CloneScrollbar(rootRt, scrollbarWidth);
            if (scrollbar != null)
            {
                scrollRect.verticalScrollbar = scrollbar;
                // Hide the bar when content fits (vanilla does the same — see
                // TextsDialog's m_rightScrollbar.activeSelf check). The gutter
                // stays reserved so rows don't reflow when it appears/hides.
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            }

            return content;
        }
    }
}
