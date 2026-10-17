using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public static class FluMixUi
    {
        static Sprite white;

        public static TMP_FontAsset Font { get; set; }

        public static Sprite White
        {
            get
            {
                if (white == null)
                {
                    Texture2D texture = Texture2D.whiteTexture;
                    white = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }

                return white;
            }
        }

        public static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 size, bool raycast)
        {
            var rect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : White;
            image.raycastTarget = raycast;
            image.preserveAspect = false;
            return image;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null)
                label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }
    }
}
