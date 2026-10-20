using UnityEngine;

namespace FluMix
{
    public class WorldBillboard : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;
        float fit = 1f;
        float requestedHeight = 1.6f;
        bool fitted;

        public VirusView ClickTarget { get; set; }
        float drawnFit = 1f;
        float drawnPulse = 1f;
        Color drawnTint = Color.white;
        bool hasDrawn;

        public void Prepare(float worldHeight, Sprite sprite)
        {
            requestedHeight = worldHeight > 0f ? worldHeight : requestedHeight;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            if (sprite != null)
                spriteRenderer.sprite = sprite;
            Fit(worldHeight, sprite);
        }

        public void Present(Camera view, Vector3 world, Sprite sprite, Vector3 uiScale, float rollDegrees, Color tint, Sprite scaleSprite = null)
        {
            if (spriteRenderer == null)
                Prepare(1.6f, sprite);

            transform.position = world;
            if (sprite != null)
                spriteRenderer.sprite = sprite;
            Sprite fitted = scaleSprite != null ? scaleSprite : sprite;
            if (fitted != null)
                Fit(requestedHeight, fitted);

            if (view != null)
            {
                Vector3 away = world - view.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(away, Vector3.up) * Quaternion.Euler(0f, 0f, -rollDegrees);
            }

            float vanish = VanishOf();
            float pulse = uiScale.x > 0.05f ? uiScale.x : 1f;
            transform.localScale = Vector3.one * (fit * vanish * pulse);
            Color color = tint;
            color.a *= vanish;
            spriteRenderer.color = color;
            drawnFit = fit;
            drawnPulse = pulse;
            drawnTint = tint;
            hasDrawn = true;
        }

        public void ApplyVanish()
        {
            if (!hasDrawn || spriteRenderer == null)
                return;
            float vanish = VanishOf();
            transform.localScale = Vector3.one * (drawnFit * vanish * drawnPulse);
            Color color = drawnTint;
            color.a *= vanish;
            spriteRenderer.color = color;
        }

        float VanishOf()
        {
            return ClickTarget != null ? Mathf.Clamp01(ClickTarget.Vanish) : 1f;
        }

        public bool HitScreen(Camera cam, Vector2 screen)
        {
            if (ClickTarget == null || spriteRenderer == null || cam == null || !spriteRenderer.enabled)
                return false;

            Bounds world = spriteRenderer.bounds;
            Vector3 center = cam.WorldToScreenPoint(world.center);
            if (center.z <= 0f)
                return false;

            Vector3 right = cam.WorldToScreenPoint(world.center + new Vector3(world.extents.x, 0f, 0f));
            Vector3 up = cam.WorldToScreenPoint(world.center + new Vector3(0f, world.extents.y, 0f));
            float halfWidth = Mathf.Max(36f, Mathf.Abs(right.x - center.x));
            float halfHeight = Mathf.Max(36f, Mathf.Abs(up.y - center.y));
            return Mathf.Abs(screen.x - center.x) <= halfWidth && Mathf.Abs(screen.y - center.y) <= halfHeight;
        }

        void Fit(float worldHeight, Sprite sprite)
        {
            if (sprite == null || sprite.bounds.size.y < 0.001f || worldHeight <= 0f)
                return;
            fit = worldHeight / sprite.bounds.size.y;
            fitted = true;
        }
    }
}
