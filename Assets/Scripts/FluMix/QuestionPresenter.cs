using System;
using System.Collections;
using FluMix.Api;
using FluMix.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public class QuestionPresenter : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text prompt;
        [SerializeField] TMP_Text timer;
        [SerializeField] TMP_Text explanation;
        [SerializeField] Button[] optionButtons;
        [SerializeField] TMP_Text[] optionLabels;
        [SerializeField] Image rightHighlight;
        [SerializeField] Image wrongHighlight;
        [SerializeField] GameObject answerDescription;
        [SerializeField] TMP_Text answerBody;
        [SerializeField] TMP_Text answerAuthor;

        int cursor;
        bool locked;
        bool descriptionDismissed;
        Action<int> picked;
        QuestionData current;

        public QuestionData Current => current;

        public void Bind(
            GameConfig gameConfig,
            TMP_Text titleText,
            TMP_Text promptText,
            TMP_Text timerText,
            TMP_Text explanationText,
            Button[] buttons,
            TMP_Text[] labels,
            Image right,
            Image wrong)
        {
            config = gameConfig;
            title = titleText;
            prompt = promptText;
            timer = timerText;
            explanation = explanationText;
            optionButtons = buttons;
            optionLabels = labels;
            rightHighlight = right;
            wrongHighlight = wrong;
            WireButtons();
            ResolveAnswerPanel();
        }

        void Awake()
        {
            WireButtons();
            ResolveAnswerPanel();
            HideAnswerPanel();
        }

        public void AdoptConfig(GameConfig gameConfig)
        {
            config = gameConfig;
        }

        public void Lock()
        {
            locked = true;
            SetButtons(false);
        }

        public void ResetOrder()
        {
            cursor = 0;
        }

        public QuestionData TakeNext(QuestionData[] questions)
        {
            if (questions == null || questions.Length == 0)
                return null;
            QuestionData question = questions[cursor % questions.Length];
            cursor++;
            return CloneForDisplay(question);
        }

        static QuestionData CloneForDisplay(QuestionData source)
        {
            if (source == null)
                return null;

            var copy = new QuestionData
            {
                id = source.id,
                codeId = source.codeId,
                prompt = source.prompt,
                correctIndex = source.correctIndex,
                explanation = source.explanation,
                reference = source.reference,
                points = source.points,
                source = source.source
            };

            if (source.options != null)
            {
                copy.options = (string[])source.options.Clone();
                if (source.optionKeys != null && source.optionKeys.Length == source.options.Length)
                    copy.optionKeys = (string[])source.optionKeys.Clone();
                else if (source.correctIndex >= 0)
                {
                    // Offline local JSON: synthesize keys A/B/C…
                    copy.optionKeys = new string[source.options.Length];
                    for (int i = 0; i < source.options.Length; i++)
                        copy.optionKeys[i] = ((char)('A' + i)).ToString();
                }
            }

            if (copy.optionKeys != null && copy.correctIndex < 0)
                QuestionMapper.ShuffleOptions(copy);
            return copy;
        }

        public void Show(QuestionData question, int number, Action<int> onPicked)
        {
            current = question;
            picked = onPicked;
            locked = false;
            if (title != null)
                title.text = "CÂU HỎI " + number;
            if (prompt != null)
                prompt.text = question != null ? question.prompt : string.Empty;
            if (explanation != null)
            {
                explanation.text = string.Empty;
                explanation.gameObject.SetActive(false);
            }

            Hide(rightHighlight);
            Hide(wrongHighlight);

            if (optionLabels == null)
                return;

            int count = question != null && question.options != null ? question.options.Length : 0;
            for (int i = 0; i < optionLabels.Length; i++)
            {
                bool visible = i < count;
                if (optionLabels[i] != null)
                {
                    if (visible)
                    {
                        string key = QuestionMapper.KeyAt(question, i);
                        string label = question.options[i];
                        optionLabels[i].text = string.IsNullOrEmpty(key) ? label : key + ". " + label;
                    }
                    else
                    {
                        optionLabels[i].text = string.Empty;
                    }
                }

                if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                {
                    optionButtons[i].gameObject.SetActive(visible);
                    optionButtons[i].interactable = visible;
                }
            }

            SetTimer(config != null ? config.questionSeconds : 0f);
        }

        public void SetTimer(float remaining)
        {
            if (timer != null)
                timer.text = ScoreKeeper.FormatClock(remaining);
        }

        public void Pick(int index)
        {
            if (locked)
                return;
            locked = true;
            SetButtons(false);
            FluMixSfx.Play("quiz_select");
            picked?.Invoke(index);
        }

        public string KeyForIndex(int index) => QuestionMapper.KeyAt(current, index);

        public IEnumerator Reveal(QuestionData question, int pickedIndex, string correctKey = null)
        {
            if (question == null)
                yield break;

            int correctIndex = !string.IsNullOrEmpty(correctKey)
                ? QuestionMapper.IndexOfKey(question, correctKey)
                : question.correctIndex;

            if (correctIndex >= 0)
                Place(rightHighlight, correctIndex);
            if (pickedIndex >= 0 && pickedIndex != correctIndex)
                Place(wrongHighlight, pickedIndex);
            else
                Hide(wrongHighlight);

            if (explanation != null)
                explanation.gameObject.SetActive(false);

            if (pickedIndex >= 0 && pickedIndex != correctIndex)
                FluMixSfx.Play("quiz_wrong");
            yield break;
        }

        public IEnumerator ShowAnswerDescription(QuestionData question, int pickedIndex, string correctKey = null, bool? serverCorrect = null)
        {
            if (question == null || config != null && config.explainHoldSeconds <= config.explainDisabledSeconds)
                yield break;

            float delay = config != null ? Mathf.Max(0f, config.answerDescriptionDelay) : 1f;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            ResolveAnswerPanel();
            if (answerDescription == null)
                yield break;

            int correctIndex = !string.IsNullOrEmpty(correctKey)
                ? QuestionMapper.IndexOfKey(question, correctKey)
                : question.correctIndex;
            bool correct = serverCorrect ?? (pickedIndex == correctIndex);

            if (answerBody != null)
            {
                string body = question.explanation ?? string.Empty;
                if (!correct && correctIndex >= 0)
                    body = "Đáp án đúng: " + OptionLine(question, correctIndex) + "\n" + body;
                answerBody.text = body;
                answerBody.enableAutoSizing = true;
                answerBody.fontSizeMax = 30.6f;
                answerBody.fontSizeMin = 16f;
            }

            if (answerAuthor != null)
            {
                answerAuthor.text = string.IsNullOrEmpty(question.reference)
                    ? string.Empty
                    : "Tài liệu tham khảo: " + question.reference;
                answerAuthor.enableAutoSizing = true;
                answerAuthor.fontSizeMax = 22f;
                answerAuthor.fontSizeMin = 12f;
            }

            if (explanation != null && explanation.transform.parent != null)
                explanation.transform.parent.gameObject.SetActive(false);

            descriptionDismissed = false;
            answerDescription.SetActive(true);
            answerDescription.transform.SetAsLastSibling();
            WireAnswerDismiss();
            FluMixSfx.Play("quiz_explain");

            float hold = config != null && config.answerDescriptionSeconds > 0.5f ? config.answerDescriptionSeconds : 8f;
            float elapsed = 0f;
            while (elapsed < hold && !descriptionDismissed)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            HideAnswerPanel();
        }

        public void HideAll()
        {
            locked = true;
            picked = null;
            current = null;
            Hide(rightHighlight);
            Hide(wrongHighlight);
            if (explanation != null)
                explanation.gameObject.SetActive(false);
            HideAnswerPanel();
        }

        void WireButtons()
        {
            if (optionButtons == null)
                return;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                Button button = optionButtons[i];
                if (button == null)
                    continue;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Pick(index));
            }
        }

        void Place(Image highlight, int index)
        {
            if (highlight == null || optionButtons == null || index < 0 || index >= optionButtons.Length || optionButtons[index] == null)
                return;

            RectTransform mark = highlight.rectTransform;
            RectTransform option = optionButtons[index].transform as RectTransform;
            if (option == null)
                return;

            Vector2 position = mark.anchoredPosition;
            position.y = option.parent == mark.parent
                ? option.anchoredPosition.y
                : mark.parent.InverseTransformPoint(option.position).y;
            mark.anchoredPosition = position;
            highlight.gameObject.SetActive(true);
            highlight.raycastTarget = false;
        }

        void SetButtons(bool value)
        {
            if (optionButtons == null)
                return;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                    optionButtons[i].interactable = value;
            }
        }

        static void Hide(Image image)
        {
            if (image != null)
                image.gameObject.SetActive(false);
        }

        void ResolveAnswerPanel()
        {
            if (answerDescription != null && answerBody != null && answerAuthor != null)
                return;

            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                    continue;
                if (answerBody == null && text.name == "Answer Description Text (TMP)")
                {
                    answerBody = text;
                    if (text.transform.parent != null)
                        answerDescription = text.transform.parent.gameObject;
                }

                if (answerAuthor == null && text.name == "Author Text (TMP)")
                    answerAuthor = text;
            }
        }

        void WireAnswerDismiss()
        {
            if (answerDescription == null)
                return;

            var button = answerDescription.GetComponent<Button>();
            if (button == null)
            {
                button = answerDescription.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                var image = answerDescription.GetComponent<Image>();
                if (image != null)
                    button.targetGraphic = image;
            }

            button.onClick.RemoveListener(DismissAnswer);
            button.onClick.AddListener(DismissAnswer);
        }

        void DismissAnswer()
        {
            descriptionDismissed = true;
        }

        void HideAnswerPanel()
        {
            if (answerDescription != null)
                answerDescription.SetActive(false);
        }

        static string OptionLine(QuestionData question, int index)
        {
            string key = QuestionMapper.KeyAt(question, index);
            if (string.IsNullOrEmpty(key))
                key = index >= 0 && index < 26 ? ((char)('A' + index)).ToString() : "?";
            string option = question != null && question.options != null && index >= 0 && index < question.options.Length
                ? question.options[index]
                : string.Empty;
            return key + ". " + option;
        }
    }
}
