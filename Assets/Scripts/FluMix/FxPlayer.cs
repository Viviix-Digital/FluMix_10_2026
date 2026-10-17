using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    /// <summary>
    /// Section 7 tweens. hero_a_attack and hero_g_attack are single attack sheets.
    /// boss_states is one 16-frame sheet: frames 0-3 idle, 4-7 attack, 8-11 defeat.
    /// virus_states is one 16-frame sheet for the green virus: frames 0-7 idle, 8-11 hit.
    /// The virus sheet has no shrink, so death still uses the scale tween.
    /// Hero T and the win shield were not animated in this pass.
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
        readonly List<Image> confetti = new List<Image>();
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

            while (confetti.Count < (config != null ? config.confettiCount : 24))
            {
                Image piece = FluMixUi.CreateImage(shakeRoot != null ? shakeRoot : fxRoot, "Confetti", FluMixUi.White, new Vector2(18f, 28f), false);
                piece.gameObject.SetActive(false);
                confetti.Add(piece);
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

            if (warningPulse && warningPanel != null)
            {
                float pulse = 0.72f + 0.28f * (Mathf.Sin(Time.time * 6f) * 0.5f + 0.5f);
                var image = warningPanel.GetComponent<Image>();
                if (image != null)
                    image.color = new Color(pulse, pulse, pulse, 1f);
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
            warningPulse = false;
            bossShaking = false;
        }

        public IEnumerator PlayWarning()
        {
            if (darken != null)
            {
                darken.transform.SetAsLastSibling();
                if (warningPanel != null)
                    warningPanel.SetAsLastSibling();
                if (countdown != null)
                    countdown.rectTransform.SetAsLastSibling();
            }

            if (warningPanel != null)
                warningPanel.gameObject.SetActive(true);
            if (countdown != null)
                countdown.gameObject.SetActive(true);
            warningPulse = true;

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

        public IEnumerator PlayBossEnter()
        {
            if (boss == null)
                yield break;

            boss.gameObject.SetActive(true);
            boss.SetAsLastSibling();
            if (bossGroup != null)
                bossGroup.alpha = 1f;
            boss.localScale = Vector3.one * config.bossEnterScale;
            float elapsed = 0f;
            while (elapsed < config.warningPopSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.warningPopSeconds);
                boss.localScale = Vector3.one * Mathf.Lerp(config.bossEnterScale, 1f, EaseOutBack(u));
                yield return null;
            }

            boss.localScale = Vector3.one;
            bossRest = boss.anchoredPosition;
            if (bossSheet != null && bossIdleCount > 0)
                bossSheet.PlayRange(bossIdleStart, bossIdleCount, true, null);
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

            winShield.transform.SetAsLastSibling();
            winShield.gameObject.SetActive(true);
            bool hasSheet = winSheet != null && winSheet.HasFrames;
            if (hasSheet)
                winSheet.PlayLoop();

            float elapsed = 0f;
            float sway = 0f;
            while (elapsed < config.winShieldDuration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.winShieldDuration);
                float scale = u < 0.65f
                    ? Mathf.Lerp(config.winShieldStartScale, config.winShieldPeakScale, u / 0.65f)
                    : Mathf.Lerp(config.winShieldPeakScale, 1f, (u - 0.65f) / 0.35f);
                winShield.rectTransform.localScale = Vector3.one * scale;
                if (!hasSheet)
                {
                    sway += Time.deltaTime;
                    float angle = Mathf.Sin(sway * 3f) * config.winShieldSwayDegrees;
                    winShield.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
                }

                yield return null;
            }

            winShield.rectTransform.localScale = Vector3.one;
        }

        public void HideWinShield()
        {
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
            StartCoroutine(ConfettiRoutine());
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
            for (int i = 0; i < confetti.Count; i++)
                confetti[i].gameObject.SetActive(false);
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

        public IEnumerator PlayBeam(HeroId hero, Vector2 from, Vector2 to, bool connect)
        {
            if (beam == null)
                yield break;

            Vector2 direction = to - from;
            float distance = direction.magnitude;
            if (distance < 1f)
                distance = 1f;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized;
            float travel = connect ? distance : Mathf.Max(config.beamThickness, distance - config.wrongBeamGap);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float aligned = angle - config.beamArtAngle;

            beam.sprite = SpriteFor(hero);
            beam.gameObject.SetActive(true);
            beam.rectTransform.SetAsLastSibling();
            beam.rectTransform.anchoredPosition = from;
            beam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, aligned);
            beam.rectTransform.localScale = Vector3.one;

            float elapsed = 0f;
            while (elapsed < config.beamTravelSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.beamTravelSeconds);
                float length = Mathf.Lerp(travel * config.beamStartScale, travel, u);
                float wobble = Mathf.Sin(u * 30f) * config.beamWobble;
                beam.rectTransform.anchoredPosition = from + normal * wobble;
                beam.rectTransform.sizeDelta = new Vector2(length, config.beamThickness);
                if (beamCore != null)
                {
                    beamCore.rectTransform.sizeDelta = new Vector2(length, config.beamCoreThickness);
                    beamCore.rectTransform.anchoredPosition = Vector2.zero;
                }

                yield return null;
            }

            Vector2 end = from + direction.normalized * travel;
            yield return PlayImpact(end, angle);
            beam.gameObject.SetActive(false);
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
                virus.localScale = Vector3.Lerp(startScale, Vector3.zero, u);
                virus.localRotation = startRot * Quaternion.Euler(0f, 0f, config.deathSpinDegrees * u);
                yield return null;
            }
        }

        public IEnumerator PlayRetype(VirusView virus, System.Action swap)
        {
            if (virus == null)
                yield break;

            var view = virus;
            view.PlayHit();
            Image flash = virus.Flash;
            RectTransform rect = virus.Rect;
            Quaternion start = rect.localRotation;
            float elapsed = 0f;
            while (virus != null && elapsed < config.whiteFlashSeconds)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / config.whiteFlashSeconds);
                if (flash != null)
                    flash.color = new Color(1f, 1f, 1f, Mathf.Sin(u * Mathf.PI));
                float turn = Mathf.Sin(u * Mathf.PI) * config.virusRetypeDegrees;
                rect.localRotation = start * Quaternion.Euler(0f, 0f, turn);
                yield return null;
            }

            if (virus == null)
                yield break;

            swap?.Invoke();
            if (flash != null)
                flash.color = new Color(1f, 1f, 1f, 0f);
            rect.localRotation = start;
            yield return PulseRing(virus);
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

        public void PlayScorePopup(int delta, Vector2 position)
        {
            Sprite sprite = delta >= config.correctQuestionScore ? plus200
                : delta > 0 ? plus100
                : minus50;
            if (sprite == null)
                return;

            Image popup = null;
            for (int i = 0; i < popups.Count; i++)
            {
                if (!popups[i].gameObject.activeSelf)
                {
                    popup = popups[i];
                    break;
                }
            }

            if (popup == null && popups.Count > 0)
                popup = popups[0];
            if (popup == null)
                return;

            popup.sprite = sprite;
            popup.color = Color.white;
            popup.rectTransform.sizeDelta = new Vector2(config.scorePopupSize, config.scorePopupSize * 0.5f);
            popup.gameObject.SetActive(true);
            popup.rectTransform.SetAsLastSibling();
            StartCoroutine(FloatPopup(popup, position));
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

        IEnumerator ConfettiRoutine()
        {
            int count = Mathf.Min(config.confettiCount, confetti.Count);
            var velocity = new Vector2[count];
            var spin = new float[count];
            Color[] colors =
            {
                new Color(1f, 0.35f, 0.7f),
                new Color(1f, 0.86f, 0.2f),
                new Color(0.25f, 0.55f, 1f),
                new Color(0.3f, 0.9f, 0.4f)
            };

            for (int i = 0; i < count; i++)
            {
                Image piece = confetti[i];
                piece.gameObject.SetActive(true);
                piece.transform.SetAsLastSibling();
                piece.color = colors[i % colors.Length];
                piece.rectTransform.anchoredPosition = Vector2.zero;
                piece.rectTransform.localScale = Vector3.one;
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(420f, 900f);
                velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                velocity[i].y = Mathf.Abs(velocity[i].y) + 200f;
                spin[i] = Random.Range(-180f, 180f);
            }

            float elapsed = 0f;
            while (elapsed < config.confettiSeconds)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                float u = Mathf.Clamp01(elapsed / config.confettiSeconds);
                for (int i = 0; i < count; i++)
                {
                    velocity[i] += Vector2.down * 1400f * dt;
                    confetti[i].rectTransform.anchoredPosition += velocity[i] * dt;
                    confetti[i].rectTransform.Rotate(0f, 0f, spin[i] * dt);
                    Color color = confetti[i].color;
                    color.a = 1f - u;
                    confetti[i].color = color;
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
                confetti[i].gameObject.SetActive(false);
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

        Sprite SpriteFor(HeroId hero)
        {
            switch (hero)
            {
                case HeroId.G: return beamG != null ? beamG : beamA;
                case HeroId.T: return beamT != null ? beamT : beamA;
                default: return beamA;
            }
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
