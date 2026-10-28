using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public sealed class AimDashes
    {
        readonly Image[] marks;

        public AimDashes(RectTransform parent, int count)
        {
            marks = new Image[count];
            for (int i = 0; i < count; i++)
            {
                Image mark = FluMixUi.CreateImage(parent, "Dash", FluMixUi.White, new Vector2(18f, 6f), false);
                mark.color = Color.white;
                mark.enabled = false;
                marks[i] = mark;
            }
        }

        public void Hide()
        {
            for (int i = 0; i < marks.Length; i++)
                marks[i].enabled = false;
        }

        public void Draw(Vector2 origin, Vector2 end, Color color)
        {
            Vector2 delta = end - origin;
            float distance = delta.magnitude;
            if (distance < 24f)
            {
                Hide();
                return;
            }

            Vector2 dir = delta / distance;
            float gap = Mathf.Max(22f, distance / Mathf.Max(1, marks.Length));
            int placed = 0;
            for (float cursor = gap * 0.5f; cursor < distance - 8f && placed < marks.Length - 1; cursor += gap)
            {
                Place(placed, origin + dir * cursor, dir, cursor / distance, Mathf.Min(18f, gap * 0.62f), color);
                placed++;
            }

            if (placed < marks.Length)
            {
                Place(placed, end, dir, 1f, Mathf.Min(18f, gap * 0.62f), color);
                placed++;
            }

            for (int i = placed; i < marks.Length; i++)
                marks[i].enabled = false;
        }

        public static float ExitDistance(Vector2 origin, Vector2 direction, Rect bounds)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return 0f;

            Vector2 dir = direction.normalized;
            float best = 4000f;
            if (Mathf.Abs(dir.x) > 0.0001f)
            {
                Consider((bounds.xMin - origin.x) / dir.x, origin, dir, bounds, ref best);
                Consider((bounds.xMax - origin.x) / dir.x, origin, dir, bounds, ref best);
            }

            if (Mathf.Abs(dir.y) > 0.0001f)
            {
                Consider((bounds.yMin - origin.y) / dir.y, origin, dir, bounds, ref best);
                Consider((bounds.yMax - origin.y) / dir.y, origin, dir, bounds, ref best);
            }

            return best >= 4000f ? 0f : best;
        }

        static void Consider(float distance, Vector2 origin, Vector2 dir, Rect bounds, ref float best)
        {
            if (distance < 8f || distance >= best)
                return;

            Vector2 point = origin + dir * distance;
            if (point.x < bounds.xMin - 2f || point.x > bounds.xMax + 2f || point.y < bounds.yMin - 2f || point.y > bounds.yMax + 2f)
                return;

            best = distance;
        }

        void Place(int index, Vector2 point, Vector2 tangent, float along, float length, Color color)
        {
            Image mark = marks[index];
            mark.enabled = true;
            RectTransform rect = mark.rectTransform;
            rect.anchoredPosition = point;
            rect.sizeDelta = new Vector2(length, 6f);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
            float alpha = Mathf.Lerp(1f, 0.45f, along) * color.a;
            mark.color = new Color(color.r, color.g, color.b, alpha);
        }
    }

    public sealed class AimArc
    {
        readonly Image[] ticks;

        public AimArc(RectTransform parent, int count)
        {
            ticks = new Image[count];
            for (int i = 0; i < count; i++)
            {
                Image tick = FluMixUi.CreateImage(parent, "Arc", FluMixUi.White, new Vector2(14f, 5f), false);
                tick.color = new Color(1f, 1f, 1f, 0.55f);
                tick.enabled = false;
                ticks[i] = tick;
            }
        }

        public void Hide()
        {
            for (int i = 0; i < ticks.Length; i++)
                ticks[i].enabled = false;
        }

        public void Draw(Vector2 origin, Vector2 forward)
        {
            if (forward.sqrMagnitude < 1f)
            {
                Hide();
                return;
            }

            Vector2 dir = forward.normalized;
            float radius = 96f;
            float facing = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < ticks.Length; i++)
            {
                float u = ticks.Length == 1 ? 0.5f : i / (float)(ticks.Length - 1);
                float angle = (facing + Mathf.Lerp(-90f, 90f, u)) * Mathf.Deg2Rad;
                Vector2 point = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Image tick = ticks[i];
                tick.enabled = true;
                tick.rectTransform.anchoredPosition = point;
                tick.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle * Mathf.Rad2Deg);
                tick.color = new Color(1f, 1f, 1f, 0.7f);
            }
        }
    }

    public sealed class PullLine
    {
        readonly Image shaft;

        public PullLine(RectTransform parent)
        {
            shaft = FluMixUi.CreateImage(parent, "PullLine", FluMixUi.White, new Vector2(8f, 8f), false);
            shaft.rectTransform.pivot = new Vector2(0f, 0.5f);
            shaft.color = new Color(1f, 1f, 1f, 0.9f);
            shaft.enabled = false;
        }

        public void Hide()
        {
            shaft.enabled = false;
        }

        public void Draw(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 8f)
            {
                Hide();
                return;
            }

            shaft.enabled = true;
            shaft.rectTransform.anchoredPosition = from;
            shaft.rectTransform.sizeDelta = new Vector2(length, 7f);
            shaft.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }

    public sealed class SwipeHint
    {
        static Sprite fingerSprite;
        readonly Image finger;
        readonly Image[] trail;

        public SwipeHint(RectTransform parent)
        {
            trail = new Image[5];
            for (int i = 0; i < trail.Length; i++)
            {
                Image dot = FluMixUi.CreateImage(parent, "SwipeDot", FluMixUi.White, new Vector2(10f, 10f), false);
                dot.color = new Color(1f, 1f, 1f, 0.35f);
                dot.enabled = false;
                trail[i] = dot;
            }

            finger = FluMixUi.CreateImage(parent, "SwipeFinger", Finger(), new Vector2(54f, 70f), false);
            finger.preserveAspect = true;
            finger.enabled = false;
        }

        public void Hide()
        {
            finger.enabled = false;
            for (int i = 0; i < trail.Length; i++)
                trail[i].enabled = false;
        }

        public void Show(Vector2 from, Vector2 to, float move, float fade)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 1f)
            {
                Hide();
                return;
            }

            Vector2 dir = delta.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
            for (int i = 0; i < trail.Length; i++)
            {
                float u = (i + 1f) / (trail.Length + 1f);
                bool on = u <= move;
                trail[i].enabled = on;
                trail[i].rectTransform.anchoredPosition = Vector2.Lerp(from, to, u);
                trail[i].color = new Color(1f, 1f, 1f, 0.4f * fade);
            }

            finger.enabled = fade > 0.02f;
            finger.rectTransform.anchoredPosition = Vector2.Lerp(from, to, move);
            finger.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
            finger.color = new Color(1f, 1f, 1f, fade);
        }

        static Sprite Finger()
        {
            if (fingerSprite != null)
                return fingerSprite;

            const int w = 64;
            const int h = 84;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            Vector2 tip = new Vector2(32f, 62f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - tip.x) / 16f;
                    float dy = (y - tip.y) / 18f;
                    float head = dx * dx + dy * dy;
                    float body = Mathf.Abs(x - 32f) / 13f + Mathf.Abs(y - 28f) / 26f;
                    float alpha = Mathf.Clamp01(Mathf.Max((1.05f - head) * 6f, (1.05f - body) * 5f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            fingerSprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.15f), 100f);
            return fingerSprite;
        }
    }
}
