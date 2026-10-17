using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FluMix
{
    public class ScoreKeeper : MonoBehaviour
    {
        static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

        [SerializeField] GameConfig config;

        readonly HashSet<int> usedMilestones = new HashSet<int>();
        readonly Queue<int> pendingMilestones = new Queue<int>();

        public int Score { get; private set; }
        public event Action<int> ScoreChanged;

        public bool HasPendingMilestone => pendingMilestones.Count > 0;

        public void Bind(GameConfig gameConfig)
        {
            config = gameConfig;
        }

        public static string FormatScore(int value)
        {
            return value.ToString("N0", Vietnamese);
        }

        public static string FormatClock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds - 0.001f));
            return string.Format("{0:00}:{1:00}", total / 60, total % 60);
        }

        public static string FormatElapsed(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return string.Format("{0:00}:{1:00}", total / 60, total % 60);
        }

        public void Add(int delta)
        {
            SetScore(Score + delta);
        }

        public void SetScore(int value)
        {
            Score = value;
            CollectMilestones();
            ScoreChanged?.Invoke(Score);
        }

        public void ResetScore()
        {
            Score = 0;
            usedMilestones.Clear();
            pendingMilestones.Clear();
            ScoreChanged?.Invoke(Score);
        }

        public bool TryDequeueMilestone(out int milestone)
        {
            if (pendingMilestones.Count == 0)
            {
                milestone = 0;
                return false;
            }

            milestone = pendingMilestones.Dequeue();
            usedMilestones.Add(milestone);
            return true;
        }

        public int PointsUntilNextMilestone()
        {
            if (config == null || config.bossScoreMilestones == null)
                return 0;

            int best = int.MaxValue;
            for (int i = 0; i < config.bossScoreMilestones.Length; i++)
            {
                int milestone = config.bossScoreMilestones[i];
                if (usedMilestones.Contains(milestone) || PendingContains(milestone))
                    continue;
                if (milestone > Score && milestone < best)
                    best = milestone;
            }

            if (best == int.MaxValue)
                return 0;
            return best - Score;
        }

        void CollectMilestones()
        {
            if (config == null || config.bossScoreMilestones == null)
                return;

            for (int i = 0; i < config.bossScoreMilestones.Length; i++)
            {
                int milestone = config.bossScoreMilestones[i];
                if (Score < milestone || usedMilestones.Contains(milestone) || PendingContains(milestone))
                    continue;
                pendingMilestones.Enqueue(milestone);
            }
        }

        bool PendingContains(int milestone)
        {
            foreach (int pending in pendingMilestones)
            {
                if (pending == milestone)
                    return true;
            }

            return false;
        }
    }
}
