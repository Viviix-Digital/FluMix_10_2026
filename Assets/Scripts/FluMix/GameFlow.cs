using System.Collections;
using FluMix.Api;
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
        float mePollTimer;
        bool timerPaused;
        bool timerLocked;
        bool roundActive;
        bool submitted;
        bool startingRound;
        bool endingRound;
        int lastRank;
        int lastTotalScore;
        int questionsAnswered;
        int questionsCorrect;
        int lastTickSecond = -1;
        string statusBanner;

        public GameConfig Config => config;
        public FluMixScreen Screen { get; private set; }
        public int QuestionsAnswered => questionsAnswered;
        public int QuestionsCorrect => questionsCorrect;
        public bool IsRoundOver => config != null && (questionsAnswered >= config.maxQuestions || remaining <= 0f);
        public string PlayerName => player != null ? player.displayName : "CHIẾN BINH 01";
        public int LastTotalScore => lastTotalScore;

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

            if (services != null && services.Api != null)
                services.Api.LoggedOut += OnApiLoggedOut;

            if (battle != null && services != null)
                battle.BindRoundSession(services.UseOnlineApi ? services.RoundSession : null);

            RefreshHud();
            StartCoroutine(BootAuth());
        }

        IEnumerator BootAuth()
        {
            SetScreen(FluMixScreen.Login);

            if (services == null || !services.UseOnlineApi || services.Api == null)
                yield break;

            if (!TokenStore.HasSession && !services.Api.IsAuthenticated)
                yield break;

            var loginView = FindFirstObjectByType<LoginView>(FindObjectsInactive.Include);
            if (loginView != null)
                loginView.ShowStatus("Đang đăng nhập…", true);

            MeDto me = null;
            string needLogin = null;
            yield return services.Api.TryRestoreSession(
                m => me = m,
                reason => needLogin = reason);

            if (me == null || me.player == null)
            {
                if (loginView != null)
                    loginView.ShowStatus(
                        string.IsNullOrEmpty(needLogin)
                            ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                            : needLogin,
                        true);
                yield break;
            }

            if (loginView != null)
                loginView.ShowStatus("Đang tải câu hỏi…", true);

            QuestionsDto questions = null;
            string qErr = null;
            yield return services.Api.Questions(
                q => questions = q,
                (h, c, m) => qErr = m);

            if (questions == null || questions.questions == null || questions.questions.Length == 0)
            {
                if (loginView != null)
                    loginView.ShowStatus(
                        (qErr ?? "Không tải được câu hỏi") + " Vui lòng thử lại sau.",
                        true);
                yield break;
            }

            services.SetCachedQuestions(questions);
            OnLoggedIn(new PlayerRecord
            {
                username = me.player.username,
                displayName = string.IsNullOrEmpty(me.player.fullName) ? me.player.username : me.player.fullName,
                uuid = me.player.uuid
            });
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
            if (services != null && services.Api != null)
                services.Api.LoggedOut -= OnApiLoggedOut;
        }

        void OnApiLoggedOut(string reason)
        {
            StopAllCoroutines();
            startingRound = false;
            endingRound = false;
            roundActive = false;
            if (boss != null)
                boss.Abort();
            if (battle != null)
                battle.StopRound();
            if (services != null && services.RoundSession != null)
                services.RoundSession.ResetLocal();
            player = null;
            statusBanner = reason;
            Debug.LogWarning("[FluMist] LoggedOut: " + reason);
            SetScreen(FluMixScreen.Login);
            var loginView = FindFirstObjectByType<LoginView>(FindObjectsInactive.Include);
            if (loginView != null && !string.IsNullOrEmpty(reason))
                loginView.ShowStatus(reason, true);
        }

        void Update()
        {
            PollSession();

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
                int second = Mathf.CeilToInt(remaining);
                if (second != lastTickSecond)
                {
                    if (second > 0 && second <= 5)
                        FluMixSfx.Play("timer_tick");
                    if (second <= 0 && lastTickSecond > 0)
                        FluMixSfx.Play("timer_end");
                    lastTickSecond = second;
                }
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

        void PollSession()
        {
            if (services == null || !services.UseOnlineApi || services.Api == null || !services.Api.IsAuthenticated)
                return;
            if (Screen != FluMixScreen.Rules && Screen != FluMixScreen.Leaderboard && Screen != FluMixScreen.Result)
                return;

            mePollTimer += Time.unscaledDeltaTime;
            if (mePollTimer < 45f)
                return;
            mePollTimer = 0f;
            StartCoroutine(PollMe());
        }

        IEnumerator PollMe()
        {
            yield return services.Api.Me(_ => { }, (h, c, m) =>
            {
                if (h == 401)
                    Debug.LogWarning("[FluMist] /me failed: " + m);
            });
        }

        public void OnLoggedIn(PlayerRecord record)
        {
            player = record;
            mePollTimer = 0f;
            statusBanner = null;
            SetScreen(FluMixScreen.Rules);
        }

        public void StartRound()
        {
            if (startingRound || endingRound)
                return;
            StartCoroutine(StartRoundRoutine());
        }

        IEnumerator StartRoundRoutine()
        {
            startingRound = true;
            if (boss != null)
                boss.Abort();
            if (fx != null)
                fx.HideImmediate();
            if (questions != null)
                questions.ResetOrder();

            questionsAnswered = 0;
            questionsCorrect = 0;
            remaining = config != null ? config.roundSeconds : 0f;
            elapsed = 0f;
            timerPaused = false;
            timerLocked = false;
            submitted = false;
            lastRank = 0;
            lastTotalScore = 0;

            bool online = services != null && services.UseOnlineApi && services.Api != null;
            if (online)
            {
                if (services.CachedQuestions == null || services.CachedQuestions.Length == 0)
                {
                    QuestionsDto dto = null;
                    string err = null;
                    yield return services.Api.Questions(q => dto = q, (h, c, m) => err = m);
                    if (dto == null)
                    {
                        startingRound = false;
                        statusBanner = err ?? "Không tải được câu hỏi";
                        Debug.LogError("[FluMist] " + statusBanner);
                        SetScreen(FluMixScreen.Rules);
                        yield break;
                    }

                    services.SetCachedQuestions(dto);
                }

                services.RoundSession.ResetLocal();
                string startErr = null;
                yield return services.RoundSession.Begin(e => startErr = e);
                if (!services.RoundSession.HasPlay)
                {
                    startingRound = false;
                    statusBanner = startErr ?? "Không bắt đầu được ván";
                    Debug.LogError("[FluMist] " + statusBanner);
                    SetScreen(FluMixScreen.Rules);
                    yield break;
                }

                if (score != null)
                    score.SetScore(services.RoundSession.HudScore);
            }
            else if (score != null)
            {
                score.ResetScore();
            }

            if (battle != null)
                battle.BindRoundSession(online ? services.RoundSession : null);

            roundActive = true;
            SetScreen(FluMixScreen.Battle);
            if (battle != null)
                battle.BeginRound();
            RefreshHud();
            startingRound = false;
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
            if (endingRound || submitted)
            {
                if (submitted && resultView != null)
                {
                    resultView.Show(score != null ? score.Score : 0, elapsed, questionsCorrect, questionsAnswered, lastRank);
                    SetScreen(FluMixScreen.Result);
                }

                return;
            }

            StartCoroutine(EndRoundRoutine());
        }

        IEnumerator EndRoundRoutine()
        {
            endingRound = true;
            if (boss != null && boss.IsRunning)
                boss.Abort();
            if (battle != null)
                battle.StopRound();
            if (fx != null)
                fx.HideImmediate();

            roundActive = false;
            timerPaused = true;

            bool online = services != null && services.UseOnlineApi && services.RoundSession != null && services.RoundSession.HasPlay;
            if (online)
            {
                ProgressResultDto finish = null;
                yield return services.RoundSession.Finish(r => finish = r);

                if (finish == null && services.RoundSession.HasPendingProgress)
                {
                    // retry once
                    yield return services.RoundSession.RetryPending(r => finish = r);
                    if (finish == null || !finish.completed)
                        yield return services.RoundSession.Finish(r => finish = r);
                }

                if (finish != null)
                {
                    if (finish.play != null && score != null)
                        score.SetScore(finish.play.score);
                    lastTotalScore = finish.totalScore;
                    if (finish.leaderboard != null && finish.leaderboard.me != null)
                        lastRank = finish.leaderboard.me.rank;
                    else
                    {
                        LeaderboardDto board = null;
                        yield return services.Api.Leaderboard(10, b => board = b, (h, c, m) => { });
                        if (board != null && board.me != null)
                            lastRank = board.me.rank;
                    }
                }
                else
                {
                    Debug.LogWarning("[FluMist] Finish failed: " + services.RoundSession.LastError);
                    LeaderboardDto board = null;
                    yield return services.Api.Leaderboard(10, b => board = b, (h, c, m) => { });
                    if (board != null && board.me != null)
                    {
                        lastRank = board.me.rank;
                        lastTotalScore = board.me.totalScore;
                    }
                }
            }
            else if (services != null)
            {
                lastRank = services.Leaderboard.Submit(PlayerName, score != null ? score.Score : 0);
            }

            submitted = true;
            endingRound = false;

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
            bool battleMusic = screen == FluMixScreen.Battle
                || screen == FluMixScreen.Warning
                || screen == FluMixScreen.BossIntro
                || screen == FluMixScreen.Question
                || screen == FluMixScreen.BossResolve;
            FluMixSfx.SetBattleMusic(battleMusic);
            if (screen == FluMixScreen.Leaderboard)
                FluMixSfx.Play("ui_page");
            if (screen == FluMixScreen.Result)
                FluMixSfx.Play("podium_land");
            if (screen == FluMixScreen.Question)
                FluMixSfx.Play("quiz_unlock");
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
            if (services != null && services.UseOnlineApi && services.RoundSession != null)
                services.RoundSession.OnVirusDelta(delta);
        }

        public void DebugAddScore(int delta)
        {
            if (score != null)
                score.Add(delta);
            if (services != null && services.UseOnlineApi && services.RoundSession != null)
                services.RoundSession.OnVirusDelta(delta);
        }

        public void DebugSetScore(int value)
        {
            if (score == null)
                return;
            int delta = value - score.Score;
            score.SetScore(value);
            if (services != null && services.UseOnlineApi && services.RoundSession != null)
                services.RoundSession.OnVirusDelta(delta);
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

            if (screen == FluMixScreen.Rules || screen == FluMixScreen.Login || screen == FluMixScreen.Register)
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
