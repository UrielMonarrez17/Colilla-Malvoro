using System;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo2.UI
{
    public static class FeralTheme
    {
        public static readonly Color Ink = new Color32(22, 20, 19, 249);
        public static readonly Color Card = new Color32(42, 36, 30, 250);
        public static readonly Color Cream = new Color32(232, 211, 181, 255);
        public static readonly Color Muted = new Color32(179, 159, 136, 255);
        public static readonly Color Red = new Color32(135, 39, 27, 255);
        public static readonly Color Gold = new Color32(197, 150, 88, 255);
        public static readonly Color Ready = new Color32(166, 185, 132, 255);
        public static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); Place(r, x, y, w, h); return r;
        }
        public static void Place(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        }
        public static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
        public static Image Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        public static MenuBrushGraphic Brush(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var graphic = Rect(name, parent, x, y, w, h).gameObject.AddComponent<MenuBrushGraphic>();
            graphic.color = color; graphic.raycastTarget = false; return graphic;
        }
        public static RawImage Illustration(string name, Transform parent, int quadrant, float x, float y, float size)
        {
            var image = Rect(name, parent, x, y, size, size).gameObject.AddComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>("FeralUI/SurvivalAtlas");
            image.uvRect = new Rect((quadrant % 2) * .5f, quadrant < 2 ? .5f : 0, .5f, .5f);
            image.raycastTarget = false; image.enabled = image.texture != null; return image;
        }
        public static Text Label(string name, Transform parent, string value, float x, float y, float w, float h, int size = 24, Color? color = null)
        {
            var label = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = Font; label.fontSize = size; label.color = color ?? Cream;
            label.text = value; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }
        public static Button Button(string name, Transform parent, string text, float x, float y, float w, float h, Action action, bool primary = false)
        {
            var rect = Rect(name, parent, x, y, w, h);
            var graphic = rect.gameObject.AddComponent<MenuBrushGraphic>();
            var b = rect.gameObject.AddComponent<Button>(); b.targetGraphic = graphic;
            StyleButton(b, primary);
            var label = Label(name + "Label", rect, text, 20, 0, w - 40, h, 24);
            label.alignment = TextAnchor.MiddleLeft;
            if (action != null) b.onClick.AddListener(() => action());
            return b;
        }
        public static void StyleButton(Button b, bool primary = false)
        {
            var c = b.colors; c.normalColor = primary ? Red : Card;
            c.highlightedColor = c.selectedColor = new Color32(160, 62, 40, 255);
            c.pressedColor = new Color32(91, 24, 18, 255); c.disabledColor = new Color32(53, 48, 42, 160);
            c.fadeDuration = .12f; b.colors = c;
        }
        public static Slider Slider(string name, Transform parent, float x, float y, float width, float value, Action<float> changed)
        {
            var r = Rect(name, parent, x, y, width, 36);
            var bg = Panel("Track", r, 0, 12, width, 10, Card);
            var fill = Panel("Fill", r, 0, 12, width, 10, Gold);
            var handle = Panel("Handle", r, 0, 4, 16, 28, Cream);
            handle.raycastTarget = true; bg.raycastTarget = true;
            var slider = r.gameObject.AddComponent<Slider>(); slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform; slider.targetGraphic = handle; slider.value = value;
            if (changed != null) slider.onValueChanged.AddListener(v => changed(v));
            return slider;
        }
        public static RectTransform Scroll(string name, Transform parent, float x, float y, float w, float h, float contentHeight)
        {
            var root = Rect(name, parent, x, y, w, h);
            var bg = root.gameObject.AddComponent<Image>(); bg.color = Ink;
            var scroll = root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 45;
            var viewport = Rect("Viewport", root, 0, 0, w, h); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport, 0, 0, w - 12, Mathf.Max(h, contentHeight));
            scroll.viewport = viewport; scroll.content = content; return content;
        }
        public static void ApplyMenu(Transform root)
        {
            foreach (var page in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (page.name != "Sessions" && page.name != "Settings" && page.name != "Controls" && page.name != "Exit") continue;
                foreach (var t in page.GetComponentsInChildren<Text>(true))
                { t.color = Cream; t.font = Font; if (t.text == "I / TAB") t.text = "TAB"; }
                if (page.name == "Controls")
                {
                    var shortcuts = page.Find("MenuShortcuts") as RectTransform;
                    if (!shortcuts) shortcuts = Label("MenuShortcuts", page, "M  Diario de misiones     ·     ESC  Pausa", 112, 680, 790, 30, 20, Gold).rectTransform;
                    Place(shortcuts, 112, 680, 790, 30);
                }
                foreach (var image in page.GetComponentsInChildren<Image>(true))
                { image.color = image.name.Contains("Shade") ? new Color(.045f, .036f, .03f, .97f) : Card; }
                foreach (var b in page.GetComponentsInChildren<Button>(true))
                {
                    var old = b.GetComponent<Image>();
                    if (old) UnityEngine.Object.DestroyImmediate(old);
                    if (!b.GetComponent<CanvasRenderer>()) b.gameObject.AddComponent<CanvasRenderer>();
                    var brush = b.GetComponent<MenuBrushGraphic>();
                    if (!brush) brush = b.gameObject.AddComponent<MenuBrushGraphic>();
                    b.targetGraphic = brush; brush.SetAllDirty();
                    StyleButton(b, b.name == "NewGame" || b.name == "ConfirmQuit");
                }
                foreach (var s in page.GetComponentsInChildren<Slider>(true))
                {
                    if (s.fillRect != null && s.fillRect.TryGetComponent<Image>(out var fill)) fill.color = Gold;
                    if (s.handleRect != null && s.handleRect.TryGetComponent<Image>(out var handle)) handle.color = Cream;
                }
            }
        }
    }
}
