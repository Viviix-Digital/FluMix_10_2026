using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public class LobbyController : MonoBehaviour
    {
        public ParticleSystem LeftJet;
        public ParticleSystem RightJet;
        public ParticleSystem LeftSplash;
        public ParticleSystem RightSplash;
        bool _intro;

        void Start()
        {
            transform.Find("HowTo").GetComponent<Button>().onClick.AddListener(() => FluMixFlow.Instance.GoHowTo());
            transform.Find("Fight").GetComponent<Button>().onClick.AddListener(() => StartCoroutine(WaterThenBattle()));
            transform.Find("Board").GetComponent<Button>().onClick.AddListener(() => FluMixFlow.Instance.GoLeaderboard());
            SetJets(false);
            if (FluMixFlow.Instance != null && FluMixFlow.Instance.State == FlowState.WaterIntro)
                StartCoroutine(WaterThenBattle());
        }

        public void PlayWaterIsolated()
        {
            StartCoroutine(WaterOnly());
        }

        IEnumerator WaterOnly()
        {
            SetJets(true);
            if (FluMixAudio.Instance != null)
            {
                FluMixAudio.Instance.Play("stinger_sos");
                FluMixAudio.Instance.Play("water_loop");
            }
            yield return new WaitForSecondsRealtime(1.6f);
            SetJets(false);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.StopWater();
        }

        IEnumerator WaterThenBattle()
        {
            if (_intro) yield break;
            _intro = true;
            FluMixFlow.Instance.State = FlowState.WaterIntro;
            FluMixLog.Info("Flow", "WaterIntro");
            var cfg = FluMixFlow.Instance.Motion;
            if (cfg != null && cfg.reducedMotion)
            {
                FluMixFlow.Instance.BeginBattle();
                yield break;
            }
            SetJets(true);
            if (FluMixAudio.Instance != null)
            {
                FluMixAudio.Instance.Play("stinger_sos");
                FluMixAudio.Instance.Play("water_loop");
            }
            yield return new WaitForSecondsRealtime(1.6f);
            var flash = transform.Find("Flash");
            if (flash != null)
            {
                var img = flash.GetComponent<Image>();
                flash.gameObject.SetActive(true);
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.unscaledDeltaTime;
                    var c = img.color;
                    c.a = Mathf.Clamp01(t / 0.25f);
                    img.color = c;
                    yield return null;
                }
            }
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.StopWater();
            SetJets(false);
            FluMixFlow.Instance.BeginBattle();
        }

        void SetJets(bool on)
        {
            if (LeftJet != null) { if (on) LeftJet.Play(); else LeftJet.Stop(); }
            if (RightJet != null) { if (on) RightJet.Play(); else RightJet.Stop(); }
            if (LeftSplash != null) { if (on) LeftSplash.Play(); else LeftSplash.Stop(); }
            if (RightSplash != null) { if (on) RightSplash.Play(); else RightSplash.Stop(); }
        }

        void Update()
        {
            var siren = transform.Find("Siren");
            if (siren != null) siren.Rotate(0f, 0f, 90f * Time.unscaledDeltaTime);
            var rings = transform.Find("Rings");
            if (rings == null) return;
            for (int i = 0; i < rings.childCount; i++)
            {
                float t = Mathf.Repeat(Time.unscaledTime * 0.45f + i * 0.4f, 1.4f) / 1.4f;
                float s = Mathf.Lerp(0.4f, 1.6f, t);
                rings.GetChild(i).localScale = Vector3.one * s;
                var img = rings.GetChild(i).GetComponent<Image>();
                if (img != null)
                {
                    var c = img.color;
                    c.a = 1f - t;
                    img.color = c;
                }
            }
            var sos = transform.Find("Sos");
            if (sos != null)
            {
                var cg = sos.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0.85f + 0.15f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
            }
        }
    }
}
