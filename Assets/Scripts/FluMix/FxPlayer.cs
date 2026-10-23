using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    /// <summary>
    /// Section 7 tweens. hero_a_attack, hero_g_attack, and hero_t_attack are single attack sheets.
    /// boss_states is one 16-frame sheet: frames 0-3 idle, 4-7 attack, 8-11 defeat.
    /// virus_states is one 16-frame sheet for the green virus: frames 0-7 idle, 8-11 hit.
    /// The virus sheet has no shrink, so death still uses the scale tween.
    /// The win shield was not animated in this pass.
    /// </summary>
    public class FxPlayer : MonoBehaviour
    {
        struct BobTarget
        {
            public RectTransform rect;
            public Vector2 basePosition;
            public float phase;
            public Vector2 extra;
        }

        [SerializeField] GameConfig config;
        [SerializeField] RectTransform arena;
        [SerializeField] RectTransform shakeRoot;
        [SerializeField] Image darken;
        [SerializeField] RectTransform warningPanel;
        [SerializeField] Image countdown;
        [SerializeField] Sprite[] countdownSprites;
        [SerializeField] RectTransform boss;
        [SerializeField] CanvasGroup bossGroup;
        [SerializeField] SpriteSheetPlayer bossSheet;
        [SerializeField] Sprite[] bossAttackFrames;
        [SerializeField] Sprite[] bossDefeatFrames;
        [SerializeField] int bossIdleStart;
        [SerializeField] int bossIdleCount;
        [SerializeField] int bossAttackStart;
        [SerializeField] int bossAttackCount;
        [SerializeField] int bossDefeatStart;
        [SerializeField] int bossDefeatCount;
        [SerializeField] Image questionPanel;
        [SerializeField] RectTransform winBanner;
        [SerializeField] Image winButtonImage;
        [SerializeField] Sprite continueSprite;
        [SerializeField] Sprite resultSprite;
        [SerializeField] Image winShield;
        [SerializeField] SpriteSheetPlayer winSheet;
        [SerializeField] Image beam;
        [SerializeField] Image beamCore;
        [SerializeField] Image impact;
        [SerializeField] Sprite beamA;
        [SerializeField] Sprite beamG;
        [SerializeField] Sprite beamT;
        [SerializeField] Image wrongMark;
        [SerializeField] Image redFlash;
        [SerializeField] Sprite plus100;
        [SerializeField] Sprite minus50;
        [SerializeField] Sprite plus200;
        [SerializeField] RectTransform[] bobTargets;

        readonly List<BobTarget> bobs = new List<BobTarget>();
        Transform confettiBurst;
        readonly List<Image> popups = new List<Image>();

        Vector2 shakeBase;
        Vector2 bossRest;
        float shakeTime;
        bool bossShaking;
        bool warningPulse;
        bool built;

        public void Bind(
            GameConfig gameConfig,
            RectTransform arenaRect,
            RectTransform shake,
            Image darkenImage,
            RectTransform warning,
            Image countdownImage,
            Sprite[] countdownArts,
            RectTransform bossRect,
            CanvasGroup bossCanvasGroup,
            SpriteSheetPlayer bossPlayer,
            Image question,
            RectTransform banner,
            Image winButton,
            Sprite continueArt,
            Sprite resultArt,
            Sprite shieldArt,
            Sprite shotA,
            Sprite shotG,
            Sprite shotT,
            Sprite impactArt,
            Sprite wrongArt,
            Sprite popup100,
            Sprite popupMinus,
            Sprite popup200)
        {
            config = gameConfig;
            arena = arenaRect;
            shakeRoot = shake;
            darken = darkenImage;
            warningPanel = warning;
            countdown = countdownImage;
            countdownSprites = countdownArts;
            boss = bossRect;
            bossGroup = bossCanvasGroup;
            bossSheet = bossPlayer;
            questionPanel = question;
            winBanner = banner;
            winButtonImage = winButton;
            continueSprite = continueArt;
            resultSprite = resultArt;
            beamA = shotA;
            beamG = shotG;
            beamT = shotT;
            plus100 = popup100;
            minus50 = popupMinus;
            plus200 = popup200;
            BuildVisuals(shieldArt, impactArt, wrongArt);
        }

        public void AdoptConfig(GameConfig gameConfig)
        {
            config = gameConfig;
            if (winSheet != null)
                winSheet.AdoptConfig(gameConfig);
            if (bossSheet != null)
                bossSheet.AdoptConfig(gameConfig);
        }

        public void SetBossClip(int idleStart, int idleCount, int attackStart, int attackCount, int defeatStart, int defeatCount)
        {
            bossIdleStart = idleStart;
            bossIdleCount = idleCount;
            bossAttackStart = attackStart;
            bossAttackCount = attackCount;
            bossDefeatStart = defeatStart;
            bossDefeatCount = defeatCount;
        }

        public void SetBobTargets(RectTransform[] targets)
        {
            bobTargets = targets;
        }

        public void SetWinSheet(Sprite[] frames)
        {
            if (winSheet != null)
                winSheet.SetFrames(frames, null);
        }

        public void RegisterBob(RectTransform rect, float phase)
        {
            if (rect == null)
                return;

            for (int i = 0; i < bobs.Count; i++)
            {
                if (bobs[i].rect == rect)
                    return;
            }

            bobs.Add(new BobTarget
            {
                rect = rect,
                basePosition = rect.anchoredPosition,
                phase = phase,
                extra = Vector2.zero
            });
        }

        public void BuildVisuals(Sprite shieldArt, Sprite impactArt, Sprite wrongArt)
        {
            if (arena == null)
                return;

            Transform fx = arena.Find("Fx");
            RectTransform fxRoot;
            if (fx == null)
            {
                fxRoot = FluMixUi.CreateRect(arena, "Fx", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
                fxRoot.offsetMin = Vector2.zero;
                fxRoot.offsetMax = Vector2.zero;
            }
            else
            {
                fxRoot = (RectTransform)fx;
            }

            scoreRoot = fxRoot;

            if (beam == null)
            {
                beam = FluMixUi.CreateImage(fxRoot, "Beam", beamA, new Vector2(100f, config != null ? config.beamThickness : 72f), false);
                RectTransform beamRect = beam.rectTransform;
                beamRect.pivot = new Vector2(0f, 0.5f);
                beam.color = Color.white;
                beamCore = FluMixUi.CreateImage(beamRect, "Core", FluMixUi.White, new Vector2(100f, 14f), false);
                RectTransform coreRect = beamCore.rectTransform;
                coreRect.pivot = new Vector2(0f, 0.5f);
                coreRect.anchorMin = new Vector2(0f, 0.5f);
                coreRect.anchorMax = new Vector2(0f, 0.5f);
                beamCore.color = Color.white;
            }

            if (impact == null)
            {
                impact = FluMixUi.CreateImage(fxRoot, "Impact", impactArt != null ? impactArt : FluMixUi.White, new Vector2(180f, 180f), false);
                impact.preserveAspect = true;
            }
            else if (impactArt != null)
            {
                impact.sprite = impactArt;
            }

            if (wrongMark == null)
            {
                wrongMark = FluMixUi.CreateImage(fxRoot, "WrongMark", wrongArt != null ? wrongArt : FluMixUi.White, new Vector2(220f, 64f), false);
                wrongMark.preserveAspect = true;
            }
            else if (wrongArt != null)
            {
                wrongMark.sprite = wrongArt;
            }

            if (winShield == null && shakeRoot != null)
            {
                winShield = FluMixUi.CreateImage(shakeRoot, "WinShield", shieldArt != null ? shieldArt : FluMixUi.White, new Vector2(720f, 720f), false);
                winShield.preserveAspect = true;
                winSheet = winShield.gameObject.AddComponent<SpriteSheetPlayer>();
                winSheet.Bind(winShield, config);
            }

            if (redFlash == null && shakeRoot != null)
            {
                var rect = FluMixUi.CreateRect(shakeRoot, "RedFlash", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                redFlash = rect.gameObject.AddComponent<Image>();
                redFlash.sprite = FluMixUi.White;
                redFlash.color = new Color(1f, 0.05f, 0.08f, 0f);
                redFlash.raycastTarget = false;
            }

            while (popups.Count < 4)
            {
                Image popup = FluMixUi.CreateImage(fxRoot, "ScorePopup", plus100, new Vector2(180f, 90f), false);
                popup.preserveAspect = true;
                popup.gameObject.SetActive(false);
                popups.Add(popup);
            }

            HideImmediate();
            built = true;
            if (shakeRoot != null)
                shakeBase = shakeRoot.anchoredPosition;
            if (boss != null)
                bossRest = boss.anchoredPosition;
        }

        void Awake()
        {
            if (!built)
                BuildVisuals(winShield != null ? winShield.sprite : null, impact != null ? impact.sprite : null, wrongMark != null ? wrongMark.sprite : null);
        }

        void Start()
        {
            SilenceConfetti();
            if (bobTargets == null)
                return;

            for (int i = 0; i < bobTargets.Length; i++)
                RegisterBob(bobTargets[i], bobTargets.Length <= 1 ? 0f : (float)i / bobTargets.Length);
        }

        void Update()
        {
            if (config == null)
                return;

            float bobDuration = Mathf.Max(0.01f, config.idleBobSeconds);
            for (int i = 0; i < bobs.Count; i++)
            {
                BobTarget bob = bobs[i];
                if (bob.rect == null)
                    continue;
                float wave = Mathf.Sin((Time.time / bobDuration + bob.phase) * Mathf.PI * 2f);
                bob.rect.anchoredPosition = bob.basePosition + bob.extra + new Vector2(0f, wave * config.idleBobPixels);
            }

            if (bossShaking && boss != null)
            {
                float speed = config.bossShakeSpeed;
                boss.anchoredPosition = bossRest + new Vector2(
                    Mathf.Sin(Time.time * speed) * config.bossShakePixels,
                    Mathf.Cos(Time.time * speed * 0.8f) * config.bossShakePixels);
            }

            if (warningPulse && darken != null)
            {
                float wave = Mathf.Sin(Time.time * 6f) * 0.5f + 0.5f;
                float peak = config != null ? config.darkenAlpha : 0.6f;
                float alpha = Mathf.Lerp(peak * 0.35f, peak, wave);
                darken.color = new Color(0f, 0f, 0f, alpha);
            }

            if (shakeTime > 0f && shakeRoot != null)
            {
                shakeTime -= Time.deltaTime;
                if (shakeTime <= 0f)
                {
                    shakeRoot.anchoredPosition = shakeBase;
                }
                else
                {
                    shakeRoot.anchoredPosition = shakeBase + Random.insideUnitCircle * config.screenShake;
                }
            }
        }

        public void HideImmediate()
        {
            SetActive(beam, false);
            SetActive(impact, false);
            SetActive(wrongMark, false);
            SetActive(winShield, false);
            SetActive(countdown, false);
            if (darken != null)
                darken.color = new Color(0f, 0f, 0f, 0f);
            if (redFlash != null)
                redFlash.color = new Color(1f, 0.05f, 0.08f, 0f);
            if (questionPanel != null)
                questionPanel.gameObject.SetActive(false);
            if (warningPanel != null)
                warningPanel.gameObject.SetActive(false);
            if (winBanner != null)
                winBanner.gameObject.SetActive(false);
            if (winButtonImage != null)
                winButtonImage.gameObject.SetActive(false);
            if (boss != null)
                boss.gameObject.SetActive(false);
            StopConfetti();
            warningPulse = false;
            bossShaking = false;
        }

        public IEnumerator PlayWarning()
        {
            FluMixSfx.Play("warning_alarm");
            FluMixSfx.Play("stinger_sos");
            if (darken != null)
            {
                darken.transform.SetAsLastSibling();
                if (warningPanel != null)
                    warningPanel.SetAsLastSibling();
                if (countdown != null)
                    countdown.rectTransform.SetAsLastSibling();
            }

            if (warningPanel != null)
            {
                warningPanel.gameObject.SetActive(true);
                var image = warningPanel.GetComponent<Image>();
                if (image != null)
                    image.color = Color.white;
            }
            if (countdown != null)
                countdown.gameObject.SetActive(true);

            float pop = config.warningPopSeconds;
            float elapsed = 0f;
            while (elapsed < pop)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / pop);
                float scale = Mathf.Lerp(config.warningStartScale, 1f, EaseOutBack(u));
                if (warningPanel != null)
                    warningPanel.localScale = Vector3.one * scale;
                if (darken != null)
                    darken.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, config.darkenAlpha, u));
                yield return null;
            }

            warningPulse = true;

            for (int step = config.countdownFrom; step >= 1; step--)
            {
                if (countdown != null)
                {
                    int index = step - 1;
                    if (countdownSprites != null && index >= 0 && index < countdownSprites.Length && countdownSprites[index] != null)
                        countdown.sprite = countdownSprites[index];
                    countdown.rectTransform.localScale = Vector3.one * config.countdownPopScale;
                }

                float stepTime = 0f;
                while (stepTime < config.bossCountdownStep)
                {
                    stepTime += Time.deltaTime;
                    float u = Mathf.Clamp01(stepTime / config.bossCountdownStep);
                    if (countdown != null)
                        countdown.rectTransform.localScale = Vector3.one * Mathf.Lerp(config.countdownPopScale, 1f, EaseOutBack(u));
                    yield return null;
                }
            }
        }

        public void HideWarning()
        {
            warningPulse = false;
            if (warningPanel != null)
            {
                warningPanel.gameObject.SetActive(false);
                warningPanel.localScale = Vector3.one;
                var image = warningPanel.GetComponent<Image>();
                if (image != null)
                    image.color = Color.white;
            }

            if (countdown != null)
                countdown.gameObject.SetActive(false);
            if (darken != null)
                darken.color = new Color(0f, 0f, 0f, 0f);
        }

        public IEnumerator PlayBossEnter(Vector2? spawn)
        {
            if (boss == null)
                yield break;

            Vector2 home = bossRest.sqrMagnitude > 0.01f ? bossRest : boss.anchoredPosition;
            Vector2 from = spawn ?? home + Vector2.left * 820f;
            boss.gameObject.SetActive(true);
            boss.SetAsLastSibling();
            if (bossGroup != null)
                bossGroup.alpha = 1f;
            boss.anchoredPosition = from;
            boss.localScale = Vector3.one * 0.86f;
            bossShaking = false;
            if (bossSheet != null && bossSheet.HasFrames)
            {
                if (bossIdleCount > 0)
                    bossSheet.PlayRange(bossIdleStart, bossIdleCount, true, null);
                else
                    bossSheet.PlayLoop();
            }

            float enter = config.bossEnterSeconds > 0.2f ? config.bossEnterSeconds : 2.6f;
            float elapsed = 0f;
            while (elapsed < enter)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / enter));
                boss.anchoredPosition = Vector2.Lerp(from, home, u);
                boss.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, u);
                yield return null;
            }

            boss.anchoredPosition = home;
            boss.localScale = Vector3.one;
            bossRest = home;
            bossShaking = true;
        }

        public void StopBossShake()
        {
            bossShaking = false;
            if (boss != null)
                boss.anchoredPosition = bossRest;
        }

        public IEnumerator PlayBossDefeat()
        {
            StopBossShake();
            if (boss == null)
                yield break;

            bool playedSheet = bossSheet != null && bossDefeatCount > 0 && bossSheet.HasFrames;
            if (!playedSheet && bossDefeatFrames != null && bossDefeatFrames.Length > 0 && bossSheet != null)
            {
                bossSheet.SetFrames(bossDefeatFrames, boss.GetComponent<Image>() != null ? boss.GetComponent<Image>().sprite : null);
                playedSheet = true;
                bossDefeatStart = 0;
                bossDefeatCount = bossDefeatFrames.Length;
            }

            if (playedSheet)
            {
                bool done = false;
                bossSheet.PlayRange(bossDefeatStart, bossDefeatCount, false, () => done = true, false);
                float guard = 0f;
                float limit = bossDefeatCount / Mathf.Max(1f, config.sheetFps) + 0.1f;
                while (!done && guard < limit)
                {
                    guard += Time.deltaTime;
                    yield return null;
                }
            }

            float elapsed = 0f;
            Vector3 start = boss.localScale;
            while (elapsed < config.bossDefeatSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.bossDefeatSeconds);
                boss.localScale = Vector3.Lerp(start, Vector3.zero, u);
                if (bossGroup != null)
                    bossGroup.alpha = 1f - u;
                yield return null;
            }

            boss.gameObject.SetActive(false);
            boss.localScale = Vector3.one;
            if (bossGroup != null)
                bossGroup.alpha = 1f;
        }

        public IEnumerator PlayBossLunge()
        {
            StopBossShake();
            if (bossSheet != null && bossAttackCount > 0 && bossSheet.HasFrames)
                bossSheet.PlayRange(bossAttackStart, bossAttackCount, false, null, false);
            else if (bossSheet != null && bossAttackFrames != null && bossAttackFrames.Length > 0)
            {
                Sprite idle = boss.GetComponent<Image>() != null ? boss.GetComponent<Image>().sprite : null;
                bossSheet.SetFrames(bossAttackFrames, idle);
                bossSheet.PlayOnce(null);
            }

            Vector2 start = boss != null ? boss.anchoredPosition : bossRest;
            Vector2 lunged = start + Vector2.left * config.bossLungePixels;
            float half = config.bossLungeSeconds * 0.5f;
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                if (boss != null)
                    boss.anchoredPosition = Vector2.Lerp(start, lunged, Mathf.Clamp01(elapsed / half));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                if (boss != null)
                    boss.anchoredPosition = Vector2.Lerp(lunged, start, Mathf.Clamp01(elapsed / half));
                yield return null;
            }

            if (boss != null)
                boss.anchoredPosition = start;
            bossRest = start;
            Shake();
            StartCoroutine(FlashRed());
        }

        public void HideBoss()
        {
            StopBossShake();
            if (bossSheet != null)
                bossSheet.Stop();
            if (boss != null)
            {
                boss.gameObject.SetActive(false);
                boss.localScale = Vector3.one;
                boss.anchoredPosition = bossRest;
            }

            if (bossGroup != null)
                bossGroup.alpha = 1f;
        }

        public IEnumerator PlayWinShield()
        {
            if (winShield == null)
                yield break;

            FluMixShotVfx shots = GetComponent<FluMixShotVfx>();
            if (shots == null)
                shots = FindAnyObjectByType<FluMixShotVfx>();
            if (shots != null)
                shots.PlayWinOverUi();

            winShield.gameObject.SetActive(true);
            winShield.rectTransform.SetAsLastSibling();
            float duration = config != null && config.winShieldDuration > 0.05f ? config.winShieldDuration : 0.45f;
            float start = config != null ? config.winShieldStartScale : 0.4f;
            float peak = config != null ? config.winShieldPeakScale : 1.05f;
            float sway = config != null ? config.winShieldSwayDegrees : 8f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float scale = u < 0.6f
                    ? Mathf.Lerp(start, peak, u / 0.6f)
                    : Mathf.Lerp(peak, 1f, (u - 0.6f) / 0.4f);
                winShield.rectTransform.localScale = Vector3.one * scale;
                winShield.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(u * Mathf.PI * 2f) * sway);
                yield return null;
            }

            winShield.rectTransform.localScale = Vector3.one;
            winShield.rectTransform.localEulerAngles = Vector3.zero;
        }

        public void HideWinShield()
        {
            FluMixShotVfx shots = GetComponent<FluMixShotVfx>();
            if (shots != null)
                shots.HideWin();
            if (winSheet != null)
                winSheet.Stop();
            if (winShield != null)
            {
                winShield.gameObject.SetActive(false);
                winShield.rectTransform.localEulerAngles = Vector3.zero;
                winShield.rectTransform.localScale = Vector3.one;
            }
        }

        public void PlayConfetti()
        {
            if (config == null || !config.playConfetti)
                return;

            if (confettiBurst == null)
                confettiBurst = FindConfettiBurst();
            if (confettiBurst == null)
                return;

            confettiBurst.gameObject.SetActive(true);
            ParticleSystem[] systems = confettiBurst.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
                main.playOnAwake = false;
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].Play(true);
            }
        }

        public void ShowWinBanner(bool roundOver)
        {
            if (winBanner != null)
            {
                winBanner.gameObject.SetActive(true);
                winBanner.SetAsLastSibling();
                Vector2 rest = winBanner.anchoredPosition;
                winBanner.anchoredPosition = rest + Vector2.up * config.bannerSlideDistance;
                StartCoroutine(SlideBanner(rest));
            }

            if (winButtonImage != null)
            {
                winButtonImage.gameObject.SetActive(true);
                winButtonImage.sprite = roundOver ? resultSprite : continueSprite;
                winButtonImage.rectTransform.SetAsLastSibling();
            }
        }

        public void HideWinBanner()
        {
            if (winBanner != null)
                winBanner.gameObject.SetActive(false);
            if (winButtonImage != null)
                winButtonImage.gameObject.SetActive(false);
            HideWinShield();
            StopConfetti();
        }

        public void ShowQuestion(bool visible)
        {
            if (questionPanel != null)
            {
                questionPanel.gameObject.SetActive(visible);
                if (visible)
                    questionPanel.rectTransform.SetAsLastSibling();
            }
        }

        public IEnumerator PlayBeam(HeroId hero, Vector2 from, Vector2 to, bool connect, bool impactSound = true)
        {
            Vector2 direction = to - from;
            float distance = direction.magnitude;
            if (distance < 1f)
                distance = 1f;
            float thickness = config != null ? config.beamThickness : 72f;
            float travel = connect ? distance : Mathf.Max(thickness, distance - config.wrongBeamGap);
            Vector2 endPoint = from + direction.normalized * travel;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            FluMixShotVfx shots = GetComponent<FluMixShotVfx>();
            if (shots == null)
                shots = FindAnyObjectByType<FluMixShotVfx>();
            bool particles = shots != null && arena != null && shots.PlayOverUi(hero, arena, from, endPoint, connect, impactSound);
            if (particles)
            {
                float shotElapsed = 0f;
                float shotSeconds = config != null ? config.beamTravelSeconds : 0.18f;
                while (shotElapsed < shotSeconds)
                {
                    shotElapsed += Time.deltaTime;
                    yield return null;
                }

                yield break;
            }

            if (beam == null)
                yield break;

            Sprite art = hero == HeroId.G ? beamG : hero == HeroId.T ? beamT : beamA;
            if (art != null)
                beam.sprite = art;

            string cue = hero == HeroId.A ? "beam_blue" : hero == HeroId.G ? "beam_green" : "beam_pink";
            FluMixSfx.Play(cue);
            if (impactSound)
                FluMixSfx.Play(connect ? "impact_correct" : "impact_wrong");

            beam.gameObject.SetActive(true);
            beam.rectTransform.SetAsLastSibling();
            beam.rectTransform.anchoredPosition = from;
            beam.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
            float core = config != null ? config.beamCoreThickness : 14f;
            float start = config != null ? Mathf.Clamp(config.beamStartScale, 0.05f, 1f) : 0.15f;
            if (beamCore != null)
            {
                beamCore.rectTransform.anchoredPosition = Vector2.zero;
                beamCore.gameObject.SetActive(true);
            }

            float elapsed = 0f;
            float seconds = config != null ? config.beamTravelSeconds : 0.18f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, seconds));
                float length = Mathf.Lerp(travel * start, travel, u);
                beam.rectTransform.sizeDelta = new Vector2(length, thickness);
                if (beamCore != null)
                    beamCore.rectTransform.sizeDelta = new Vector2(length, core);
                yield return null;
            }

            beam.rectTransform.sizeDelta = new Vector2(travel, thickness);
            yield return PlayImpact(endPoint, angle);
            beam.gameObject.SetActive(false);
        }

        RectTransform aimArrow;
        Image aimGlow;
        Image aimCore;
        Image aimRing;
        Image aimHead;
        static Sprite aimHeadSprite;
        static Sprite aimGlowSprite;

        public IEnumerator PlayAimArrow(Vector2 origin, Vector2 target)
        {
            EnsureAimArrow();
            if (aimArrow == null)
                yield break;

            aimArrow.gameObject.SetActive(true);
            aimArrow.SetAsLastSibling();
            float elapsed = 0f;
            const float seconds = 0.34f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
                float pulse = 0.85f + 0.15f * Mathf.Sin(elapsed * 28f);
                LayoutAimArrow(origin, Vector2.Lerp(origin, target, u), pulse);
                float fade = u < 0.82f ? 1f : 1f - (u - 0.82f) / 0.18f;
                TintAim(fade);
                yield return null;
            }

            aimArrow.gameObject.SetActive(false);
        }

        void EnsureAimArrow()
        {
            if (aimArrow != null || (arena == null && scoreRoot == null))
                return;

            Transform parent = arena != null ? arena : scoreRoot;
            aimArrow = FluMixUi.CreateRect(parent, "AimArrow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f));
            aimGlow = Shaft(aimArrow, "Glow", AimGlowSprite(), 22f);
            aimCore = Shaft(aimArrow, "Core", FluMixUi.White, 4f);
            aimRing = FluMixUi.CreateImage(aimArrow, "Ring", AimGlowSprite(), new Vector2(42f, 42f), false);
            aimRing.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            aimRing.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            aimRing.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            aimHead = FluMixUi.CreateImage(aimArrow, "Head", AimHeadSprite(), new Vector2(34f, 34f), false);
            aimHead.rectTransform.pivot = new Vector2(0.15f, 0.5f);
            aimHead.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            aimHead.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            aimArrow.gameObject.SetActive(false);
        }

        static Image Shaft(RectTransform parent, string name, Sprite sprite, float thickness)
        {
            Image image = FluMixUi.CreateImage(parent, name, sprite, new Vector2(40f, thickness), false);
            image.rectTransform.pivot = new Vector2(0f, 0.5f);
            image.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            image.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            return image;
        }

        void LayoutAimArrow(Vector2 from, Vector2 to, float pulse)
        {
            Vector2 direction = to - from;
            float length = direction.magnitude;
            float angle = length > 0.01f ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg : 0f;
            aimArrow.anchoredPosition = from;
            aimArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
            const float head = 34f;
            bool showHead = length > head + 8f;
            aimHead.enabled = showHead;
            aimRing.enabled = showHead;
            float shaft = showHead ? Mathf.Max(0f, length - head * 0.55f) : length;
            aimGlow.rectTransform.anchoredPosition = Vector2.zero;
            aimGlow.rectTransform.sizeDelta = new Vector2(shaft, 22f * pulse);
            aimCore.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            aimCore.rectTransform.sizeDelta = new Vector2(shaft, 4f);
            Vector2 tip = new Vector2(length, 0f);
            aimRing.rectTransform.anchoredPosition = tip;
            aimRing.rectTransform.sizeDelta = new Vector2(46f * pulse, 46f * pulse);
            aimHead.rectTransform.anchoredPosition = tip - new Vector2(8f, 0f);
            aimHead.rectTransform.sizeDelta = new Vector2(head, head);
        }

        void TintAim(float fade)
        {
            aimGlow.color = new Color(0.35f, 0.9f, 1f, 0.38f * fade);
            aimCore.color = new Color(0.92f, 1f, 1f, fade);
            aimRing.color = new Color(0.45f, 0.95f, 1f, 0.55f * fade);
            aimHead.color = new Color(1f, 1f, 1f, fade);
        }

        static Sprite AimHeadSprite()
        {
            if (aimHeadSprite != null)
                return aimHeadSprite;

            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float across = x / (float)(size - 1);
                    float fromCenter = Mathf.Abs(y / (float)(size - 1) - 0.5f) * 2f;
                    texture.SetPixel(x, y, across >= fromCenter ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            aimHeadSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0f, 0.5f), 100f);
            return aimHeadSprite;
        }

        static Sprite AimGlowSprite()
        {
            if (aimGlowSprite != null)
                return aimGlowSprite;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = center.x;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha *= alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            aimGlowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return aimGlowSprite;
        }

        public IEnumerator PlayVirusDeath(RectTransform virus)
        {
            if (virus == null)
                yield break;

            VirusView view = virus.GetComponent<VirusView>();
            if (view != null)
            {
                if (view.HasHit)
                    view.PlayHit();
                else
                    view.PlayDeath();
            }

            float elapsed = 0f;
            Quaternion startRot = virus.localRotation;
            Vector3 startScale = virus.localScale;
            while (elapsed < config.deathSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.deathSeconds);
                float vanish = 1f - Mathf.SmoothStep(0f, 1f, u);
                if (view != null)
                    view.Vanish = vanish;
                virus.localScale = Vector3.Lerp(startScale, Vector3.zero, u);
                virus.localRotation = startRot * Quaternion.Euler(0f, 0f, config.deathSpinDegrees * u);
                yield return null;
            }

            if (view != null)
                view.Vanish = 0f;
        }

        public IEnumerator PlayRetype(VirusView virus, System.Action swap)
        {
            if (virus == null)
                yield break;

            var view = virus;
            view.PlayHit();
            FluMixSfx.Play("virus_evolve");
            Color previous = virus.Tint;
            float elapsed = 0f;
            while (virus != null && elapsed < config.whiteFlashSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.whiteFlashSeconds);
                virus.SetTint(Color.Lerp(previous, Color.white, Mathf.Sin(u * Mathf.PI)));
                yield return null;
            }

            if (virus == null)
                yield break;

            swap?.Invoke();
            virus.SetTint(Color.white);
            virus.Rect.localScale = Vector3.one;
        }

        public void ShowWrongMark(Vector2 position)
        {
            if (wrongMark == null)
                return;
            wrongMark.rectTransform.anchoredPosition = position;
            wrongMark.rectTransform.sizeDelta = new Vector2(config.wrongMarkWidth, config.wrongMarkHeight);
            wrongMark.gameObject.SetActive(true);
            StartCoroutine(HideAfter(wrongMark.gameObject, config.wrongMarkSeconds));
        }

        RectTransform scoreRoot;

        public void PlayScorePopup(int delta, Vector2 position)
        {
            ShowScore(delta, position);
        }

        public void PlayScorePopup(int delta, Vector3 world)
        {
            if (scoreRoot == null || world.sqrMagnitude < 0.01f)
            {
                ShowScore(delta, Vector2.zero);
                return;
            }

            Canvas canvas = scoreRoot.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? (canvas.worldCamera != null ? canvas.worldCamera : Camera.main)
                : null;
            Camera projector = cam != null ? cam : Camera.main;
            if (projector == null)
            {
                ShowScore(delta, Vector2.zero);
                return;
            }

            Vector3 screen = projector.WorldToScreenPoint(world);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(scoreRoot, screen, cam, out Vector2 local))
            {
                ShowScore(delta, Vector2.zero);
                return;
            }

            ShowScore(delta, local);
        }

        void ShowScore(int delta, Vector2 position)
        {
            if (config == null)
                return;

            Sprite sprite = delta >= config.correctQuestionScore ? plus200
                : delta > 0 ? plus100
                : minus50;
            if (sprite == null)
                return;

            Image popup = null;
            for (int i = 0; i < popups.Count; i++)
            {
                if (popups[i] != null && !popups[i].gameObject.activeSelf)
                {
                    popup = popups[i];
                    break;
                }
            }

            if (popup == null && popups.Count > 0)
                popup = popups[0];
            if (popup == null)
                return;

            Transform parent = scoreRoot != null ? scoreRoot : arena;
            if (parent == null)
                return;

            popup.rectTransform.SetParent(parent, false);
            popup.sprite = sprite;
            popup.preserveAspect = true;
            popup.raycastTarget = false;
            popup.color = Color.white;
            float height = config.scorePopupSize > 1f ? config.scorePopupSize * 1.35f : 240f;
            popup.rectTransform.sizeDelta = new Vector2(height * 2.15f, height);
            popup.rectTransform.localScale = Vector3.one;
            popup.rectTransform.anchoredPosition = position + new Vector2(0f, 90f);
            popup.gameObject.SetActive(true);
            popup.rectTransform.SetAsLastSibling();
            StartCoroutine(FloatPopup(popup, popup.rectTransform.anchoredPosition));
        }

        public void Shake()
        {
            shakeTime = config.shakeSeconds;
            if (shakeRoot != null)
                shakeBase = bossShaking ? shakeBase : shakeRoot.anchoredPosition;
        }

        public IEnumerator FlashRed()
        {
            if (redFlash == null)
                yield break;

            redFlash.transform.SetAsLastSibling();
            float elapsed = 0f;
            while (elapsed < config.redFlashSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.redFlashSeconds);
                redFlash.color = new Color(1f, 0.05f, 0.08f, Mathf.Lerp(config.redFlashAlpha, 0f, u));
                yield return null;
            }

            redFlash.color = new Color(1f, 0.05f, 0.08f, 0f);
        }

        public IEnumerator HeroRecoil(RectTransform hero)
        {
            int index = IndexOfBob(hero);
            if (index < 0)
            {
                yield return Nudge(hero, Vector2.left * config.heroRecoilPixels);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < config.beamTravelSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.beamTravelSeconds);
                float kick = u < 0.35f ? u / 0.35f : 1f - ((u - 0.35f) / 0.65f);
                BobTarget bob = bobs[index];
                bob.extra = Vector2.left * (config.heroRecoilPixels * kick);
                bobs[index] = bob;
                yield return null;
            }

            BobTarget done = bobs[index];
            done.extra = Vector2.zero;
            bobs[index] = done;
        }

        public Vector2 ArenaPoint(RectTransform point)
        {
            if (arena == null || point == null)
                return Vector2.zero;
            Vector3 local = arena.InverseTransformPoint(point.TransformPoint(Vector3.zero));
            return new Vector2(local.x, local.y);
        }

        IEnumerator PlayImpact(Vector2 position, float angle)
        {
            if (impact == null)
                yield break;

            impact.gameObject.SetActive(true);
            impact.rectTransform.anchoredPosition = position;
            impact.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle - config.beamArtAngle);
            float elapsed = 0f;
            while (elapsed < config.hitFlashSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.hitFlashSeconds);
                float scale = u < 0.45f
                    ? Mathf.Lerp(0f, config.hitFlashPeakScale, u / 0.45f)
                    : Mathf.Lerp(config.hitFlashPeakScale, 0f, (u - 0.45f) / 0.55f);
                impact.rectTransform.localScale = Vector3.one * scale;
                yield return null;
            }

            impact.gameObject.SetActive(false);
            impact.rectTransform.localScale = Vector3.one;
        }

        IEnumerator PulseRing(VirusView virus)
        {
            Color color = ColorFor(virus.Type);
            Image ring = FluMixUi.CreateImage(virus.Rect.parent, "RetypeRing", FluMixUi.White, new Vector2(40f, 40f), false);
            ring.rectTransform.anchoredPosition = virus.Rect.anchoredPosition;
            ring.color = color;
            float elapsed = 0f;
            const float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 2.4f, u);
                ring.color = new Color(color.r, color.g, color.b, 1f - u);
                yield return null;
            }

            Destroy(ring.gameObject);
        }

        IEnumerator FloatPopup(Image popup, Vector2 start)
        {
            float elapsed = 0f;
            while (elapsed < config.scorePopupSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.scorePopupSeconds);
                popup.rectTransform.anchoredPosition = start + Vector2.up * (config.scorePopupRise * u);
                popup.color = new Color(1f, 1f, 1f, 1f - u);
                yield return null;
            }

            popup.gameObject.SetActive(false);
        }

        void SilenceConfetti()
        {
            if (confettiBurst == null)
                confettiBurst = FindConfettiBurst();
            StopConfetti();
        }

        void StopConfetti()
        {
            if (confettiBurst == null)
                return;

            ParticleSystem[] systems = confettiBurst.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
                main.playOnAwake = false;
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            confettiBurst.gameObject.SetActive(false);
        }

        static Transform FindConfettiBurst()
        {
            ParticleSystem[] systems = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < systems.Length; i++)
            {
                Transform cursor = systems[i].transform;
                while (cursor.parent != null)
                    cursor = cursor.parent;
                if (cursor.name == "Confetti")
                    return cursor;
            }

            return null;
        }

        IEnumerator SlideBanner(Vector2 rest)
        {
            float elapsed = 0f;
            Vector2 start = rest + Vector2.up * config.bannerSlideDistance;
            while (elapsed < config.bannerSlideSeconds && winBanner != null)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.bannerSlideSeconds);
                winBanner.anchoredPosition = Vector2.Lerp(start, rest, EaseOutBack(u));
                yield return null;
            }

            if (winBanner != null)
                winBanner.anchoredPosition = rest;
        }

        IEnumerator Nudge(RectTransform rect, Vector2 offset)
        {
            if (rect == null)
                yield break;
            Vector2 start = rect.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < config.beamTravelSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.beamTravelSeconds);
                float kick = u < 0.35f ? u / 0.35f : 1f - ((u - 0.35f) / 0.65f);
                rect.anchoredPosition = start + offset * kick;
                yield return null;
            }

            rect.anchoredPosition = start;
        }

        IEnumerator HideAfter(GameObject target, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (target != null)
                target.SetActive(false);
        }

        int IndexOfBob(RectTransform rect)
        {
            for (int i = 0; i < bobs.Count; i++)
            {
                if (bobs[i].rect == rect)
                    return i;
            }

            return -1;
        }

        static Color ColorFor(VirusType type)
        {
            switch (type)
            {
                case VirusType.Orange: return new Color(1f, 0.55f, 0.1f);
                case VirusType.Red: return new Color(1f, 0.15f, 0.15f);
                default: return new Color(0.2f, 0.95f, 0.35f);
            }
        }

        static float EaseOutBack(float u)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(u - 1f, 3f) + c1 * Mathf.Pow(u - 1f, 2f);
        }

        static void SetActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }
    }
}
