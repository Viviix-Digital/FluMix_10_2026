using System.Collections;
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

            QuestionData[] bank = services.Questions.LoadAll();
            QuestionData question = presenter.TakeNext(bank);
            int number = flow.QuestionsAnswered + 1;
            flow.SetScreen(FluMixScreen.Question);
            fx.ShowQuestion(true);
            presenter.Show(question, number, NotifyPicked);
            acceptAnswer = true;

            float remaining = config.questionSeconds;
            ForcedAnswer forced = ConsumeQueue();
            while (pickedIndex == -2 && remaining > 0f && forced != ForcedAnswer.Timeout)
            {
                if (forced == ForcedAnswer.Correct && question != null)
                {
                    presenter.Pick(question.correctIndex);
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
            bool correct = question != null && !timedOut && chosen == question.correctIndex;

            yield return presenter.Reveal(question, chosen);
            fx.ShowQuestion(false);
            presenter.HideAll();

            flow.RegisterQuestion(correct);
            flow.SetScreen(FluMixScreen.BossResolve);
            fx.StopBossShake();

            if (correct)
            {
                score.Add(config.correctQuestionScore);
                fx.HideBoss();
                if (battle != null)
                    battle.ClearField();
                fx.PlayConfetti();
                yield return fx.PlayWinShield();
                fx.PlayScorePopup(config.correctQuestionScore, Vector2.zero);
                bannerEndsRound = flow.IsRoundOver;
                fx.ShowWinBanner(bannerEndsRound);
                while (!bannerDismissed)
                    yield return null;
            }
            else
            {
                score.Add(config.wrongQuestionScore);
                fx.PlayScorePopup(config.wrongQuestionScore, Vector2.zero);
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
            for (int i = 0; i < question.options.Length; i++)
            {
                if (i != question.correctIndex)
                    return i;
            }

            return 0;
        }
    }
}
