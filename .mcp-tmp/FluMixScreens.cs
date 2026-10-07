using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public static class UiMotion
    {
        public static IEnumerator Enter(CanvasGroup group, RectTransform rect, UiMotionConfig cfg)
        {
            if (group == null || rect == null) yield break;
            Vector2 end = rect.anchoredPosition;
            float slide = cfg != null ? cfg.enterSlide : 24f;
            float dur = cfg != null ? cfg.enterDuration : 0.28f;
            group.alpha = 0f;
            rect.anchoredPosition = end + new Vector2(0f, -slide);
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = EaseOutCubic(Mathf.Clamp01(t / dur));
                group.alpha = k;
                rect.anchoredPosition = Vector2.Lerp(end + new Vector2(0f, -slide), end, k);
                yield return null;
            }
            group.alpha = 1f;
            rect.anchoredPosition = end;
        }

        public static IEnumerator ModalIn(CanvasGroup dim, RectTransform panel, UiMotionConfig cfg)
        {
            float dur = cfg != null && !cfg.reducedMotion ? cfg.modalInDuration : 0.12f;
            float start = cfg != null ? cfg.modalStartScale : 0.86f;
            float dimA = cfg != null ? cfg.modalDim : 0.45f;
            if (dim != null) dim.alpha = 0f;
            panel.localScale = Vector3.one * start;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = cfg != null && cfg.reducedMotion ? k : EaseOutBack(k);
                if (dim != null) dim.alpha = dimA * k;
                panel.localScale = Vector3.one * Mathf.Lerp(start, 1f, e);
                yield return null;
            }
            if (dim != null) dim.alpha = dimA;
            panel.localScale = Vector3.one;
        }

        public static IEnumerator Shake(RectTransform rect, float duration)
        {
            Vector2 origin = rect.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(t / duration);
                float x = Mathf.Sin(t / duration * 3f * Mathf.PI * 2f) * 8f * k;
                rect.anchoredPosition = origin + new Vector2(x, 0f);
                yield return null;
            }
            rect.anchoredPosition = origin;
        }

        public static IEnumerator Punch(RectTransform rect)
        {
            Vector3 baseScale = Vector3.one;
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.22f);
                float s = k < 0.45f ? Mathf.Lerp(1f, 1.18f, k / 0.45f) : Mathf.Lerp(1.18f, 1f, (k - 0.45f) / 0.55f);
                rect.localScale = baseScale * s;
                yield return null;
            }
            rect.localScale = baseScale;
        }

        public static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

        public static float EaseOutBack(float x)
        {
            const float c1 = 1.1f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }

    public class PillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Button Button;
        public Image Glow;
        public Image Ring;
        Vector3 _base = Vector3.one;
        bool _over;

        void Awake()
        {
            if (Button == null) Button = GetComponent<Button>();
        }

        void Update()
        {
            if (Button != null && !Button.interactable)
            {
                if (Glow != null) Glow.color = new Color(1f, 1f, 1f, 0.15f);
                return;
            }
            if (Glow != null)
            {
                float a = 0.65f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 1.1f));
                var c = Glow.color;
                c.a = a;
                Glow.color = c;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Button != null && !Button.interactable) return;
            _over = true;
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_hover");
            StopAllCoroutines();
            StartCoroutine(ScaleTo(_base * 1.05f, 0.12f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _over = false;
            StopAllCoroutines();
            StartCoroutine(ScaleTo(_base, 0.12f));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Button != null && !Button.interactable) return;
            StopAllCoroutines();
            StartCoroutine(Press());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_over) StartCoroutine(ScaleTo(_base * 1.05f, 0.08f));
        }

        IEnumerator Press()
        {
            yield return ScaleTo(_base * 0.96f, 0.06f);
            yield return ScaleTo(_base, 0.06f);
            if (Ring != null) StartCoroutine(RingBurst());
        }

        IEnumerator ScaleTo(Vector3 target, float dur)
        {
            Vector3 start = transform.localScale;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(start, target, Mathf.Clamp01(t / dur));
                yield return null;
            }
            transform.localScale = target;
        }

        IEnumerator RingBurst()
        {
            Ring.gameObject.SetActive(true);
            var rt = Ring.rectTransform;
            var c = Ring.color;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.25f);
                rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.35f, k);
                c.a = 1f - k;
                Ring.color = c;
                yield return null;
            }
            Ring.gameObject.SetActive(false);
        }
    }

    public class TitleBob : MonoBehaviour
    {
        RectTransform[] _glyphs;
        Vector2 _root;
        bool _ready;

        public void Play(RectTransform[] glyphs)
        {
            _glyphs = glyphs;
            _root = ((RectTransform)transform).anchoredPosition;
            _ready = true;
            for (int i = 0; i < glyphs.Length; i++)
            {
                glyphs[i].localScale = Vector3.zero;
                glyphs[i].localRotation = Quaternion.Euler(0f, 0f, -8f);
            }
            StartCoroutine(Intro());
        }

        IEnumerator Intro()
        {
            for (int i = 0; i < _glyphs.Length; i++)
            {
                StartCoroutine(Pop(_glyphs[i]));
                yield return new WaitForSecondsRealtime(0.04f);
            }
        }

        IEnumerator Pop(RectTransform rt)
        {
            float t = 0f;
            while (t < 0.28f)
            {
                t += Time.unscaledDeltaTime;
                float k = UiMotion.EaseOutBack(Mathf.Clamp01(t / 0.28f));
                rt.localScale = Vector3.one * k;
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-8f, 0f, k));
                yield return null;
            }
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        void Update()
        {
            if (!_ready) return;
            float y = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 2.4f) * 3f;
            ((RectTransform)transform).anchoredPosition = _root + new Vector2(0f, y);
        }
    }

    public class LoginController : MonoBehaviour
    {
        TMP_InputField _account;
        TMP_InputField _password;
        Button _submit;
        Button _eye;
        GameObject _modal;
        bool _hidden = true;

        void Start()
        {
            var card = transform.Find("Card");
            _account = card.Find("Account").GetComponent<TMP_InputField>();
            _password = card.Find("Password").GetComponent<TMP_InputField>();
            _submit = card.Find("Submit").GetComponent<Button>();
            _eye = card.Find("Eye").GetComponent<Button>();
            _modal = transform.Find("ForgotModal").gameObject;
            _submit.onClick.AddListener(Submit);
            _eye.onClick.AddListener(ToggleEye);
            card.Find("Forgot").GetComponent<Button>().onClick.AddListener(OpenForgot);
            _modal.transform.Find("Panel/Ok").GetComponent<Button>().onClick.AddListener(CloseForgot);
            _account.onValueChanged.AddListener(_ => Keystroke());
            _password.onValueChanged.AddListener(_ => Keystroke());
            var group = card.GetComponent<CanvasGroup>();
            var motion = FluMixFlow.Instance != null ? FluMixFlow.Instance.Motion : null;
            StartCoroutine(UiMotion.Enter(group, card.GetComponent<RectTransform>(), motion));
            var title = card.Find("Title");
            var bob = title.GetComponent<TitleBob>();
            if (bob != null)
            {
                var glyphs = new RectTransform[title.childCount];
                for (int i = 0; i < title.childCount; i++) glyphs[i] = title.GetChild(i) as RectTransform;
                bob.Play(glyphs);
            }
        }

        void Keystroke()
        {
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("input_key");
        }

        void ToggleEye()
        {
            _hidden = !_hidden;
            _password.contentType = _hidden ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            _password.ForceLabelUpdate();
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("input_password");
        }

        void Submit()
        {
            bool emptyA = string.IsNullOrWhiteSpace(_account.text);
            bool emptyP = string.IsNullOrWhiteSpace(_password.text);
            if (emptyA || emptyP)
            {
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_deny");
                var target = emptyA ? _account.transform as RectTransform : _password.transform as RectTransform;
                StartCoroutine(UiMotion.Shake(target, 0.28f));
                return;
            }
            GameSession.Account = _account.text.Trim();
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_confirm");
            FluMixFlow.Instance.GoLobby();
        }

        void OpenForgot()
        {
            _modal.SetActive(true);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_modal_open");
            var panel = _modal.transform.Find("Panel") as RectTransform;
            var dim = _modal.GetComponent<CanvasGroup>();
            StartCoroutine(UiMotion.ModalIn(dim, panel, FluMixFlow.Instance.Motion));
        }

        void CloseForgot()
        {
            _modal.SetActive(false);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_modal_close");
        }
    }

    public class HowToController : MonoBehaviour
    {
        void Start()
        {
            var panel = transform.Find("Panel");
            var group = panel.GetComponent<CanvasGroup>();
            StartCoroutine(UiMotion.Enter(group, panel as RectTransform, FluMixFlow.Instance != null ? FluMixFlow.Instance.Motion : null));
            var title = panel.Find("Title");
            var bob = title.GetComponent<TitleBob>();
            if (bob != null)
            {
                var glyphs = new RectTransform[title.childCount];
                for (int i = 0; i < title.childCount; i++) glyphs[i] = title.GetChild(i) as RectTransform;
                bob.Play(glyphs);
            }
            transform.Find("Ready").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_confirm");
                FluMixFlow.Instance.BeginBattle();
            });
            StartCoroutine(StaggerRows(panel));
        }

        void Update()
        {
            var dots = GetComponentsInChildren<Image>(true);
            float p = 0.75f + 0.25f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f));
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i].name != "Dot") continue;
                var c = dots[i].color;
                c.a = p;
                dots[i].color = c;
            }
        }

        IEnumerator StaggerRows(Transform panel)
        {
            for (int i = 0; i < panel.childCount; i++)
            {
                var cg = panel.GetChild(i).GetComponent<CanvasGroup>();
                if (cg == null) continue;
                cg.alpha = 0f;
            }
            for (int i = 0; i < panel.childCount; i++)
            {
                var cg = panel.GetChild(i).GetComponent<CanvasGroup>();
                if (cg == null) continue;
                float t = 0f;
                var rt = cg.GetComponent<RectTransform>();
                Vector2 end = rt.anchoredPosition;
                while (t < 0.2f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / 0.2f);
                    cg.alpha = k;
                    rt.anchoredPosition = end + new Vector2(0f, Mathf.Lerp(16f, 0f, k));
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.05f);
            }
        }
    }

    public class LobbyController : MonoBehaviour
    {
        public ParticleSystem LeftJet;
        public ParticleSystem RightJet;
        public ParticleSystem LeftSplash;
        public ParticleSystem RightSplash;
        bool _intro;

        void Start()
        {
            transform.Find("HowTo").GetComponent<Button>().onClick.AddListener(() => FluMixFlow.Instance.GoHowTo());
            transform.Find("Fight").GetComponent<Button>().onClick.AddListener(() => StartCoroutine(WaterThenBattle()));
            transform.Find("Board").GetComponent<Button>().onClick.AddListener(() => FluMixFlow.Instance.GoLeaderboard());
            SetJets(false);
            if (FluMixFlow.Instance != null && FluMixFlow.Instance.State == FlowState.WaterIntro)
                StartCoroutine(WaterThenBattle());
        }

        public void PlayWaterIsolated()
        {
            StartCoroutine(WaterOnly());
        }

        IEnumerator WaterOnly()
        {
            SetJets(true);
            if (FluMixAudio.Instance != null)
            {
                FluMixAudio.Instance.Play("stinger_sos");
                FluMixAudio.Instance.Play("water_loop");
            }
            yield return new WaitForSecondsRealtime(1.6f);
            SetJets(false);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.StopWater();
        }

        IEnumerator WaterThenBattle()
        {
            if (_intro) yield break;
            _intro = true;
            FluMixFlow.Instance.State = FlowState.WaterIntro;
            FluMixLog.Info("Flow", "WaterIntro");
            var cfg = FluMixFlow.Instance.Motion;
            if (cfg != null && cfg.reducedMotion)
            {
                FluMixFlow.Instance.BeginBattle();
                yield break;
            }
            SetJets(true);
            if (FluMixAudio.Instance != null)
            {
                FluMixAudio.Instance.Play("stinger_sos");
                FluMixAudio.Instance.Play("water_loop");
            }
            yield return new WaitForSecondsRealtime(1.6f);
            var flash = transform.Find("Flash");
            if (flash != null)
            {
                var img = flash.GetComponent<Image>();
                flash.gameObject.SetActive(true);
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.unscaledDeltaTime;
                    var c = img.color;
                    c.a = Mathf.Clamp01(t / 0.25f);
                    img.color = c;
                    yield return null;
                }
            }
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.StopWater();
            SetJets(false);
            FluMixFlow.Instance.BeginBattle();
        }

        void SetJets(bool on)
        {
            if (LeftJet != null) { if (on) LeftJet.Play(); else LeftJet.Stop(); }
            if (RightJet != null) { if (on) RightJet.Play(); else RightJet.Stop(); }
            if (LeftSplash != null) { if (on) LeftSplash.Play(); else LeftSplash.Stop(); }
            if (RightSplash != null) { if (on) RightSplash.Play(); else RightSplash.Stop(); }
        }

        void Update()
        {
            var siren = transform.Find("Siren");
            if (siren != null) siren.Rotate(0f, 0f, 90f * Time.unscaledDeltaTime);
            var rings = transform.Find("Rings");
            if (rings == null) return;
            for (int i = 0; i < rings.childCount; i++)
            {
                float t = Mathf.Repeat(Time.unscaledTime * 0.45f + i * 0.4f, 1.4f) / 1.4f;
                float s = Mathf.Lerp(0.4f, 1.6f, t);
                rings.GetChild(i).localScale = Vector3.one * s;
                var img = rings.GetChild(i).GetComponent<Image>();
                if (img != null)
                {
                    var c = img.color;
                    c.a = 1f - t;
                    img.color = c;
                }
            }
            var sos = transform.Find("Sos");
            if (sos != null)
            {
                var cg = sos.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0.85f + 0.15f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
            }
        }
    }

    public class LeaderboardController : MonoBehaviour
    {
        BoardFile _file;
        int _page;
        Transform _slots;

        void Start()
        {
            _file = LeaderboardStore.Load();
            _slots = transform.Find("Panel/Slots");
            transform.Find("Home").GetComponent<Button>().onClick.AddListener(() => FluMixFlow.Instance.GoLobby());
            transform.Find("Close").GetComponent<Button>().onClick.AddListener(Close);
            transform.Find("Prev").GetComponent<Button>().onClick.AddListener(() => ChangePage(-1));
            transform.Find("Next").GetComponent<Button>().onClick.AddListener(() => ChangePage(1));
            var title = transform.Find("Panel/Title");
            var bob = title.GetComponent<TitleBob>();
            if (bob != null)
            {
                var glyphs = new RectTransform[title.childCount];
                for (int i = 0; i < title.childCount; i++) glyphs[i] = title.GetChild(i) as RectTransform;
                bob.Play(glyphs);
            }
            ShowPage(0);
            StartCoroutine(UiMotion.Enter(transform.Find("Panel").GetComponent<CanvasGroup>(), transform.Find("Panel") as RectTransform, FluMixFlow.Instance.Motion));
        }

        void ChangePage(int dir)
        {
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_page");
            ShowPage(Mathf.Clamp(_page + dir, 0, 3));
        }

        void ShowPage(int page)
        {
            _page = page;
            var label = transform.Find("Page").GetComponent<TMP_Text>();
            label.text = (_page + 1) + "/4";
            StartCoroutine(UiMotion.Punch(label.rectTransform));
            if (_page == 0)
            {
                int[] map = { 1, 0, 2 };
                for (int i = 0; i < _slots.childCount; i++)
                {
                    var slot = _slots.GetChild(i).gameObject;
                    if (i >= map.Length || _file == null || map[i] >= _file.entries.Count)
                    {
                        slot.SetActive(false);
                        continue;
                    }
                    slot.SetActive(true);
                    var entry = _file.entries[map[i]];
                    slot.GetComponentInChildren<TMP_Text>().text = entry.name + "\n" + GameSession.FormatScore(entry.score) + " điểm";
                }
                StartCoroutine(DropPodium());
                return;
            }
            int pageSize = 5;
            for (int i = 0; i < _slots.childCount; i++)
            {
                int index = 3 + (_page - 1) * pageSize + i;
                var slot = _slots.GetChild(i).gameObject;
                if (_file == null || index >= _file.entries.Count)
                {
                    slot.SetActive(false);
                    continue;
                }
                slot.SetActive(true);
                var entry = _file.entries[index];
                slot.GetComponentInChildren<TMP_Text>().text = entry.name + "\n" + GameSession.FormatScore(entry.score) + " điểm";
            }
        }

        IEnumerator DropPodium()
        {
            int[] order = { 1, 0, 2 };
            for (int n = 0; n < order.Length && n < _slots.childCount; n++)
            {
                var rt = _slots.GetChild(order[n]) as RectTransform;
                if (!rt.gameObject.activeSelf) continue;
                Vector2 end = rt.anchoredPosition;
                float t = 0f;
                while (t < 0.45f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = EaseOutBounce(Mathf.Clamp01(t / 0.45f));
                    rt.anchoredPosition = end + new Vector2(0f, Mathf.Lerp(40f, 0f, k));
                    yield return null;
                }
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("podium_land");
                yield return new WaitForSecondsRealtime(0.08f);
            }
        }

        static float EaseOutBounce(float x)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (x < 1f / d1) return n1 * x * x;
            if (x < 2f / d1) return n1 * (x -= 1.5f / d1) * x + 0.75f;
            if (x < 2.5f / d1) return n1 * (x -= 2.25f / d1) * x + 0.9375f;
            return n1 * (x -= 2.625f / d1) * x + 0.984375f;
        }

        void Close()
        {
            var prev = GameSession.Previous;
            if (prev == FlowState.Leaderboard || prev == FlowState.Boot) prev = FlowState.Lobby;
            FluMixFlow.Instance.Go(prev);
        }
    }

    public class RibbonMotion : MonoBehaviour
    {
        void Update()
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 3f) * 4f);
            var heart = transform.Find("Heart");
            if (heart != null)
            {
                float s = 1f + 0.06f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.4f));
                heart.localScale = Vector3.one * s;
            }
        }
    }

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
            gameObject.SetActive(false);
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
