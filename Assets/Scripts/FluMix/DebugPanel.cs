using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FluMix
{
    public class DebugPanel : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text info;

        bool built;

        public void Bind(GameFlow gameFlow, GameObject panelRoot)
        {
            flow = gameFlow;
            panel = panelRoot;
        }

        void Awake()
        {
            if (!Allowed)
            {
                if (panel != null)
                    panel.SetActive(false);
                enabled = false;
                return;
            }

            if (panel != null && panel.transform.childCount == 0)
                Build();
            if (panel != null)
                panel.SetActive(false);
        }

        void Update()
        {
            if (!Allowed || flow == null)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame && panel != null)
                panel.SetActive(!panel.activeSelf);

            if (info != null && panel != null && panel.activeSelf)
            {
                string explain = flow.Config != null && flow.Config.explainHoldSeconds > 0f ? "bật" : "tắt";
                string timer = flow.TimerLocked ? "đang khóa" : "đang chạy";
                info.text = flow.DescribeVirus() + "\nGiải thích " + explain + "\nTimer " + timer;
            }
        }

        public void Build()
        {
            if (panel == null || built)
                return;

            var background = FluMixUi.CreateRect(panel.transform, "Background", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            var image = background.gameObject.AddComponent<Image>();
            image.sprite = FluMixUi.White;
            image.color = new Color(0.02f, 0.05f, 0.12f, 0.92f);

            var grid = FluMixUi.CreateRect(panel.transform, "Buttons", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            grid.offsetMin = new Vector2(12f, 110f);
            grid.offsetMax = new Vector2(-12f, -12f);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(200f, 36f);
            layout.spacing = new Vector2(8f, 6f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.childAlignment = TextAnchor.UpperLeft;

            Add(grid, "+100", () => flow.DebugAddScore(flow.Config.correctHeroScore));
            Add(grid, "+1000", () => flow.DebugAddScore(flow.Config.debugBumpScore));
            Add(grid, "−50", () => flow.DebugAddScore(flow.Config.wrongHeroScore));
            Add(grid, "Mốc 500", () => flow.DebugSetScore(Milestone(0)));
            Add(grid, "Mốc 1000", () => flow.DebugSetScore(Milestone(1)));
            Add(grid, "Mốc 2000", () => flow.DebugSetScore(Milestone(2)));
            Add(grid, "Mốc 5000", () => flow.DebugSetScore(Milestone(3)));
            Add(grid, "Mốc 10000", () => flow.DebugSetScore(Milestone(4)));
            Add(grid, "Reset điểm", () => flow.DebugResetScore());
            Add(grid, "Virus xanh", () => flow.DebugSpawn(VirusType.Green));
            Add(grid, "Virus cam", () => flow.DebugSpawn(VirusType.Orange));
            Add(grid, "Virus đỏ", () => flow.DebugSpawn(VirusType.Red));
            Add(grid, "Chạm thành", () => flow.DebugVirusReachWall());
            Add(grid, "Ép boss", () => flow.DebugForceBoss());
            Add(grid, "Ép đúng", () => flow.DebugForceAnswer(ForcedAnswer.Correct));
            Add(grid, "Ép sai", () => flow.DebugForceAnswer(ForcedAnswer.Wrong));
            Add(grid, "Hết giờ câu", () => flow.DebugForceAnswer(ForcedAnswer.Timeout));
            Add(grid, "Còn 5 giây", () => flow.DebugSetRemaining(flow.Config.debugQuickSeconds));
            Add(grid, "Khóa timer", () => flow.DebugSetTimerLocked(!flow.TimerLocked));
            Add(grid, "Login", () => flow.DebugShow(FluMixScreen.Login));
            Add(grid, "Luật", () => flow.DebugShow(FluMixScreen.Rules));
            Add(grid, "Battle", () => flow.DebugShow(FluMixScreen.Battle));
            Add(grid, "Kết quả", () => flow.DebugShow(FluMixScreen.Result));
            Add(grid, "Xếp hạng", () => flow.DebugShow(FluMixScreen.Leaderboard));
            Add(grid, "Tải JSON", () => flow.DebugReloadJson());
            Add(grid, "Giải thích", () => flow.ToggleExplainHold());

            info = FluMixUi.CreateText(panel.transform, "VirusInfo", "Virus", 22f, TextAlignmentOptions.TopLeft);
            info.rectTransform.anchorMin = new Vector2(0f, 0f);
            info.rectTransform.anchorMax = new Vector2(1f, 0f);
            info.rectTransform.pivot = new Vector2(0.5f, 0f);
            info.rectTransform.sizeDelta = new Vector2(-24f, 90f);
            info.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            info.raycastTarget = false;
            built = true;
        }

        void Add(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Image image = FluMixUi.CreateImage(parent, label, FluMixUi.White, new Vector2(200f, 36f), true);
            image.color = new Color(0.1f, 0.35f, 0.75f, 1f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            TextMeshProUGUI text = FluMixUi.CreateText(image.transform, "Label", label, 20f, TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.color = Color.white;
        }

        int Milestone(int index)
        {
            if (flow == null || flow.Config == null || flow.Config.bossScoreMilestones == null)
                return 0;
            if (index < 0 || index >= flow.Config.bossScoreMilestones.Length)
                return 0;
            return flow.Config.bossScoreMilestones[index];
        }

        static bool Allowed => Application.isEditor || Debug.isDebugBuild;
    }
}
