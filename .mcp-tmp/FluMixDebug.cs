#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public class DebugOverlay : MonoBehaviour
    {
        public static DebugOverlay Instance { get; private set; }
        Canvas _canvas;
        TMP_Text _body;
        bool _visible = true;
        float _volume = 1f;
        bool _muted;

        public static void Ensure(FluMixFlow flow)
        {
            if (flow.GetComponent<DebugOverlay>() == null)
                flow.gameObject.AddComponent<DebugOverlay>();
        }

        void Awake()
        {
            Instance = this;
            Build();
            FluMixLog.LineAdded += _ => Refresh();
        }

        void Build()
        {
            var go = new GameObject("DebugOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = NewImage(go.transform, "Panel", new Color(0.02f, 0.05f, 0.1f, 0.82f));
            var rt = panel.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(460f, 0f);
            rt.anchoredPosition = Vector2.zero;

            var textGo = new GameObject("Body", typeof(RectTransform));
            textGo.transform.SetParent(panel.transform, false);
            var tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 0.42f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.offsetMin = new Vector2(12f, 8f);
            tr.offsetMax = new Vector2(-12f, -12f);
            _body = textGo.AddComponent<TextMeshProUGUI>();
            _body.fontSize = 18f;
            _body.color = Color.white;
            _body.alignment = TextAlignmentOptions.TopLeft;
            if (TMP_Settings.defaultFontAsset != null) _body.font = TMP_Settings.defaultFontAsset;

            string[] labels = { "G", "A", "T", "Ready", "+100", "-50", "Match", "Evolve", "3s", "Q0", "Board", "Water", "Battle", "Mute" };
            System.Action[] actions =
            {
                () => Select(0), () => Select(1), () => Select(2), Confirm,
                () => Score(100), () => Score(-50), SpawnMatch, SpawnEvolve,
                () => Timer(3f), Explain0, Board, Water, Battle, ToggleMute
            };
            for (int i = 0; i < labels.Length; i++)
            {
                int col = i % 7;
                int row = i / 7;
                var btn = NewImage(panel.transform, labels[i], new Color(0.1f, 0.45f, 0.7f, 0.95f));
                var br = btn.rectTransform;
                br.anchorMin = br.anchorMax = new Vector2(0f, 0f);
                br.pivot = new Vector2(0f, 0f);
                br.sizeDelta = new Vector2(58f, 36f);
                br.anchoredPosition = new Vector2(10f + col * 64f, 78f - row * 42f);
                var b = btn.gameObject.AddComponent<Button>();
                int captured = i;
                b.onClick.AddListener(() => actions[captured]());
                var tg = new GameObject("T", typeof(RectTransform));
                tg.transform.SetParent(btn.transform, false);
                Stretch(tg.GetComponent<RectTransform>());
                var tmp = tg.AddComponent<TextMeshProUGUI>();
                tmp.text = labels[i];
                tmp.fontSize = 12f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            }
            Refresh();
        }

        static Image NewImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f1Key.wasPressedThisFrame)
            {
                _visible = !_visible;
                if (_canvas != null) _canvas.gameObject.SetActive(_visible);
            }
            if (!_visible) return;
            if (kb.digit1Key.wasPressedThisFrame) Select(0);
            if (kb.digit2Key.wasPressedThisFrame) Select(1);
            if (kb.digit3Key.wasPressedThisFrame) Select(2);
            if (kb.enterKey.wasPressedThisFrame) Confirm();
            if (kb.f2Key.wasPressedThisFrame) Score(100);
            if (kb.f3Key.wasPressedThisFrame) Score(-50);
            if (kb.f4Key.wasPressedThisFrame) SpawnMatch();
            if (kb.f5Key.wasPressedThisFrame) SpawnEvolve();
            if (kb.f6Key.wasPressedThisFrame) Timer(3f);
            if (kb.f7Key.wasPressedThisFrame) Explain0();
            if (kb.f8Key.wasPressedThisFrame) Board();
            if (kb.f9Key.wasPressedThisFrame) Water();
            if (kb.f10Key.wasPressedThisFrame) Battle();
            Refresh();
        }

        BattleController BattleCtrl => Object.FindAnyObjectByType<BattleController>();

        void Select(int i)
        {
            GameSession.SelectedGuardian = i;
            var b = BattleCtrl;
            if (b != null) b.SelectGuardian(i);
        }

        void Confirm()
        {
            var quiz = Object.FindAnyObjectByType<QuizPresenter>();
            var b = BattleCtrl;
            if (quiz != null && quiz.gameObject.activeInHierarchy)
            {
                var ready = quiz.transform.Find("Ready");
                if (ready != null) ready.GetComponent<Button>().onClick.Invoke();
                return;
            }
            if (b != null) b.ConfirmShot();
        }

        void Score(int d)
        {
            var b = BattleCtrl;
            if (b != null) b.AddScore(d, "debug");
            else
            {
                GameSession.Score += d;
                GameSession.LastScoreDelta = d;
                FluMixLog.Info("Score", "debug " + d);
            }
        }

        void SpawnMatch() { var b = BattleCtrl; if (b != null) b.SpawnMatch(); }
        void SpawnEvolve() { var b = BattleCtrl; if (b != null) b.SpawnMismatchAndEvolve(); }
        void Timer(float s) { var b = BattleCtrl; if (b != null) b.SetTimer(s); else GameSession.TimeLeft = s; }

        void Explain0()
        {
            var b = BattleCtrl;
            if (b == null)
            {
                FluMixFlow.Instance.BeginBattle();
                return;
            }
            var q = FluMixFlow.Instance.Questions.Get(0);
            b.OpenQuiz(false);
        }

        void Board() { FluMixFlow.Instance.GoLeaderboard(); }

        void Water()
        {
            var lobby = Object.FindAnyObjectByType<LobbyController>();
            if (lobby != null) lobby.PlayWaterIsolated();
            else
            {
                GameSession.Previous = FluMixFlow.Instance.State;
                FluMixFlow.Instance.Go(FlowState.Lobby);
                FluMixFlow.Instance.State = FlowState.WaterIntro;
            }
        }

        void Battle() { FluMixFlow.Instance.BeginBattle(); }

        void ToggleMute()
        {
            _muted = !_muted;
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.SetMuted(_muted);
        }

        void Refresh()
        {
            if (_body == null || FluMixFlow.Instance == null) return;
            var b = BattleCtrl;
            string viruses = "";
            if (b != null)
            {
                foreach (var v in b.Alive)
                {
                    if (v == null) continue;
                    viruses += v.Archetype.guardian + " t" + v.Tier + " x" + v.NormalizedX.ToString("0.00") + " ";
                }
            }
            string tail = string.Join("\n", FluMixLog.Tail);
            _body.text =
                "State " + FluMixFlow.Instance.State +
                "\nTime " + GameSession.TimeLeft.ToString("0.0") +
                "  Score " + GameSession.Score +
                "\nLeaks " + GameSession.Leaks +
                "  Q " + GameSession.QuestionIndex +
                "  Opt " + GameSession.SelectedOption +
                "\nGuard " + GameSession.SelectedGuardian +
                "\nViruses " + viruses +
                "\nCue " + GameSession.LastCue +
                "  dScore " + GameSession.LastScoreDelta +
                "\n" + tail;
        }
    }
}
#endif
