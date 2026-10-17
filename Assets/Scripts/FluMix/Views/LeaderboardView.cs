using FluMix.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public class LeaderboardView : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] AppServices services;
        [SerializeField] RectTransform rowsRoot;
        [SerializeField] Button playAgainButton;
        [SerializeField] Sprite rank1;
        [SerializeField] Sprite rank2;
        [SerializeField] Sprite rank3;

        public void Bind(GameFlow gameFlow, AppServices appServices, RectTransform rows, Button again, Sprite first, Sprite second, Sprite third)
        {
            flow = gameFlow;
            services = appServices;
            rowsRoot = rows;
            playAgainButton = again;
            rank1 = first;
            rank2 = second;
            rank3 = third;
            Wire();
        }

        void Awake()
        {
            Wire();
        }

        void OnEnable()
        {
            Refresh();
        }

        void Wire()
        {
            if (playAgainButton == null)
                return;
            playAgainButton.onClick.RemoveListener(PlayAgain);
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        public void Refresh()
        {
            if (rowsRoot == null || services == null)
                return;

            for (int i = rowsRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(rowsRoot.GetChild(i).gameObject);

            LeaderboardEntry[] entries = services.Leaderboard.GetTop(10);
            float height = rowsRoot.rect.height;
            if (height < 10f)
                height = 640f;
            float rowHeight = height / 10f;

            for (int i = 0; i < entries.Length; i++)
            {
                CreateRow(entries[i], i, rowHeight);
            }
        }

        void CreateRow(LeaderboardEntry entry, int index, float rowHeight)
        {
            var row = FluMixUi.CreateRect(rowsRoot, "Row" + (index + 1), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            row.sizeDelta = new Vector2(-40f, rowHeight - 6f);
            row.anchoredPosition = new Vector2(0f, -index * rowHeight - 4f);

            var background = row.gameObject.AddComponent<Image>();
            background.sprite = FluMixUi.White;
            background.color = RowColor(index);
            background.raycastTarget = false;

            if (index < 3)
            {
                Sprite icon = index == 0 ? rank1 : index == 1 ? rank2 : rank3;
                if (icon != null)
                {
                    Image badge = FluMixUi.CreateImage(row, "Badge", icon, new Vector2(rowHeight - 10f, rowHeight - 10f), false);
                    badge.preserveAspect = true;
                    badge.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                    badge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                    badge.rectTransform.pivot = new Vector2(0f, 0.5f);
                    badge.rectTransform.anchoredPosition = new Vector2(16f, 0f);
                }
            }
            else
            {
                TextMeshProUGUI rank = FluMixUi.CreateText(row, "Rank", (index + 1).ToString("00"), 28f, TextAlignmentOptions.Center);
                Stretch(rank.rectTransform, 12f, 150f);
            }

            string playerName = entry != null ? entry.name : string.Empty;
            TextMeshProUGUI name = FluMixUi.CreateText(row, "Name", playerName, 30f, TextAlignmentOptions.Left);
            Stretch(name.rectTransform, 170f, rowsRoot.rect.width * 0.35f);
            name.color = Color.white;

            int value = entry != null ? entry.score : 0;
            TextMeshProUGUI score = FluMixUi.CreateText(row, "Score", ScoreKeeper.FormatScore(value), 30f, TextAlignmentOptions.Right);
            score.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            score.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            score.rectTransform.pivot = new Vector2(1f, 0.5f);
            score.rectTransform.sizeDelta = new Vector2(220f, rowHeight);
            score.rectTransform.anchoredPosition = new Vector2(-24f, 0f);
        }

        public void PlayAgain()
        {
            if (flow != null)
                flow.PlayAgain();
        }

        static void Stretch(RectTransform rect, float x, float width)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, 48f);
        }

        static Color RowColor(int index)
        {
            switch (index)
            {
                case 0: return new Color(0.08f, 0.28f, 0.72f, 0.92f);
                case 1: return new Color(0.15f, 0.55f, 0.22f, 0.92f);
                case 2: return new Color(0.55f, 0.58f, 0.62f, 0.92f);
                default: return new Color(0.05f, 0.16f, 0.42f, 0.8f);
            }
        }
    }
}
