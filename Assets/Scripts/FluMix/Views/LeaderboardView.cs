using System.Collections;
using FluMix.Api;
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

        Coroutine refreshRoutine;

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
            // Scene placeholders must not show before the first API/local fill.
            ClearRows();
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
            if (refreshRoutine != null)
                StopCoroutine(refreshRoutine);
            refreshRoutine = StartCoroutine(RefreshRoutine());
        }

        /// <summary>Load xong (API hoặc local) rồi mới return — dùng trước khi mở màn Leaderboard.</summary>
        public IEnumerator RefreshRoutine()
        {
            if (rowsRoot == null || services == null)
                yield break;

            ClearRows();

            if (services.UseOnlineApi && services.Api != null && services.Api.IsAuthenticated)
            {
                LeaderboardDto board = null;
                string err = null;
                yield return services.Api.Leaderboard(10, b => board = b, (h, c, m) => err = m);

                if (board == null)
                {
                    Debug.LogWarning("[FluMist] Leaderboard failed: " + err);
                    ClearRows();
                    refreshRoutine = null;
                    yield break;
                }

                int count = board.top != null ? board.top.Length : 0;
                var entries = new LeaderboardEntry[count];
                for (int i = 0; i < count; i++)
                {
                    RankRowDto row = board.top[i];
                    entries[i] = new LeaderboardEntry
                    {
                        name = row != null ? row.displayName : string.Empty,
                        score = row != null ? row.totalScore : 0,
                        rank = row != null ? row.rank : i + 1,
                        isMe = row != null && row.isMe
                    };
                }

                ApplyEntries(entries);
            }
            else
            {
                LeaderboardEntry[] entries = services.Leaderboard != null
                    ? services.Leaderboard.GetTop(10)
                    : System.Array.Empty<LeaderboardEntry>();
                ApplyEntries(entries);
            }

            refreshRoutine = null;
        }

        void ClearRows()
        {
            if (rowsRoot == null)
                return;

            for (int i = 0; i < rowsRoot.childCount; i++)
            {
                Transform row = rowsRoot.GetChild(i);
                if (!IsScoreRow(row.name))
                    continue;

                row.gameObject.SetActive(false);
                TMP_Text[] labels = row.GetComponentsInChildren<TMP_Text>(true);
                for (int t = 0; t < labels.Length; t++)
                {
                    TMP_Text label = labels[t];
                    if (label == null)
                        continue;
                    string objectName = label.gameObject.name;
                    if (objectName.Contains("Player Name") || objectName.Contains("Score") || objectName.Contains("Rank"))
                        label.text = string.Empty;
                }
            }

            ApplyBottomPadding(0);
        }

        void ApplyEntries(LeaderboardEntry[] entries)
        {
            if (rowsRoot == null)
                return;

            if (entries == null)
                entries = System.Array.Empty<LeaderboardEntry>();

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

            ApplyBottomPadding(entries.Length);
        }

        /// <summary>
        /// Neo bảng lên trên bằng padding.bottom VLG.
        /// Neo: 2 người = 424, full slot = 0; các mức khác nội suy tuyến tính.
        /// </summary>
        void ApplyBottomPadding(int visibleCount)
        {
            VerticalLayoutGroup layout = rowsRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                return;

            int fullCount = CountScoreSlots();
            if (fullCount < 2)
                fullCount = 10;

            const int bottomAtTwo = 424;
            int bottom;
            if (visibleCount >= fullCount)
                bottom = 0;
            else
                bottom = Mathf.RoundToInt(bottomAtTwo * (fullCount - visibleCount) / (float)(fullCount - 2));

            RectOffset padding = layout.padding;
            if (padding.bottom == bottom)
                return;

            padding.bottom = bottom;
            layout.padding = padding;
            LayoutRebuilder.MarkLayoutForRebuild(rowsRoot);
        }

        int CountScoreSlots()
        {
            int count = 0;
            for (int i = 0; i < rowsRoot.childCount; i++)
            {
                if (IsScoreRow(rowsRoot.GetChild(i).name))
                    count++;
            }
            return count;
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
                {
                    int rank = entry != null && entry.rank > 0 ? entry.rank : index + 1;
                    label.text = rank.ToString("00");
                }
            }
        }

        public void PlayAgain()
        {
            if (flow != null)
                flow.PlayAgain();
        }
    }
}
