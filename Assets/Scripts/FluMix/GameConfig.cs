using System;
using UnityEngine;

namespace FluMix
{
    public enum HeroId
    {
        A,
        G,
        T
    }

    public enum VirusType
    {
        Green,
        Orange,
        Red
    }

    [Serializable]
    public struct HeroVirusMatch
    {
        public VirusType virus;
        public HeroId hero;
    }

    [CreateAssetMenu(fileName = "GameConfig", menuName = "FluMix/Game Config")]
    public class GameConfig : ScriptableObject
    {
        public int correctHeroScore = 100;
        public int wrongHeroScore = -50;
        public int virusReachScore = -50;
        public int correctQuestionScore = 200;
        public int wrongQuestionScore = -100;

        public float roundSeconds = 120f;
        public float questionSeconds = 10f;
        public float bossCountdownStep = 1f;
        public float explainHoldSeconds = 1.2f;
        public float virusMoveSeconds = 8f;
        public float spawnDelay = 0.6f;

        public int[] bossScoreMilestones = { 1000, 2000, 5000, 10000 };
        public int maxQuestions = 10;
        public int simultaneousViruses = 3;
        public bool pauseRoundTimerDuringBoss = true;

        public HeroVirusMatch[] match =
        {
            new HeroVirusMatch { virus = VirusType.Green, hero = HeroId.A },
            new HeroVirusMatch { virus = VirusType.Orange, hero = HeroId.G },
            new HeroVirusMatch { virus = VirusType.Red, hero = HeroId.T }
        };

        public float beamTravelSeconds = 0.18f;
        public float hitStopSeconds = 0.08f;
        public float screenShake = 18f;
        public float sheetFps = 12f;

        public float wrongBeamGap = 80f;
        public float scorePopupRise = 80f;
        public float scorePopupSeconds = 0.75f;
        public float bossLungePixels = 40f;
        public float bossLungeSeconds = 0.35f;
        public float idleBobPixels = 6f;
        public float idleBobSeconds = 1.6f;
        public float virusPulseMin = 0.96f;
        public float virusPulseMax = 1.04f;
        public float virusPulseSeconds = 0.8f;
        public float beamStartScale = 0.15f;
        public float beamThickness = 72f;
        public float beamCoreThickness = 14f;
        public float beamWobble = 8f;
        public float beamArtAngle = -25f;
        public float hitFlashSeconds = 0.2f;
        public float hitFlashPeakScale = 1.2f;
        public float whiteFlashSeconds = 0.08f;
        public float virusRetypeDegrees = 12f;
        public float warningStartScale = 0.8f;
        public float warningPopSeconds = 0.45f;
        public float countdownPopScale = 1.35f;
        public int countdownFrom = 3;
        public float bossEnterScale = 0.4f;
        public float bossShakePixels = 5f;
        public float bossShakeSpeed = 18f;
        public float darkenAlpha = 0.6f;
        public float winShieldDuration = 0.45f;
        public float winShieldStartScale = 0.4f;
        public float winShieldPeakScale = 1.05f;
        public float winShieldSwayDegrees = 8f;
        public int confettiCount = 24;
        public float confettiSeconds = 1.5f;
        public float redFlashSeconds = 0.15f;
        public float redFlashAlpha = 0.45f;
        public float shakeSeconds = 0.28f;
        public float deathSpinDegrees = 240f;
        public float deathSeconds = 0.28f;
        public float laneSpacing = 150f;
        public float heroRecoilPixels = 18f;
        public float wrongMarkWidth = 220f;
        public float wrongMarkHeight = 64f;
        public float wrongMarkSeconds = 0.45f;
        public float bannerSlideDistance = 420f;
        public float bannerSlideSeconds = 0.4f;
        public float bossDefeatSeconds = 0.55f;
        public float scorePopupSize = 180f;
        public float explainDisabledSeconds = 0f;
        public int debugBumpScore = 1000;
        public float debugQuickSeconds = 5f;
        public float maxDeltaTime = 0.1f;

        public HeroId HeroFor(VirusType virus)
        {
            if (match == null)
                return HeroId.A;

            for (int i = 0; i < match.Length; i++)
            {
                if (match[i].virus == virus)
                    return match[i].hero;
            }

            return HeroId.A;
        }
    }
}
