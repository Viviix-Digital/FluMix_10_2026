using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluMix
{
    public class Beam : MonoBehaviour
    {
        LineRenderer _core;
        LineRenderer _fringe;
        bool _alive;

        public void Build(Color color)
        {
            _fringe = gameObject.AddComponent<LineRenderer>();
            Setup(_fringe, 0.22f, new Color(color.r, color.g, color.b, 0.85f));
            var coreGo = new GameObject("Core");
            coreGo.transform.SetParent(transform, false);
            _core = coreGo.AddComponent<LineRenderer>();
            Setup(_core, 0.07f, Color.white);
        }

        static void Setup(LineRenderer line, float width, Color color)
        {
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width * 0.4f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.numCapVertices = 4;
            line.sortingOrder = 20;
            line.useWorldSpace = true;
        }

        public void Fire(Vector3 from, Vector3 to, Color color, bool correct, float duration)
        {
            if (_fringe == null) Build(color);
            _alive = true;
            StopAllCoroutines();
            StartCoroutine(Run(from, to, color, correct, duration));
        }

        IEnumerator Run(Vector3 from, Vector3 to, Color color, bool correct, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                Vector3 tip = Vector3.Lerp(from, to, Mathf.Clamp01(k * 1.4f));
                float n = 1f + Mathf.Sin(Time.unscaledTime * 6f * Mathf.PI * 2f) * 0.25f;
                _fringe.startWidth = 0.22f * n;
                _core.startWidth = 0.07f * n;
                if (!correct && k > 0.8f)
                {
                    _fringe.startColor = Color.gray;
                    _fringe.endColor = Color.gray;
                }
                _fringe.SetPosition(0, from);
                _fringe.SetPosition(1, tip);
                _core.SetPosition(0, from);
                _core.SetPosition(1, tip);
                yield return null;
            }
            _alive = false;
            _fringe.positionCount = 0;
            _core.positionCount = 0;
            _fringe.positionCount = 2;
            _core.positionCount = 2;
        }

        public bool Alive => _alive;
    }
}
