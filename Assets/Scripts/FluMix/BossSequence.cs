using System;
using System.Collections;
using FluMix.Api;
using FluMix.Data;
using UnityEngine;

namespace FluMix
{
    public enum ForcedAnswer
    {
        None,
        Correct,
        Wrong,
        Timeout
    }

    public class BossSequence : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameFlow flow;
        [SerializeField] ScoreKeeper score;
        [SerializeField] BattleController battle;
        [SerializeField] QuestionPresenter presenter;
        [SerializeField] FxPlayer fx;
        [SerializeField] AppServices services;

        ForcedAnswer queued = ForcedAnswer.None;
        bool running;
        bool acceptAnswer;
        int pickedIndex = -2;
        bool bannerEndsRound;
        bool bannerDismissed;

        public void AdoptConfig(GameConfig gameConfig)
        {
            config = gameConfig;
        }

        public bool IsRunning => running;

        public void Bind(GameConfig gameConfig, GameFlow gameFlow, ScoreKeeper scoreKeeper, BattleController battleController, QuestionPresenter questionPresenter, FxPlayer effects, AppServices appServices)
        {
            config = gameConfig;
            flow = gameFlow;
            score = scoreKeeper;
            battle = battleController;
            presenter = questionPresenter;
            fx = effects;
            services = appServices;
        }

        public void Begin()
        {
            if (running)
                return;
            StartCoroutine(Run());
        }

        public void Queue(ForcedAnswer answer)
        {
            queued = answer;
        }

        public void NotifyPicked(int index)
        {
            if (!acceptAnswer)
                return;
            pickedIndex = index;
        }

        public void NotifyBannerPressed()
        {
            if (flow == null)
                return;
            fx.HideWinBanner();
            fx.HideBoss();
            presenter.HideAll();
            bannerDismissed = true;
            if (bannerEndsRound || flow.IsRoundOver)
                flow.EndRound();
            else
                flow.ResumeBattle();
        }

        public void Abort()
        {
            StopAllCoroutines();
            running = false;
            acceptAnswer = false;
            queued = ForcedAnswer.None;
            if (fx != null)
            {
                fx.HideWarning();
                fx.HideBoss();
                fx.HideWinBanner();
                fx.ShowQuestion(false);
            }

            if (presenter != null)
                presenter.HideAll();
        }

