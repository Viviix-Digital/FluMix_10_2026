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

        public void Draw(Vector2 origin, Vector2 end)
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
                Place(placed, origin + dir * cursor, dir, cursor / distance, Mathf.Min(18f, gap * 0.62f));
                placed++;
            }

            if (placed < marks.Length)
            {
                Place(placed, end, dir, 1f, Mathf.Min(18f, gap * 0.62f));
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

        void Place(int index, Vector2 point, Vector2 tangent, float along, float length)
        {
            Image mark = marks[index];
            mark.enabled = true;
            RectTransform rect = mark.rectTransform;
            rect.anchoredPosition = point;
            rect.sizeDelta = new Vector2(length, 6f);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
            mark.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.45f, along));
        }
    }
}
