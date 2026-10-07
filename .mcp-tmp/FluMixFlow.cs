using UnityEngine;
using UnityEngine.SceneManagement;

namespace FluMix
{
    [DefaultExecutionOrder(-200)]
    public class FluMixFlow : MonoBehaviour
    {
        public static FluMixFlow Instance { get; private set; }

        public FlowState State = FlowState.Boot;
        public StringTable Strings;
        public QuestionBank Questions;
        public RoundRules Rules;
        public UiMotionConfig Motion;
        public AudioBank AudioBank;
        public VirusArchetype Green;
        public VirusArchetype Blue;
        public VirusArchetype Pink;
        public UnityEngine.Audio.AudioMixer Mixer;

        public VirusArchetype[] Archetypes
        {
            get { return new[] { Green, Blue, Pink }; }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            FluMixAudio.Ensure(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugOverlay.Ensure(this);
#endif
        }

        void Start()
        {
            if (gameObject.scene.name == "Boot")
                Go(FlowState.Login, true);
        }

        public void Go(FlowState next, bool log = true)
        {
            if (next != FlowState.WaterIntro && next != FlowState.Result)
                GameSession.Previous = State;
            State = next;
            if (log) FluMixLog.Info("Flow", next.ToString());
            string scene = SceneFor(next);
            if (!string.IsNullOrEmpty(scene) && SceneManager.GetActiveScene().name != scene)
                SceneManager.LoadScene(scene);
            if (FluMixAudio.Instance != null)
                FluMixAudio.Instance.OnFlow(next);
        }

        public static string SceneFor(FlowState state)
        {
            switch (state)
            {
                case FlowState.Boot: return "Boot";
                case FlowState.Login: return "Login";
                case FlowState.Lobby: return "Lobby";
                case FlowState.HowToPlay: return "HowToPlay";
                case FlowState.WaterIntro: return "Lobby";
                case FlowState.Battle: return "Battle";
                case FlowState.Result: return "Battle";
                case FlowState.Leaderboard: return "Leaderboard";
                default: return "Login";
            }
        }

        public void GoLogin() => Go(FlowState.Login);
        public void GoLobby() => Go(FlowState.Lobby);
        public void GoHowTo() => Go(FlowState.HowToPlay);
        public void GoLeaderboard() => Go(FlowState.Leaderboard);

        public void BeginBattle()
        {
            if (Rules != null) GameSession.ResetRound(Rules.roundSeconds);
            else GameSession.ResetRound(120f);
            Go(FlowState.Battle);
        }

        public VirusArchetype ArchetypeFor(GuardianId id)
        {
            if (id == GuardianId.G) return Green;
            if (id == GuardianId.A) return Blue;
            return Pink;
        }
    }
}
