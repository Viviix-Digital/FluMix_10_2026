using UnityEngine;

namespace FluMix
{
    public enum FluMixScreen
    {
        Boot,
        Login,
        Rules,
        Battle,
        Warning,
        BossIntro,
        Question,
        BossResolve,
        Result,
        Leaderboard
    }

    public class ScreenRouter : MonoBehaviour
    {
        [SerializeField] GameObject login;
        [SerializeField] GameObject rules;
        [SerializeField] GameObject gameplay;
        [SerializeField] GameObject result;
        [SerializeField] GameObject leaderboard;

        public void Bind(GameObject loginRoot, GameObject rulesRoot, GameObject gameplayRoot, GameObject resultRoot, GameObject leaderboardRoot)
        {
            login = loginRoot;
            rules = rulesRoot;
            gameplay = gameplayRoot;
            result = resultRoot;
            leaderboard = leaderboardRoot;
        }

        public void Show(FluMixScreen screen)
        {
            bool battle = screen == FluMixScreen.Battle
                || screen == FluMixScreen.Warning
                || screen == FluMixScreen.BossIntro
                || screen == FluMixScreen.Question
                || screen == FluMixScreen.BossResolve;

            Set(login, screen == FluMixScreen.Login);
            Set(rules, screen == FluMixScreen.Rules);
            Set(gameplay, battle);
            Set(result, screen == FluMixScreen.Result);
            Set(leaderboard, screen == FluMixScreen.Leaderboard);
        }

        static void Set(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
