using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public static class UiMotion
    {
        public static IEnumerator Enter(CanvasGroup group, RectTransform rect, UiMotionConfig cfg)
        {
            if (group == null || rect == null) yield break;
            Vector2 end = rect.anchoredPosition;
            float slide = cfg != null ? cfg.enterSlide : 24f;
            float dur = cfg != null ? cfg.enterDuration : 0.28f;
            group.alpha = 0f;
            rect.anchoredPosition = end + new Vector2(0f, -slide);
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = EaseOutCubic(Mathf.Clamp01(t / dur));
                group.alpha = k;
                rect.anchoredPosition = Vector2.Lerp(end + new Vector2(0f, -slide), end, k);
                yield return null;
            }
            group.alpha = 1f;
            rect.anchoredPosition = end;
        }

        public static IEnumerator ModalIn(CanvasGroup dim, RectTransform panel, UiMotionConfig cfg)
        {
            float dur = cfg != null && !cfg.reducedMotion ? cfg.modalInDuration : 0.12f;
            float start = cfg != null ? cfg.modalStartScale : 0.86f;
            float dimA = cfg != null ? cfg.modalDim : 0.45f;
            if (dim != null) dim.alpha = 0f;
            panel.localScale = Vector3.one * start;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = cfg != null && cfg.reducedMotion ? k : EaseOutBack(k);
                if (dim != null) dim.alpha = dimA * k;
                panel.localScale = Vector3.one * Mathf.Lerp(start, 1f, e);
                yield return null;
            }
            if (dim != null) dim.alpha = dimA;
            panel.localScale = Vector3.one;
        }

        public static IEnumerator Shake(RectTransform rect, float duration)
        {
            Vector2 origin = rect.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(t / duration);
                float x = Mathf.Sin(t / duration * 3f * Mathf.PI * 2f) * 8f * k;
                rect.anchoredPosition = origin + new Vector2(x, 0f);
                yield return null;
            }
            rect.anchoredPosition = origin;
        }

        public static IEnumerator Punch(RectTransform rect)
        {
            Vector3 baseScale = Vector3.one;
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.22f);
                float s = k < 0.45f ? Mathf.Lerp(1f, 1.18f, k / 0.45f) : Mathf.Lerp(1.18f, 1f, (k - 0.45f) / 0.55f);
                rect.localScale = baseScale * s;
                yield return null;
            }
            rect.localScale = baseScale;
        }

        public static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

        public static float EaseOutBack(float x)
        {
            const float c1 = 1.1f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }

}
