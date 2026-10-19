using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public class HowToController : MonoBehaviour
    {
        void Start()
        {
            var panel = transform.Find("Panel");
            var group = panel.GetComponent<CanvasGroup>();
            StartCoroutine(UiMotion.Enter(group, panel as RectTransform, FluMixFlow.Instance != null ? FluMixFlow.Instance.Motion : null));
            var title = panel.Find("Title");
            var bob = title.GetComponent<TitleBob>();
            if (bob != null)
            {
                var glyphs = new RectTransform[title.childCount];
                for (int i = 0; i < title.childCount; i++) glyphs[i] = title.GetChild(i) as RectTransform;
                bob.Play(glyphs);
            }
            transform.Find("Ready").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_confirm");
                FluMixFlow.Instance.BeginBattle();
            });
            StartCoroutine(StaggerRows(panel));
        }

        void Update()
        {
            var dots = GetComponentsInChildren<Image>(true);
            float p = 0.75f + 0.25f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f));
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i].name != "Dot") continue;
                var c = dots[i].color;
                c.a = p;
                dots[i].color = c;
            }
        }

        IEnumerator StaggerRows(Transform panel)
        {
            for (int i = 0; i < panel.childCount; i++)
            {
                var cg = panel.GetChild(i).GetComponent<CanvasGroup>();
                if (cg == null) continue;
                cg.alpha = 0f;
            }
            for (int i = 0; i < panel.childCount; i++)
            {
                var cg = panel.GetChild(i).GetComponent<CanvasGroup>();
                if (cg == null) continue;
                float t = 0f;
                var rt = cg.GetComponent<RectTransform>();
                Vector2 end = rt.anchoredPosition;
                while (t < 0.2f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / 0.2f);
                    cg.alpha = k;
                    rt.anchoredPosition = end + new Vector2(0f, Mathf.Lerp(16f, 0f, k));
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.05f);
            }
        }
    }
}
