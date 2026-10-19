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

        int cursor;
        bool locked;
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
        }

        void Awake()
        {
            WireButtons();
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
                if (optionLabels[i] != null)
                    optionLabels[i].text = i < count ? question.options[i] : string.Empty;
                if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                    optionButtons[i].interactable = i < count;
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
            {
                explanation.gameObject.SetActive(true);
                explanation.text = question.explanation;
                FluMixSfx.Play("quiz_explain");
            }

            if (pickedIndex >= 0 && pickedIndex != question.correctIndex)
                FluMixSfx.Play("quiz_wrong");

            float hold = config != null ? config.explainHoldSeconds : 0f;
            if (hold > 0f)
                yield return new WaitForSeconds(hold);
        }

        public void HideAll()
        {
            locked = true;
            picked = null;
            Hide(rightHighlight);
            Hide(wrongHighlight);
            if (explanation != null)
                explanation.gameObject.SetActive(false);
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
            if (highlight == null || optionLabels == null || index < 0 || index >= optionLabels.Length || optionLabels[index] == null)
                return;

            highlight.gameObject.SetActive(true);
            highlight.rectTransform.position = optionLabels[index].rectTransform.position;
            highlight.transform.SetSiblingIndex(0);
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
    }
}
