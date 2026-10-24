using System;
using FluMix.Api;

namespace FluMix.Data
{
    [Serializable]
    public class QuestionData
    {
        public string id;
        public string codeId;
        public string prompt;
        public string[] options;
        public string[] optionKeys;
        public int correctIndex;
        public string explanation;
        public string reference;
        public int points;
        [NonSerialized] public QuestionDto source;
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
        public string uuid;
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
        public int rank;
        public bool isMe;
    }

    [Serializable]
    public class LeaderboardFile
    {
        public LeaderboardEntry[] entries;
    }
}
