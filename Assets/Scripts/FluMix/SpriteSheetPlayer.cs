using System;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public class SpriteSheetPlayer : MonoBehaviour
    {
        [SerializeField] Image target;
        [SerializeField] Sprite[] frames;
        [SerializeField] GameConfig config;
        [SerializeField] Sprite idleSprite;

        int index;
        int rangeStart;
        int rangeEnd;
        float timer;
        bool playing;
        bool loop;
        Action finished;

        public bool HasFrames => frames != null && frames.Length > 0;
        public Sprite[] Frames => frames;

        public void AdoptConfig(GameConfig gameConfig)
        {
            config = gameConfig;
        }

        public void Bind(Image image, GameConfig gameConfig)
        {
            target = image;
            config = gameConfig;
            if (idleSprite == null && image != null)
                idleSprite = image.sprite;
        }

        public void SetFrames(Sprite[] sheetFrames, Sprite idle)
        {
            frames = sheetFrames;
            if (idle != null)
                idleSprite = idle;
        }

        public void SetIdle(Sprite sprite)
        {
            idleSprite = sprite;
        }

        public void PlayOnce(Action onFinished)
        {
            PlayRange(0, frames != null ? frames.Length : 0, false, onFinished);
        }

        public void PlayLoop()
        {
            PlayRange(0, frames != null ? frames.Length : 0, true, null);
        }

        bool restoreOnFinish = true;

        public void PlayRange(int start, int count, bool shouldLoop, Action onFinished, bool restoreIdle = true)
        {
            finished = onFinished;
            restoreOnFinish = restoreIdle;
            if (!HasFrames || count <= 0)
            {
                if (!shouldLoop)
                    Finish();
                return;
            }

            rangeStart = Mathf.Clamp(start, 0, frames.Length - 1);
            rangeEnd = Mathf.Clamp(start + count, rangeStart + 1, frames.Length);
            loop = shouldLoop;
            playing = true;
            index = rangeStart;
            timer = 0f;
            ApplyFrame();
        }

        public void Stop(bool restoreIdle = true)
        {
            playing = false;
            finished = null;
            if (restoreIdle && target != null && idleSprite != null)
                target.sprite = idleSprite;
        }

        void Awake()
        {
            if (target == null)
                target = GetComponent<Image>();
            if (idleSprite == null && target != null)
                idleSprite = target.sprite;
        }

        void Update()
        {
            if (!playing || !HasFrames || target == null || config == null)
                return;

            float frameDuration = 1f / Mathf.Max(1f, config.sheetFps);
            timer += Time.deltaTime;
            if (timer < frameDuration)
                return;

            timer -= frameDuration;
            index++;
            if (index >= rangeEnd)
            {
                if (loop)
                {
                    index = rangeStart;
                    ApplyFrame();
                    return;
                }

                playing = false;
                index = rangeEnd - 1;
                Finish();
                return;
            }

            ApplyFrame();
        }

        void ApplyFrame()
        {
            if (target != null && frames[index] != null)
                target.sprite = frames[index];
        }

        void Finish()
        {
            if (restoreOnFinish && target != null && idleSprite != null)
                target.sprite = idleSprite;
            Action done = finished;
            finished = null;
            done?.Invoke();
        }
    }
}
