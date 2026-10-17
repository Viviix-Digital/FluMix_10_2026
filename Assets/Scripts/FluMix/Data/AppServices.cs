using UnityEngine;

namespace FluMix.Data
{
    [DefaultExecutionOrder(-200)]
    public class AppServices : MonoBehaviour
    {
        public static AppServices Instance { get; private set; }

        [SerializeField] LocalJsonQuestionRepository questions;
        [SerializeField] LocalJsonPlayerRepository players;
        [SerializeField] LocalJsonLeaderboardRepository leaderboard;

        public IQuestionRepository Questions => questions;
        public IPlayerRepository Players => players;
        public ILeaderboardRepository Leaderboard => leaderboard;

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
