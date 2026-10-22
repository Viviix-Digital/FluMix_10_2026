using System;
using System.Collections;
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
            return question;
        }

        public void Show(QuestionData question, int number, Action<int> onPicked)
        {
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
                    optionLabels[i].text = visible ? question.options[i] : string.Empty;
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

        public IEnumerator Reveal(QuestionData question, int pickedIndex)
        {
            if (question == null)
                yield break;

            Place(rightHighlight, question.correctIndex);
            if (pickedIndex >= 0 && pickedIndex != question.correctIndex)
                Place(wrongHighlight, pickedIndex);
            else
                Hide(wrongHighlight);

            if (explanation != null)
                explanation.gameObject.SetActive(false);

            if (pickedIndex >= 0 && pickedIndex != question.correctIndex)
                FluMixSfx.Play("quiz_wrong");
            yield break;
        }

        public IEnumerator ShowAnswerDescription(QuestionData question, int pickedIndex)
        {
            if (question == null || config != null && config.explainHoldSeconds <= config.explainDisabledSeconds)
                yield break;

            float delay = config != null ? Mathf.Max(0f, config.answerDescriptionDelay) : 1f;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            ResolveAnswerPanel();
            if (answerDescription == null)
                yield break;

            bool correct = pickedIndex == question.correctIndex;
            if (answerBody != null)
            {
                string body = question.explanation ?? string.Empty;
                if (!correct)
                    body = "Đáp án đúng: " + OptionLine(question, question.correctIndex) + "\n" + body;
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
            string letter = index >= 0 && index < 26 ? ((char)('A' + index)).ToString() : "?";
            string option = question != null && question.options != null && index >= 0 && index < question.options.Length
                ? question.options[index]
                : string.Empty;
            return letter + ". " + option;
        }
    }
}
