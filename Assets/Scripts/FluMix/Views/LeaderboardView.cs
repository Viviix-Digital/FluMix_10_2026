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

            LeaderboardEntry[] entries = services.Leaderboard.GetTop(10);
            int rowIndex = 0;
            for (int i = 0; i < rowsRoot.childCount; i++)
            {
                Transform row = rowsRoot.GetChild(i);
                if (!IsScoreRow(row.name))
                    continue;

                bool visible = rowIndex < entries.Length;
                row.gameObject.SetActive(visible);
                if (visible)
                    FillRow(row, entries[rowIndex], rowIndex);
                rowIndex++;
            }
        }

        static bool IsScoreRow(string name)
        {
            return name.StartsWith("Top") || name.StartsWith("Label");
        }

        static void FillRow(Transform row, LeaderboardEntry entry, int index)
        {
            TMP_Text[] labels = row.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null)
                    continue;
                string objectName = label.gameObject.name;
                if (objectName.Contains("Player Name"))
                    label.text = entry != null ? entry.name : string.Empty;
                else if (objectName.Contains("Score"))
                    label.text = ScoreKeeper.FormatScore(entry != null ? entry.score : 0);
                else if (objectName.Contains("Rank"))
                    label.text = (index + 1).ToString("00");
            }
        }

        public void PlayAgain()
        {
            if (flow != null)
                flow.PlayAgain();
        }
    }
}
