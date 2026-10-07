using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public class PillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Button Button;
        public Image Glow;
        public Image Ring;
        Vector3 _base = Vector3.one;
        bool _over;

        void Awake()
        {
            if (Button == null) Button = GetComponent<Button>();
        }

        void Update()
        {
            if (Button != null && !Button.interactable)
            {
                if (Glow != null) Glow.color = new Color(1f, 1f, 1f, 0.15f);
                return;
            }
            if (Glow != null)
            {
                float a = 0.65f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 1.1f));
                var c = Glow.color;
                c.a = a;
                Glow.color = c;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Button != null && !Button.interactable) return;
            _over = true;
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_hover");
            StopAllCoroutines();
            StartCoroutine(ScaleTo(_base * 1.05f, 0.12f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _over = false;
            StopAllCoroutines();
            StartCoroutine(ScaleTo(_base, 0.12f));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Button != null && !Button.interactable) return;
            StopAllCoroutines();
            StartCoroutine(Press());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_over) StartCoroutine(ScaleTo(_base * 1.05f, 0.08f));
        }

        IEnumerator Press()
        {
            yield return ScaleTo(_base * 0.96f, 0.06f);
            yield return ScaleTo(_base, 0.06f);
            if (Ring != null) StartCoroutine(RingBurst());
        }

        IEnumerator ScaleTo(Vector3 target, float dur)
        {
            Vector3 start = transform.localScale;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(start, target, Mathf.Clamp01(t / dur));
                yield return null;
            }
            transform.localScale = target;
        }

        IEnumerator RingBurst()
        {
            Ring.gameObject.SetActive(true);
            var rt = Ring.rectTransform;
            var c = Ring.color;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.25f);
                rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.35f, k);
                c.a = 1f - k;
                Ring.color = c;
                yield return null;
            }
            Ring.gameObject.SetActive(false);
        }
    }
}
