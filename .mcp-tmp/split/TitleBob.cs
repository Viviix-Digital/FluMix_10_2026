using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public class TitleBob : MonoBehaviour
    {
        RectTransform[] _glyphs;
        Vector2 _root;
        bool _ready;

        public void Play(RectTransform[] glyphs)
        {
            _glyphs = glyphs;
            _root = ((RectTransform)transform).anchoredPosition;
            _ready = true;
            for (int i = 0; i < glyphs.Length; i++)
            {
                glyphs[i].localScale = Vector3.zero;
                glyphs[i].localRotation = Quaternion.Euler(0f, 0f, -8f);
            }
            StartCoroutine(Intro());
        }

        IEnumerator Intro()
        {
            for (int i = 0; i < _glyphs.Length; i++)
            {
                StartCoroutine(Pop(_glyphs[i]));
                yield return new WaitForSecondsRealtime(0.04f);
            }
        }

        IEnumerator Pop(RectTransform rt)
        {
            float t = 0f;
            while (t < 0.28f)
            {
                t += Time.unscaledDeltaTime;
                float k = UiMotion.EaseOutBack(Mathf.Clamp01(t / 0.28f));
                rt.localScale = Vector3.one * k;
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-8f, 0f, k));
                yield return null;
            }
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        void Update()
        {
            if (!_ready) return;
            float y = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 2.4f) * 3f;
            ((RectTransform)transform).anchoredPosition = _root + new Vector2(0f, y);
        }
    }
}