        IEnumerator Run()
        {
            running = true;
            acceptAnswer = false;
            pickedIndex = -2;
            bannerDismissed = false;
            battle.SetPaused(true);
            flow.SetTimerPaused(config.pauseRoundTimerDuringBoss);
            flow.SetScreen(FluMixScreen.Warning);

            yield return fx.PlayWarning();
            fx.HideWarning();

            flow.SetScreen(FluMixScreen.BossIntro);
            Vector2? spawn = null;
            if (battle != null && battle.TryVirusSpawnPoint(out Vector2 spawnPoint))
                spawn = spawnPoint;
            yield return fx.PlayBossEnter(spawn);

            QuestionData[] bank = services != null ? services.GetPlayQuestions() : null;
            QuestionData question = presenter.TakeNext(bank);
            int number = flow.QuestionsAnswered + 1;
            flow.SetScreen(FluMixScreen.Question);
            fx.ShowQuestion(true);
            presenter.Show(question, number, NotifyPicked);
            acceptAnswer = true;

            float remaining = config.questionSeconds;
            float startTime = Time.realtimeSinceStartup;
            ForcedAnswer forced = ConsumeQueue();
            while (pickedIndex == -2 && remaining > 0f && forced != ForcedAnswer.Timeout)
            {
                if (forced == ForcedAnswer.Correct && question != null)
                {
                    int idx = question.correctIndex >= 0 ? question.correctIndex : 0;
                    presenter.Pick(idx);
                    break;
                }

                if (forced == ForcedAnswer.Wrong && question != null)
                {
                    presenter.Pick(WrongIndex(question));
                    break;
                }

                remaining -= Time.deltaTime;
                presenter.SetTimer(Mathf.Max(0f, remaining));
                forced = ConsumeQueue();
                yield return null;
            }

            acceptAnswer = false;
            if (pickedIndex == -2)
                presenter.Lock();

            bool timedOut = pickedIndex == -2;
            int chosen = timedOut ? -1 : pickedIndex;
            int timeMs = Mathf.RoundToInt((Time.realtimeSinceStartup - startTime) * 1000f);
            string answerKey = timedOut ? string.Empty : presenter.KeyForIndex(chosen);

            bool correct;
            int pointsDelta;
            string correctKey = null;

            bool online = services != null && services.UseOnlineApi && services.RoundSession != null && services.RoundSession.HasPlay;
            if (online && question != null && question.source != null)
            {
                ProgressResultDto progress = null;
                yield return services.RoundSession.Answer(question.source, answerKey, timeMs, r => progress = r);

                if (progress == null || progress.answer == null)
                {
                    // Network fail: keep local fallback feedback, virus stays pending
                    correct = false;
                    pointsDelta = config.wrongQuestionScore;
                    Debug.LogWarning("[FluMist] Answer progress failed: " + services.RoundSession.LastError);
                    if (score != null)
                        score.SetScore(services.RoundSession.HudScore);
                }
                else
                {
                    correct = progress.answer.correct;
                    pointsDelta = progress.answer.points;
                    // Server may omit correctAnswer when project.settings.revealAnswer=false
                    correctKey = string.IsNullOrEmpty(progress.answer.correctAnswer)
                        ? null
                        : progress.answer.correctAnswer;

                    // Guard: positive points or matching revealed key means a correct answer.
                    if (!correct && pointsDelta > 0)
                        correct = true;
                    if (!correct
                        && !string.IsNullOrEmpty(correctKey)
                        && !string.IsNullOrEmpty(answerKey)
                        && string.Equals(answerKey, correctKey, StringComparison.OrdinalIgnoreCase))
                        correct = true;

                    if (score != null)
                        score.SetScore(services.RoundSession.HudScore);
                }
            }
            else
            {
                correct = question != null && !timedOut && chosen == question.correctIndex;
                pointsDelta = correct ? config.correctQuestionScore : config.wrongQuestionScore;
                if (score != null)
                    score.Add(pointsDelta);
                if (question != null && question.correctIndex >= 0)
                    correctKey = QuestionMapper.KeyAt(question, question.correctIndex);
            }

            yield return presenter.Reveal(question, chosen, correctKey, correct);
            // Wrong / timeout: skip explanation — after Wrong Answer overlay, boss attacks as before.
            if (correct)
                yield return presenter.ShowAnswerDescription(question, chosen, correctKey, correct);
            fx.ShowQuestion(false);
            presenter.HideAll();

            flow.RegisterQuestion(correct);
            flow.SetScreen(FluMixScreen.BossResolve);
            fx.StopBossShake();

            if (correct)
            {
                if (!online && score != null)
                {
                    // already added above for offline
                }

                fx.HideBoss();
                if (battle != null)
                    battle.ClearField();
                fx.PlayConfetti();
                yield return fx.PlayWinShield();
                fx.PlayScorePopup(pointsDelta, Vector2.zero);
                bannerEndsRound = flow.IsRoundOver;
                fx.ShowWinBanner(bannerEndsRound);
                while (!bannerDismissed)
                    yield return null;
            }
            else
            {
                fx.PlayScorePopup(pointsDelta, Vector2.zero);
                yield return fx.PlayBossLunge();
                fx.HideBoss();
                if (flow.IsRoundOver)
                    flow.EndRound();
                else
                    flow.ResumeBattle();
            }

            running = false;
        }

        ForcedAnswer ConsumeQueue()
        {
            ForcedAnswer value = queued;
            queued = ForcedAnswer.None;
            return value;
        }

        static int WrongIndex(QuestionData question)
        {
            if (question == null || question.options == null || question.options.Length == 0)
                return 0;
            int avoid = question.correctIndex >= 0 ? question.correctIndex : 0;
            for (int i = 0; i < question.options.Length; i++)
            {
                if (i != avoid)
                    return i;
            }

            return 0;
        }
    }
}
