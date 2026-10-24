using System;

namespace FluMix.Api
{
    [Serializable]
    public class ApiResponse<T>
    {
        public int statusCode;
        public string message;
        public T data;
        public string code;
    }

    [Serializable]
    public class PlayerDto
    {
        public string uuid;
        public string username;
        public string fullName;
        public string phone;
        public string email;
        public string projectSlug;
    }

    [Serializable]
    public class AuthDto
    {
        public PlayerDto player;
        public string accessToken;
        public string refreshToken;
    }

    [Serializable]
    public class OptionDto
    {
        public string key;
        public string label;
    }

    [Serializable]
    public class QuestionDto
    {
        public string codeId;
        public string type;
        public string content;
        public string imageUrl;
        public string explanation;
        public OptionDto[] options;
        public int points;
        public int orderNo;
        public int blanks;
    }

    [Serializable]
    public class SettingsDto
    {
        public bool revealAnswer;
        public bool allowRepeatAnswers;
        public string scoringMode;
        public int wrongAnswerPoints;
        public int? maxAnswersPerPlay;
        public int? maxPlays;
        public int? maxPlaysPerDay;
    }

    [Serializable]
    public class ProjectDto
    {
        public string slug;
        public string name;
        public string status;
        public SettingsDto settings;
    }

    [Serializable]
    public class QuestionsDto
    {
        public ProjectDto project;
        public QuestionDto[] questions;
    }

    [Serializable]
    public class PlayDto
    {
        public string playId;
        public string clientPlayId;
        public string status;
        public string endReason;
        public string startedAt;
        public string finishedAt;
        public int answerScore;
        public int bonusScore;
        public int extraScore;
        public int score;
    }

    [Serializable]
    public class StartPlayDto
    {
        public PlayDto play;
        public bool resumed;
    }

    [Serializable]
    public class AnswerResultDto
    {
        public string codeId;
        public string correctAnswer;
        public bool correct;
        public bool duplicate;
        public int points;
        public int basePoints;
        public int multiplier;
    }

    [Serializable]
    public class RankRowDto
    {
        public int rank;
        public int totalScore;
        public string displayName;
        public bool isMe;
        public bool inTop;
    }

    [Serializable]
    public class LeaderboardDto
    {
        public string asOf;
        public RankRowDto[] top;
        public RankRowDto me;
    }

    [Serializable]
    public class ProgressResultDto
    {
        public string eventId;
        public string leaderboardError;
        public bool replayed;
        public bool completed;
        public AnswerResultDto answer;
        public int extraPoints;
        public int totalScore;
        public PlayDto play;
        public LeaderboardDto leaderboard;
    }

    [Serializable]
    public class RemainingDto
    {
        public int? total;
        public int? today;
    }

    [Serializable]
    public class MeDto
    {
        public PlayerDto player;
        public ProjectDto project;
        public int totalScore;
        public int plays;
        public int playsToday;
        public RemainingDto remaining;
    }
}
