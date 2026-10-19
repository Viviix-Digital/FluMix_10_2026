using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public class BattleHud : MonoBehaviour
    {
        public TMP_Text TimeLabel;
        public TMP_Text ScoreLabel;
        public Image Warning;
        public GameObject Result;
        public Button Ready;
        BattleController _battle;
        int _shownScore;
        float _warnTimer;

        public void Bind(BattleController battle)
        {
            _battle = battle;
            if (Ready != null) Ready.onClick.AddListener(() => _battle.ConfirmShot());
            var buttons = transform.Find("Controls");
            if (buttons != null)
            {
                buttons.Find("G").GetComponent<Button>().onClick.AddListener(() => _battle.SelectGuardian(0));
                buttons.Find("A").GetComponent<Button>().onClick.AddListener(() => _battle.SelectGuardian(1));
                buttons.Find("T").GetComponent<Button>().onClick.AddListener(() => _battle.SelectGuardian(2));
            }
            if (Result != null)
                Result.transform.Find("Board").GetComponent<Button>().onClick.AddListener(() => FluMixFlow.Instance.GoLeaderboard());
        }

        public void Refresh()
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, GameSession.TimeLeft));
            if (TimeLabel != null)
            {
                TimeLabel.text = string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
                TimeLabel.color = seconds <= 10 ? new Color(1f, 0.25f, 0.3f) : Color.white;
                if (seconds <= 10) TimeLabel.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f)));
            }
            if (ScoreLabel != null) ScoreLabel.text = GameSession.Score.ToString();
            if (Warning != null && _warnTimer > 0f)
            {
                _warnTimer -= Time.unscaledDeltaTime;
                if (_warnTimer <= 0f) Warning.gameObject.SetActive(false);
            }
        }

        public void PunchScore(int delta)
        {
            if (ScoreLabel == null) return;
            ScoreLabel.color = delta >= 0 ? new Color(0.55f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
            StartCoroutine(ReturnWhite());
            var cfg = FluMixFlow.Instance != null ? FluMixFlow.Instance.Motion : null;
            if (cfg == null || !cfg.reducedMotion) StartCoroutine(UiMotion.Punch(ScoreLabel.rectTransform));
        }

        IEnumerator ReturnWhite()
        {
            yield return new WaitForSecondsRealtime(0.35f);
            if (ScoreLabel != null) ScoreLabel.color = Color.white;
        }

        public void ShowWarning()
        {
            if (Warning == null) return;
            Warning.gameObject.SetActive(true);
            _warnTimer = 1.5f;
        }

        public void ShowResult()
        {
            if (Result == null) return;
            Result.SetActive(true);
            var score = Result.transform.Find("Score").GetComponent<TMP_Text>();
            var leaks = Result.transform.Find("Leaks").GetComponent<TMP_Text>();
            var table = FluMixFlow.Instance.Strings;
            score.text = string.Format(table.Get("result.score"), GameSession.FormatScore(GameSession.Score));
            leaks.text = string.Format(table.Get("result.leaks"), GameSession.Leaks);
            StartCoroutine(UiMotion.ModalIn(null, Result.GetComponent<RectTransform>(), FluMixFlow.Instance.Motion));
        }
    }
}
