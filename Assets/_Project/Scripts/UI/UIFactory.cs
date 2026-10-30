using UnityEngine;
using UnityEngine.UI;

namespace KnightSurvivors.UI
{
    /// <summary>
    /// Small helpers that build graybox UI from code (panels, labels, bars, buttons).
    /// Building UI in code means the scene builder only has to add a component, and nothing can
    /// break because of a missing reference in the Inspector. Real UI art replaces this later.
    /// </summary>
    public static class UIFactory
    {
        private static Font cachedFont;

        /// <summary>Unity's built-in font. (In Unity 2022.2+ it is "LegacyRuntime.ttf"; "Arial.ttf" no longer exists.)</summary>
        public static Font DefaultFont
        {
            get
            {
                if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return cachedFont;
            }
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchor a rect to a corner/edge using normalized anchors (0..1), plus a pixel size and offset.</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }

        /// <summary>Stretch a rect over its whole parent.</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            var rt = CreateRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text CreateText(string name, Transform parent, string text, int size, TextAnchor align)
        {
            var rt = CreateRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>A bar = dark background + colored fill. Set fill.fillAmount (0..1) to update it.</summary>
        public static Image CreateBar(string name, Transform parent, Color back, Color fill)
        {
            var bg = CreateImage(name, parent, back);
            var f = CreateImage("Fill", bg.transform, fill);
            Stretch(f.rectTransform);

            // A "Filled" image needs a sprite to draw; the built-in white UI sprite works fine.
            f.sprite = WhiteSprite;
            f.type = Image.Type.Filled;
            f.fillMethod = Image.FillMethod.Horizontal;
            f.fillOrigin = 0;
            f.fillAmount = 1f;
            return f;
        }

        private static Sprite whiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    var tex = Texture2D.whiteTexture;
                    whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }

                return whiteSprite;
            }
        }

        public static Button CreateButton(string name, Transform parent, string label, int fontSize, out Text text)
        {
            var img = CreateImage(name, parent, new Color(0.18f, 0.16f, 0.14f, 0.95f));
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.85f, 0.5f);
            colors.selectedColor = new Color(1f, 0.85f, 0.5f);
            button.colors = colors;
            text = CreateText("Label", img.transform, label, fontSize, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(12, 8);
            text.rectTransform.offsetMax = new Vector2(-12, -8);
            return button;
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
