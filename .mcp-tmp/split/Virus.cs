using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluMix
{
    public class Virus : MonoBehaviour
    {
        public VirusArchetype Archetype;
        public int Tier;
        public float NormalizedX;
        SpriteRenderer _body;
        SpriteRenderer _outline;
        Transform _eyes;
        Transform _mote;
        bool _dead;
        float _bob;

        public void Setup(VirusArchetype archetype, Sprite body, Sprite outline)
        {
            Archetype = archetype;
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(transform, false);
            _body = bodyGo.AddComponent<SpriteRenderer>();
            _body.sprite = body;
            _body.color = archetype.color;
            _body.sortingOrder = 10;
            var outGo = new GameObject("Outline");
            outGo.transform.SetParent(transform, false);
            outGo.transform.localScale = Vector3.one * 1.18f;
            _outline = outGo.AddComponent<SpriteRenderer>();
            _outline.sprite = outline;
            _outline.color = new Color(archetype.color.r, archetype.color.g, archetype.color.b, 0f);
            _outline.sortingOrder = 9;
            _eyes = new GameObject("Eyes").transform;
            _eyes.SetParent(transform, false);
            MakeEye(new Vector3(-0.12f, 0.08f, -0.1f));
            MakeEye(new Vector3(0.12f, 0.08f, -0.1f));
            _mote = new GameObject("Mote").transform;
            _mote.SetParent(transform, false);
            var mote = _mote.gameObject.AddComponent<SpriteRenderer>();
            mote.sprite = body;
            mote.color = Color.white;
            mote.sortingOrder = 11;
            _mote.localScale = Vector3.one * 0.18f;
            _mote.gameObject.SetActive(false);
            transform.localScale = Vector3.zero;
            StartCoroutine(Pop());
        }

        void MakeEye(Vector3 pos)
        {
            var eye = new GameObject("Eye");
            eye.transform.SetParent(_eyes, false);
            eye.transform.localPosition = pos;
            eye.transform.localScale = Vector3.one * 0.16f;
            var r = eye.AddComponent<SpriteRenderer>();
            r.sprite = _body.sprite;
            r.color = new Color(0.05f, 0.08f, 0.15f);
            r.sortingOrder = 12;
        }

        IEnumerator Pop()
        {
            float t = 0f;
            while (t < 0.28f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.28f);
                float s = EaseOutBack(k);
                transform.localScale = Vector3.one * s;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        public void Tick(float dt, float gateX)
        {
            if (_dead || Archetype == null) return;
            float speed = Archetype.baseSpeed;
            if (Tier >= 2) speed *= 1.5f;
            else if (Tier == 1) speed *= 1.25f;
            var rules = FluMixFlow.Instance != null ? FluMixFlow.Instance.Rules : null;
            if (rules != null)
            {
                if (Tier >= 2) speed = Archetype.baseSpeed * rules.tier2Speed;
                else if (Tier == 1) speed = Archetype.baseSpeed * rules.tier1Speed;
            }
            transform.position += Vector3.left * speed * dt;
            _bob += dt * 6f;
            _body.transform.localPosition = new Vector3(0f, Mathf.Sin(_bob) * 0.08f, 0f);
            float blink = Mathf.Repeat(_bob, 3.2f) > 3.05f ? 0.15f : 1f;
            _eyes.localScale = new Vector3(1f, blink, 1f);
            if (Tier >= 2 && _mote != null)
            {
                _mote.gameObject.SetActive(true);
                float a = Time.time * 4f;
                _mote.localPosition = new Vector3(Mathf.Cos(a) * 0.45f, Mathf.Sin(a) * 0.45f, -0.2f);
            }
            float min = -8f;
            float max = 8f;
            NormalizedX = Mathf.InverseLerp(max, min, transform.position.x);
        }

        public bool ReachedGate(float gateX) => !_dead && transform.position.x <= gateX;

        public void Evolve()
        {
            if (Tier >= 2) return;
            Tier++;
            FluMixLog.Info("Combat", "Evolve " + Archetype.guardian + " tier " + Tier);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("virus_evolve");
            StartCoroutine(EvolvePunch());
        }

        IEnumerator EvolvePunch()
        {
            _outline.color = new Color(Archetype.color.r, Archetype.color.g, Archetype.color.b, Tier > 0 ? 0.9f : 0f);
            if (Tier >= 2)
            {
                foreach (Transform eye in _eyes)
                {
                    var r = eye.GetComponent<SpriteRenderer>();
                    if (r != null) r.color = new Color(0.8f, 0.1f, 0.15f);
                }
            }
            float t = 0f;
            Color flash = Color.white;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                float k = t / 0.35f;
                float s = k < 0.4f ? Mathf.Lerp(1f, 1.25f, k / 0.4f) : Mathf.Lerp(1.25f, 1.1f, (k - 0.4f) / 0.6f);
                transform.localScale = Vector3.one * s;
                _body.color = Color.Lerp(flash, Archetype.color, k);
                yield return null;
            }
            _body.color = Archetype.color;
            transform.localScale = Vector3.one * (Tier >= 1 ? 1.1f : 1f);
        }

        public void Die()
        {
            if (_dead) return;
            _dead = true;
            StartCoroutine(Shrink());
        }

        IEnumerator Shrink()
        {
            float t = 0f;
            Vector3 start = transform.localScale;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.25f);
                transform.localScale = Vector3.Lerp(start, Vector3.zero, k);
                var c = _body.color;
                c.a = 1f - k;
                _body.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
