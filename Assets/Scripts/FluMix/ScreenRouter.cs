using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluMix
{
    public enum FluMixScreen
    {
        Boot,
        Login,
        Register,
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
        [SerializeField] GameObject signUp;
        [SerializeField] GameObject rules;
        [SerializeField] GameObject gameplay;
        [SerializeField] GameObject result;
        [SerializeField] GameObject leaderboard;

        const float FadeSeconds = 0.32f;
        bool shown;
        readonly Dictionary<int, Coroutine> fades = new Dictionary<int, Coroutine>();

        public void Bind(GameObject loginRoot, GameObject rulesRoot, GameObject gameplayRoot, GameObject resultRoot, GameObject leaderboardRoot)
        {
            login = loginRoot;
            rules = rulesRoot;
            gameplay = gameplayRoot;
            result = resultRoot;
            leaderboard = leaderboardRoot;
            if (signUp == null)
            {
                var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == "1. Home SignUp")
                    {
                        signUp = all[i].gameObject;
                        break;
                    }
                }
            }
        }

        public void BindSignUp(GameObject signUpRoot)
        {
            signUp = signUpRoot;
        }

        public void Show(FluMixScreen screen)
        {
            bool battle = screen == FluMixScreen.Battle
                || screen == FluMixScreen.Warning
                || screen == FluMixScreen.BossIntro
                || screen == FluMixScreen.Question
                || screen == FluMixScreen.BossResolve;

            Crossfade(login, screen == FluMixScreen.Login);
            Crossfade(signUp, screen == FluMixScreen.Register);
            Crossfade(rules, screen == FluMixScreen.Rules);
            Crossfade(gameplay, battle);
            Crossfade(result, screen == FluMixScreen.Result);
            Crossfade(leaderboard, screen == FluMixScreen.Leaderboard);
            shown = true;
        }

        void Crossfade(GameObject target, bool active)
        {
            if (target == null)
                return;
            if (!shown)
            {
                SetImmediate(target, active);
                return;
            }

            if (active == target.activeSelf && (!active || GroupAlpha(target) > 0.99f))
                return;

            int id = target.GetInstanceID();
            if (fades.TryGetValue(id, out Coroutine running) && running != null)
                StopCoroutine(running);
            fades[id] = StartCoroutine(FadeRoutine(target, active));
        }

        IEnumerator FadeRoutine(GameObject target, bool active)
        {
            CanvasGroup group = Group(target);
            if (active)
            {
                bool wasOff = !target.activeSelf;
                target.SetActive(true);
                if (wasOff)
                    group.alpha = 0f;
                group.blocksRaycasts = false;
                float start = group.alpha;
                float elapsed = 0f;
                while (elapsed < FadeSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    group.alpha = Mathf.Lerp(start, 1f, Mathf.Clamp01(elapsed / FadeSeconds));
                    yield return null;
                }

                group.alpha = 1f;
                group.blocksRaycasts = true;
                yield break;
            }

            float from = group.alpha;
            float gone = 0f;
            group.blocksRaycasts = false;
            while (gone < FadeSeconds)
            {
                gone += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, 0f, Mathf.Clamp01(gone / FadeSeconds));
                yield return null;
            }

            group.alpha = 0f;
            target.SetActive(false);
        }

        static void SetImmediate(GameObject target, bool active)
        {
            target.SetActive(active);
            if (!active)
                return;
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
                group = target.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }

        static float GroupAlpha(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            return group != null ? group.alpha : 1f;
        }

        static CanvasGroup Group(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
                group = target.AddComponent<CanvasGroup>();
            return group;
        }
    }
}
