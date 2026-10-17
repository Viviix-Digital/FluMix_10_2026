using FluMix.Data;
using TMPro;
using UnityEngine;

namespace FluMix
{
    [DefaultExecutionOrder(-50)]
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] AppServices services;
        [SerializeField] ScreenRouter router;
        [SerializeField] ScoreKeeper score;
        [SerializeField] BattleController battle;
        [SerializeField] BossSequence boss;
        [SerializeField] QuestionPresenter questions;
        [SerializeField] FxPlayer fx;
        [SerializeField] ResultView resultView;
        [SerializeField] LeaderboardView leaderboardView;
        [SerializeField] TMP_Text timeText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] UnityEngine.UI.Button winButton;
        [SerializeField] TMP_FontAsset font;

        public void SetWinButton(UnityEngine.UI.Button button)
        {
            winButton = button;
        }

        PlayerRecord player;
        float remaining;
        float elapsed;
        float explainDefault;
        bool timerPaused;
        bool timerLocked;
        bool roundActive;
        bool submitted;
        int lastRank;
        int questionsAnswered;
        int questionsCorrect;

        public GameConfig Config => config;
        public FluMixScreen Screen { get; private set; }
        public int QuestionsAnswered => questionsAnswered;
        public int QuestionsCorrect => questionsCorrect;
        public bool IsRoundOver => config != null && (questionsAnswered >= config.maxQuestions || remaining <= 0f);
        public string PlayerName => player != null ? player.displayName : "CHIẾN BINH 01";

        void Awake()
        {
            if (font != null)
                FluMixUi.Font = font;

            if (config != null)
            {
                explainDefault = config.explainHoldSeconds;
                config = Instantiate(config);
                float cap = config.maxDeltaTime > 0f ? config.maxDeltaTime : 0.1f;
                Time.maximumDeltaTime = cap;
                if (score != null)
                    score.Bind(config);
                if (battle != null)
                    battle.AdoptConfig(config);
                if (boss != null)
                    boss.AdoptConfig(config);
                if (questions != null)
                    questions.AdoptConfig(config);
                if (fx != null)
                    fx.AdoptConfig(config);
            }

            if (score != null)
                score.ScoreChanged += OnScoreChanged;
        }

        void Start()
        {
            if (winButton != null)
            {
                winButton.onClick.RemoveListener(OnWinBannerPressed);
                winButton.onClick.AddListener(OnWinBannerPressed);
            }

            SetScreen(FluMixScreen.Login);
            RefreshHud();
        }

        public void OnWinBannerPressed()
        {
            if (boss != null)
                boss.NotifyBannerPressed();
        }

        void OnDestroy()
        {
            if (score != null)
                score.ScoreChanged -= OnScoreChanged;
        }

        void Update()
        {
            if (!roundActive || config == null)
                return;

            bool bossScreen = Screen == FluMixScreen.Warning
                || Screen == FluMixScreen.BossIntro
                || Screen == FluMixScreen.Question
                || Screen == FluMixScreen.BossResolve;

            if (Screen == FluMixScreen.Battle && !timerPaused && !timerLocked)
            {
                float dt = Time.deltaTime;
                remaining -= dt;
                elapsed += dt;
                if (remaining < 0f)
                    remaining = 0f;
                RefreshHud();
                if (remaining <= 0f)
                {
                    EndRound();
                    return;
                }
            }
            else if (Screen == FluMixScreen.Battle || bossScreen)
            {
                RefreshHud();
            }

            if (Screen == FluMixScreen.Battle && boss != null && !boss.IsRunning && battle != null && !battle.IsBusy && score != null && score.HasPendingMilestone && !IsRoundOver)
                StartBoss(false);
        }

        public void OnLoggedIn(PlayerRecord record)
        {
            player = record;
            SetScreen(FluMixScreen.Rules);
        }

        public void StartRound()
        {
            if (boss != null)
                boss.Abort();
            if (fx != null)
                fx.HideImmediate();
            if (score != null)
                score.ResetScore();
            if (questions != null)
                questions.ResetOrder();

            questionsAnswered = 0;
            questionsCorrect = 0;
            remaining = config != null ? config.roundSeconds : 0f;
            elapsed = 0f;
            timerPaused = false;
            timerLocked = false;
            roundActive = true;
            submitted = false;
            lastRank = 0;
            if (battle != null)
                battle.BeginRound();
            SetScreen(FluMixScreen.Battle);
            RefreshHud();
        }

        public void PlayAgain()
        {
            StartRound();
        }

        public void ResumeBattle()
        {
            timerPaused = false;
            if (battle != null)
            {
                battle.SetPaused(false);
                battle.FillIfNeeded();
            }

            SetScreen(FluMixScreen.Battle);
            if (IsRoundOver)
                EndRound();
        }

        public void EndRound()
        {
            if (boss != null && boss.IsRunning)
                boss.Abort();
            if (battle != null)
                battle.StopRound();
            if (fx != null)
                fx.HideImmediate();

            roundActive = false;
            timerPaused = true;
            if (!submitted && services != null)
            {
                lastRank = services.Leaderboard.Submit(PlayerName, score != null ? score.Score : 0);
                submitted = true;
            }

            if (resultView != null)
                resultView.Show(score != null ? score.Score : 0, elapsed, questionsCorrect, questionsAnswered, lastRank);
            SetScreen(FluMixScreen.Result);
        }

        public void OpenLeaderboard()
        {
            if (leaderboardView != null)
                leaderboardView.Refresh();
            SetScreen(FluMixScreen.Leaderboard);
        }

        public void SetScreen(FluMixScreen screen)
        {
            Screen = screen;
            if (router != null)
                router.Show(screen);
        }

        public void SetTimerPaused(bool paused)
        {
            timerPaused = paused;
        }

        public void RegisterQuestion(bool correct)
        {
            questionsAnswered++;
            if (correct)
                questionsCorrect++;
        }

        public void SetExplainHold(float seconds)
        {
            if (config != null)
                config.explainHoldSeconds = seconds;
        }

        public void ToggleExplainHold()
        {
            if (config == null)
                return;
            bool enabled = config.explainHoldSeconds > config.explainDisabledSeconds;
            config.explainHoldSeconds = enabled ? config.explainDisabledSeconds : explainDefault;
        }

        public string DescribeVirus()
        {
            if (battle == null)
                return "Virus: không có";
            return battle.DescribeAlive();
        }

        [ContextMenu("Debug/Force Boss")]
        public void DebugForceBoss()
        {
            if (!Application.isPlaying)
                return;
            StartBoss(true);
        }

        [ContextMenu("Debug/Add Milestone Score")]
        public void DebugAddMilestoneScore()
        {
            if (!Application.isPlaying || score == null)
                return;
            int delta = score.PointsUntilNextMilestone();
            if (delta <= 0 && config != null && config.bossScoreMilestones != null && config.bossScoreMilestones.Length > 0)
                delta = config.bossScoreMilestones[0];
            score.Add(delta);
        }

        public void DebugAddScore(int delta)
        {
            if (score != null)
                score.Add(delta);
        }

        public void DebugSetScore(int value)
        {
            if (score != null)
                score.SetScore(value);
        }

        public void DebugResetScore()
        {
            if (score != null)
                score.ResetScore();
            RefreshHud();
        }

        public void DebugSpawn(VirusType type)
        {
            if (battle != null)
                battle.Spawn(type);
        }

        public void DebugVirusReachWall()
        {
            if (battle != null)
                battle.ForceCurrentReachWall();
        }

        public void DebugForceAnswer(ForcedAnswer answer)
        {
            if (boss == null)
                return;
            boss.Queue(answer);
            if (!boss.IsRunning)
                StartBoss(true);
        }

        public void DebugSetRemaining(float seconds)
        {
            remaining = seconds;
            RefreshHud();
        }

        public void DebugSetTimerLocked(bool locked)
        {
            timerLocked = locked;
        }

        public bool TimerLocked => timerLocked;

        public void DebugReloadJson()
        {
            if (services != null)
                services.ReloadFromPersistent();
            if (questions != null)
                questions.ResetOrder();
            if (Screen == FluMixScreen.Leaderboard && leaderboardView != null)
                leaderboardView.Refresh();
        }

        public void DebugShow(FluMixScreen screen)
        {
            if (screen == FluMixScreen.Battle)
            {
                if (boss != null)
                    boss.Abort();
                if (fx != null)
                    fx.HideImmediate();
                if (!roundActive)
                    StartRound();
                else
                    ResumeBattle();
                return;
            }

            if (screen == FluMixScreen.Result)
            {
                EndRound();
                return;
            }

            if (screen == FluMixScreen.Leaderboard)
            {
                OpenLeaderboard();
                return;
            }

            if (screen == FluMixScreen.Rules || screen == FluMixScreen.Login)
            {
                if (boss != null)
                    boss.Abort();
                if (battle != null)
                    battle.SetPaused(true);
                SetScreen(screen);
            }
        }

        void StartBoss(bool forced)
        {
            if (boss == null || boss.IsRunning || config == null)
                return;
            if (!roundActive)
                StartRound();
            if (!forced && (score == null || !score.TryDequeueMilestone(out _)))
                return;
            boss.Begin();
        }

        void OnScoreChanged(int value)
        {
            RefreshHud();
        }

        void RefreshHud()
        {
            if (timeText != null)
                timeText.text = ScoreKeeper.FormatClock(remaining);
            if (scoreText != null)
                scoreText.text = ScoreKeeper.FormatScore(score != null ? score.Score : 0);
        }
    }
}
