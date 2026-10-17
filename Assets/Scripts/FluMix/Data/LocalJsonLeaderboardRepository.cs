using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FluMix.Data
{
    public class LocalJsonLeaderboardRepository : MonoBehaviour, ILeaderboardRepository
    {
        const int Keep = 10;

        LeaderboardEntry[] cache;

        public LeaderboardEntry[] GetTop(int n)
        {
            LeaderboardEntry[] all = Load();
            if (n < 0)
                n = 0;
            if (n > all.Length)
                n = all.Length;

            var top = new LeaderboardEntry[n];
            for (int i = 0; i < n; i++)
                top[i] = all[i];
            return top;
        }

        public int Submit(string name, int score)
        {
            var list = new List<LeaderboardEntry>(Load());
            var entry = new LeaderboardEntry { name = name, score = score };
            list.Add(entry);
            list.Sort(CompareScoreDesc);
            int rank = list.IndexOf(entry) + 1;
            if (list.Count > Keep)
                list.RemoveRange(Keep, list.Count - Keep);

            cache = list.ToArray();
            Write(cache);
            return rank;
        }

        public void Reload()
        {
            cache = null;
            Load();
        }

        LeaderboardEntry[] Load()
        {
            if (cache != null)
                return cache;

            FluMixPaths.EnsureCopied();
            string json = File.ReadAllText(FluMixPaths.Persistent(FluMixPaths.LeaderboardFile));
            LeaderboardFile file = JsonUtility.FromJson<LeaderboardFile>(json);
            cache = file != null && file.entries != null ? file.entries : new LeaderboardEntry[0];
            return cache;
        }

        static void Write(LeaderboardEntry[] entries)
        {
            FluMixPaths.EnsureCopied();
            var file = new LeaderboardFile { entries = entries };
            File.WriteAllText(FluMixPaths.Persistent(FluMixPaths.LeaderboardFile), JsonUtility.ToJson(file, true));
        }

        static int CompareScoreDesc(LeaderboardEntry a, LeaderboardEntry b)
        {
            int score = b.score.CompareTo(a.score);
            if (score != 0)
                return score;
            return 0;
        }
    }
}
