using System;

namespace FluMix.Data
{
    [Serializable]
    public class QuestionData
    {
        public string id;
        public string prompt;
        public string[] options;
        public int correctIndex;
        public string explanation;
        public string reference;
    }

    [Serializable]
    public class QuestionFile
    {
        public QuestionData[] questions;
    }

    [Serializable]
    public class PlayerRecord
    {
        public string username;
        public string password;
        public string displayName;
    }

    [Serializable]
    public class PlayerFile
    {
        public PlayerRecord[] players;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public int score;
    }

    [Serializable]
    public class LeaderboardFile
    {
        public LeaderboardEntry[] entries;
    }
}
