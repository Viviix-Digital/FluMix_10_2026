using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FluMix
{
    /// <summary>
    /// Bàn phím UI trong canvas — tránh soft keyboard HTML của WebGL (làm mất fullscreen).
    /// </summary>
    public sealed class InGameKeyboard : MonoBehaviour
    {
        static readonly string[] Row1 = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        static readonly string[] Row2 = { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" };
        static readonly string[] Row3 = { "a", "s", "d", "f", "g", "h", "j", "k", "l" };
        static readonly string[] Row4 = { "z", "x", "c", "v", "b", "n", "m" };

        static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.14f, 0.96f);
        static readonly Color KeyColor = new Color(0.22f, 0.26f, 0.34f, 1f);
        static readonly Color KeySpecial = new Color(0.16f, 0.2f, 0.28f, 1f);
        static readonly Color KeyAccent = new Color(0.18f, 0.55f, 0.42f, 1f);

        RectTransform root;
        RectTransform panel;
        TMP_InputField target;
        RectTransform liftTarget;
        Vector2 liftBase;
        bool liftStored;
        bool shifted;
        bool built;
        bool visible;
        int caretIndex;
        Coroutine hideRoutine;
        Action<bool> visibilityChanged;

        public bool IsVisible => visible;
        public TMP_InputField Target => target;

        public void SetVisibilityListener(Action<bool> listener)
        {
            visibilityChanged = listener;
        }

        public void EnsureBuilt(Transform canvasRoot)
        {
            if (built)
                return;

#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.mobileKeyboardSupport = false;
#endif

            if (canvasRoot == null)
                canvasRoot = transform;

            root = FluMixUi.CreateRect(canvasRoot, "InGameKeyboard", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.SetAsLastSibling();

            Image dim = root.gameObject.AddComponent<Image>();
            dim.sprite = FluMixUi.White;
            dim.color = new Color(0f, 0f, 0f, 0.2f);
            dim.raycastTarget = true;

            var dimButton = root.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Hide);

            panel = FluMixUi.CreateRect(root, "Panel", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(0f, 390f);

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = FluMixUi.White;
            panelImage.color = PanelColor;
            panelImage.raycastTarget = true;

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 14, 16);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            AddRow(panel, Row1, false);
            AddRow(panel, Row2, false);
            AddRow(panel, Row3, false);
            AddLetterRowWithMods(panel);
            AddBottomRow(panel);

            root.gameObject.SetActive(false);
            built = true;
            visible = false;
        }

        public void Attach(TMP_InputField field, RectTransform panelToLift = null)
        {
            if (field == null)
                return;

            if (!built)
                EnsureBuilt(FindCanvasRoot(field.transform));

            CancelHide();
            bool sameField = target == field;
            target = field;
            target.onFocusSelectAll = false;
            // Caret riêng — không dựa selection TMP (focus lại thường SelectAll → ghi đè 1 ký tự).
            string text = target.text ?? string.Empty;
            if (!sameField)
                caretIndex = text.Length;
            else
                caretIndex = Mathf.Clamp(caretIndex, 0, text.Length);

            SetLiftTarget(panelToLift);
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.mobileKeyboardSupport = false;
#endif
            Show();
            ApplyCaretToField();
        }

        public void NotifyDeselect(TMP_InputField field)
        {
            if (!visible || field == null || field != target)
                return;

            CancelHide();
            hideRoutine = StartCoroutine(HideIfLostFocus());
        }

        public void Hide()
        {
            CancelHide();
            if (!built)
                return;

            visible = false;
            if (root != null)
                root.gameObject.SetActive(false);

            RestoreLift();
            target = null;
            visibilityChanged?.Invoke(false);

            if (EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject != null
                && IsUnderKeyboard(EventSystem.current.currentSelectedGameObject.transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        void Show()
        {
            if (!built || root == null)
                return;

            visible = true;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            ApplyLift();
            RefreshShiftLabels();
            visibilityChanged?.Invoke(true);
        }

        void SetLiftTarget(RectTransform panelToLift)
        {
            if (panelToLift == liftTarget)
                return;

            RestoreLift();
            liftTarget = panelToLift;
            liftStored = false;
            if (liftTarget != null)
            {
                liftBase = liftTarget.anchoredPosition;
                liftStored = true;
            }
        }

        void ApplyLift()
        {
            if (liftTarget == null || !liftStored)
                return;
            liftTarget.anchoredPosition = liftBase + new Vector2(0f, 210f);
        }

        void RestoreLift()
        {
            if (liftTarget != null && liftStored)
                liftTarget.anchoredPosition = liftBase;
        }

        IEnumerator HideIfLostFocus()
        {
            yield return null;
            yield return null;

            if (!visible)
                yield break;

            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            if (selected != null)
            {
                if (target != null && selected == target.gameObject)
                    yield break;

                TMP_InputField other = selected.GetComponent<TMP_InputField>();
                if (other != null)
                    yield break;

                if (IsUnderKeyboard(selected.transform))
                    yield break;
            }

            Hide();
        }

        void CancelHide()
        {
            if (hideRoutine == null)
                return;
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        static Transform FindCanvasRoot(Transform from)
        {
            Canvas canvas = from != null ? from.GetComponentInParent<Canvas>() : null;
            return canvas != null ? canvas.transform : from;
        }

        bool IsUnderKeyboard(Transform t)
        {
            return root != null && t != null && t.IsChildOf(root);
        }

        void AddRow(RectTransform parent, string[] keys, bool special)
        {
            RectTransform row = CreateRow(parent);
            for (int i = 0; i < keys.Length; i++)
            {
                string value = keys[i];
                MakeKey(row, value, () => TypeText(value), 1f, special ? KeySpecial : KeyColor);
            }
        }

        void AddLetterRowWithMods(RectTransform parent)
        {
            RectTransform row = CreateRow(parent);
            MakeKey(row, "⇧", ToggleShift, 1.35f, KeySpecial);
            for (int i = 0; i < Row4.Length; i++)
            {
                string value = Row4[i];
                MakeKey(row, value, () => TypeText(value), 1f, KeyColor);
            }

            MakeKey(row, "⌫", Backspace, 1.45f, KeySpecial);
        }

        void AddBottomRow(RectTransform parent)
        {
            RectTransform row = CreateRow(parent);
            MakeKey(row, ".", () => TypeText("."), 1f, KeySpecial);
            MakeKey(row, "_", () => TypeText("_"), 1f, KeySpecial);
            MakeKey(row, "-", () => TypeText("-"), 1f, KeySpecial);
            MakeKey(row, "@", () => TypeText("@"), 1f, KeySpecial);
            MakeKey(row, "+", () => TypeText("+"), 1f, KeySpecial);
            MakeKey(row, "space", () => TypeText(" "), 3.2f, KeyColor);
            MakeKey(row, "Xong", Hide, 1.8f, KeyAccent);
        }

        RectTransform CreateRow(RectTransform parent)
        {
            RectTransform row = FluMixUi.CreateRect(parent, "Row", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;
            le.minHeight = 56f;
            return row;
        }

        void MakeKey(RectTransform row, string label, Action action, float flex, Color color)
        {
            var go = new GameObject("Key_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(row, false);
            var image = go.GetComponent<Image>();
            image.sprite = FluMixUi.White;
            image.color = color;
            image.raycastTarget = true;

            var le = go.GetComponent<LayoutElement>();
            le.flexibleWidth = flex;
            le.minWidth = 36f;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                CancelHide();
                action?.Invoke();
            });

            TextMeshProUGUI text = FluMixUi.CreateText(go.transform, "Label", DisplayLabel(label), 28f, TextAlignmentOptions.Center);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.color = Color.white;
            text.raycastTarget = false;
            if (label == "space")
                text.text = "␣";
        }

        string DisplayLabel(string label)
        {
            if (label == "space")
                return "␣";
            if (label.Length == 1 && char.IsLetter(label[0]))
                return shifted ? label.ToUpperInvariant() : label;
            return label;
        }

        void RefreshShiftLabels()
        {
            if (panel == null)
                return;

            TextMeshProUGUI[] labels = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                Transform key = labels[i].transform.parent;
                if (key == null || !key.name.StartsWith("Key_", StringComparison.Ordinal))
                    continue;

                string token = key.name.Substring(4);
                if (token.Length == 1 && char.IsLetter(token[0]))
                    labels[i].text = shifted ? token.ToUpperInvariant() : token;
            }
        }

        void ToggleShift()
        {
            shifted = !shifted;
            RefreshShiftLabels();
        }

        void TypeText(string raw)
        {
            if (target == null || string.IsNullOrEmpty(raw))
                return;

            string insert = raw;
            if (raw.Length == 1 && char.IsLetter(raw[0]))
                insert = shifted ? raw.ToUpperInvariant() : raw;

            string text = target.text ?? string.Empty;
            caretIndex = Mathf.Clamp(caretIndex, 0, text.Length);

            if (target.characterLimit > 0 && text.Length + insert.Length > target.characterLimit)
                return;

            target.text = text.Insert(caretIndex, insert);
            caretIndex += insert.Length;
            ApplyCaretToField();

            if (shifted && insert.Length == 1 && char.IsLetter(insert[0]))
            {
                shifted = false;
                RefreshShiftLabels();
            }
        }

        void Backspace()
        {
            if (target == null)
                return;

            string text = target.text ?? string.Empty;
            if (text.Length == 0 || caretIndex <= 0)
                return;

            caretIndex = Mathf.Clamp(caretIndex, 0, text.Length);
            target.text = text.Remove(caretIndex - 1, 1);
            caretIndex--;
            ApplyCaretToField();
        }

        void ApplyCaretToField()
        {
            if (target == null)
                return;

            string text = target.text ?? string.Empty;
            caretIndex = Mathf.Clamp(caretIndex, 0, text.Length);
            target.onFocusSelectAll = false;
            target.caretPosition = caretIndex;
            target.selectionStringAnchorPosition = caretIndex;
            target.selectionStringFocusPosition = caretIndex;
            target.stringPosition = caretIndex;
            target.ForceLabelUpdate();
        }

        void OnDisable()
        {
            Hide();
        }
    }
}
