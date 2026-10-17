using UnityEngine;

namespace FluMix
{
    public class WorldBillboard : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;
        float fit = 1f;
        float requestedHeight = 1.6f;
        bool fitted;

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

        public void Present(Camera view, Vector3 world, Sprite sprite, Vector3 uiScale, float rollDegrees, Color tint)
        {
            if (spriteRenderer == null)
                Prepare(1.6f, sprite);

            transform.position = world;
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
                if (!fitted)
                    Fit(requestedHeight, sprite);
            }

            float pulse = Mathf.Max(0.01f, Mathf.Max(Mathf.Abs(uiScale.x), Mathf.Abs(uiScale.y)));
            transform.localScale = Vector3.one * (fit * pulse);
            if (view != null)
            {
                Vector3 away = world - view.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(away, Vector3.up) * Quaternion.Euler(0f, 0f, -rollDegrees);
            }

            spriteRenderer.color = tint;
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
