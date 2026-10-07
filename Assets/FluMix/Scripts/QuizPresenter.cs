using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public class QuizPresenter : MonoBehaviour
    {
        public BattleController Battle;
        TMP_Text _tab;
        TMP_Text _prompt;
        Button[] _options;
        TMP_Text[] _labels;
        Image[] _fills;
        Button _ready;
        int _selected = -1;
        bool _locked;
        QuestionDef _question;

        void Awake()
        {
            _tab = transform.Find("Panel/Tab").GetComponent<TMP_Text>();
            _prompt = transform.Find("Panel/Prompt").GetComponent<TMP_Text>();
            _ready = transform.Find("Ready").GetComponent<Button>();
            _options = new Button[4];
            _labels = new TMP_Text[4];
            _fills = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var row = transform.Find("Panel/Opt" + i);
                _options[i] = row.GetComponent<Button>();
                _fills[i] = row.GetComponent<Image>();
                _labels[i] = row.Find("Label").GetComponent<TMP_Text>();
                int captured = i;
                _options[i].onClick.AddListener(() => Select(captured));
            }
            _ready.onClick.AddListener(Confirm);
        }

        public void ShowQuestion(QuestionDef question)
        {
            _question = question;
            _selected = -1;
            _locked = false;
            GameSession.SelectedOption = -1;
            gameObject.SetActive(true);
            _tab.text = "CÂU " + (GameSession.QuestionIndex + 1);
            _prompt.text = question.prompt;
            string[] letters = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                _labels[i].text = letters[i] + "   " + question.options[i];
                _fills[i].color = new Color(0.85f, 0.95f, 1f, 0.95f);
                _options[i].interactable = true;
            }
            _ready.interactable = false;
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_modal_open");
            var dim = transform.Find("Dim").GetComponent<CanvasGroup>();
            var panel = transform.Find("Panel") as RectTransform;
            StartCoroutine(UiMotion.ModalIn(dim, panel, FluMixFlow.Instance.Motion));
        }

        void Select(int index)
        {
            if (_locked) return;
            _selected = index;
            GameSession.SelectedOption = index;
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("quiz_select");
            for (int i = 0; i < 4; i++)
            {
                _fills[i].color = i == index
                    ? new Color(0.65f, 0.95f, 1f, 1f)
                    : new Color(0.8f, 0.88f, 0.95f, 0.75f);
            }
            if (!_ready.interactable)
            {
                _ready.interactable = true;
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("quiz_unlock");
            }
            StartCoroutine(Sheen(_fills[index].transform));
        }

        IEnumerator Sheen(Transform row)
        {
            var sheen = row.Find("Sheen");
            if (sheen == null) yield break;
            var img = sheen.GetComponent<Image>();
            var rt = sheen as RectTransform;
            sheen.gameObject.SetActive(true);
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.35f);
                rt.anchoredPosition = new Vector2(Mathf.Lerp(-220f, 220f, k), 0f);
                var c = img.color;
                c.a = Mathf.Sin(k * Mathf.PI) * 0.55f;
                img.color = c;
                yield return null;
            }
            sheen.gameObject.SetActive(false);
        }

        void Confirm()
        {
            if (_locked || _selected < 0) return;
            _locked = true;
            for (int i = 0; i < 4; i++) _options[i].interactable = false;
            _ready.interactable = false;
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_confirm");
            Battle.ResolveQuiz(_selected);
        }

        public void ShowCorrect(QuestionDef q)
        {
            StartCoroutine(ShowExplain(true, q, -1));
        }

        public void ShowWrong(QuestionDef q, int selected)
        {
            var panel = transform.Find("Panel") as RectTransform;
            var cfg = FluMixFlow.Instance.Motion;
            if (cfg == null || !cfg.reducedMotion) StartCoroutine(UiMotion.Shake(panel, 0.28f));
            for (int i = 0; i < 4; i++)
            {
                if (i == q.correctIndex) _fills[i].color = new Color(0.45f, 0.95f, 0.45f, 1f);
                else if (i == selected) _fills[i].color = new Color(1f, 0.4f, 0.4f, 1f);
            }
            StartCoroutine(ShowExplain(false, q, selected));
        }

        IEnumerator ShowExplain(bool correct, QuestionDef q, int selected)
        {
            yield return new WaitForSecondsRealtime(correct ? 0.05f : 0.35f);
            var explain = transform.Find("Explain");
            var questionPanel = transform.Find("Panel");
            if (questionPanel != null) questionPanel.gameObject.SetActive(false);
            explain.SetAsLastSibling();
            explain.gameObject.SetActive(true);
            explain.Find("Headline").GetComponent<TMP_Text>().text = correct
                ? FluMixFlow.Instance.Strings.Get("explain.correct")
                : FluMixFlow.Instance.Strings.Get("explain.wrong");
            explain.Find("Affirm").GetComponent<TMP_Text>().text = q.affirmation;
            explain.Find("Body").GetComponent<TMP_Text>().text = q.body;
            var cite = explain.Find("Cite").GetComponent<TMP_Text>();
            cite.text = q.citation ?? "";
            cite.gameObject.SetActive(!string.IsNullOrEmpty(q.citation));
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("quiz_explain");
            var headline = explain.Find("Headline") as RectTransform;
            headline.localScale = Vector3.one * 0.6f;
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                headline.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, UiMotion.EaseOutBack(Mathf.Clamp01(t / 0.3f)));
                yield return null;
            }
            float wait = FluMixFlow.Instance.Rules.explainAutoContinue;
            float elapsed = 0f;
            while (elapsed < wait)
            {
                elapsed += Time.unscaledDeltaTime;
                var mouse = UnityEngine.InputSystem.Mouse.current;
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if ((mouse != null && mouse.leftButton.wasPressedThisFrame) || (keyboard != null && keyboard.enterKey.wasPressedThisFrame))
                    break;
                yield return null;
            }
            explain.gameObject.SetActive(false);
            gameObject.SetActive(false);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_modal_close");
            Battle.CloseQuiz();
        }
    }
}
