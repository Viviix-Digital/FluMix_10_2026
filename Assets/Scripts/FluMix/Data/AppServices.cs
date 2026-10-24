using FluMix.Api;
using UnityEngine;

namespace FluMix.Data
{
    [DefaultExecutionOrder(-200)]
    public class AppServices : MonoBehaviour
    {
        public static AppServices Instance { get; private set; }

        [SerializeField] bool useOnlineApi = true;
        [SerializeField] LocalJsonQuestionRepository questions;
        [SerializeField] LocalJsonPlayerRepository players;
        [SerializeField] LocalJsonLeaderboardRepository leaderboard;
        [SerializeField] FluMistApi api;
        [SerializeField] FluMistRoundSession roundSession;

        QuestionData[] cachedQuestions;
        QuestionsDto lastQuestionsDto;

        public bool UseOnlineApi => useOnlineApi;
        public IQuestionRepository Questions => questions;
        public IPlayerRepository Players => players;
        public ILeaderboardRepository Leaderboard => leaderboard;
        public FluMistApi Api => api;
        public FluMistRoundSession RoundSession => roundSession;
        public QuestionData[] CachedQuestions => cachedQuestions;
        public QuestionsDto LastQuestionsDto => lastQuestionsDto;

        void Awake()
        {
            Instance = this;
            FluMixPaths.EnsureCopied();
            if (questions == null)
                questions = GetComponent<LocalJsonQuestionRepository>() ?? gameObject.AddComponent<LocalJsonQuestionRepository>();
            if (players == null)
                players = GetComponent<LocalJsonPlayerRepository>() ?? gameObject.AddComponent<LocalJsonPlayerRepository>();
            if (leaderboard == null)
                leaderboard = GetComponent<LocalJsonLeaderboardRepository>() ?? gameObject.AddComponent<LocalJsonLeaderboardRepository>();
            if (api == null)
                api = GetComponent<FluMistApi>() ?? gameObject.AddComponent<FluMistApi>();
            if (roundSession == null)
                roundSession = GetComponent<FluMistRoundSession>() ?? gameObject.AddComponent<FluMistRoundSession>();
            roundSession.Bind(api);
        }

        public void SetCachedQuestions(QuestionsDto dto)
        {
            lastQuestionsDto = dto;
            cachedQuestions = dto != null ? QuestionMapper.FromServer(dto.questions) : null;
        }

        public QuestionData[] GetPlayQuestions()
        {
            if (useOnlineApi && cachedQuestions != null && cachedQuestions.Length > 0)
                return cachedQuestions;
            return questions != null ? questions.LoadAll() : null;
        }

        public void ReloadFromPersistent()
        {
            FluMixPaths.EnsureCopied();
            questions.Reload();
            players.Reload();
            leaderboard.Reload();
        }
    }
}
