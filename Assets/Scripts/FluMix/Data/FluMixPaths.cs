using System.IO;
using UnityEngine;

namespace FluMix.Data
{
    public static class FluMixPaths
    {
        public const string QuestionsFile = "questions.json";
        public const string PlayersFile = "players.json";
        public const string LeaderboardFile = "leaderboard.json";

        public static string PersistentDir => Path.Combine(Application.persistentDataPath, "FluMix");

        public static string StreamingDir => Path.Combine(Application.streamingAssetsPath, "FluMix");

        public static void EnsureCopied()
        {
            Directory.CreateDirectory(PersistentDir);
            CopyIfMissing(QuestionsFile);
            CopyIfMissing(PlayersFile);
            CopyIfMissing(LeaderboardFile);
        }

        public static string Persistent(string fileName)
        {
            return Path.Combine(PersistentDir, fileName);
        }

        static void CopyIfMissing(string fileName)
        {
            string dest = Persistent(fileName);
            if (File.Exists(dest))
                return;

            string src = Path.Combine(StreamingDir, fileName);
            if (!File.Exists(src))
            {
                Debug.LogError("FluMix missing streaming file: " + src);
                return;
            }

            File.Copy(src, dest);
        }
    }
}
