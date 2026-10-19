using System;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public class VirusView : MonoBehaviour
    {
        [SerializeField] Image image;
        [SerializeField] Image flash;
        [SerializeField] SpriteSheetPlayer sheet;

        public VirusType Type { get; private set; }
        public Image Image => image;
        public Image Flash => flash;
        public SpriteSheetPlayer Sheet =>
            sheet != null ? sheet : (sheet = GetComponent<SpriteSheetPlayer>());
        public RectTransform Rect => (RectTransform)transform;
        [SerializeField] int idleStart;
        [SerializeField] int idleCount;
        [SerializeField] int hitStart;
        [SerializeField] int hitCount;
        [SerializeField] int deathStart;
        [SerializeField] int deathCount;

        public bool Pulse = true;
        public Color Tint { get; private set; } = Color.white;
        public bool UsesSheet { get; private set; }
        public bool HasHit => UsesSheet && sheet != null && sheet.HasFrames && hitCount > 0;
        public bool HasDeath => UsesSheet && sheet != null && sheet.HasFrames && deathCount > 0;

        public void SetClip(int idleFrom, int idleLength, int hitFrom, int hitLength, int deathFrom, int deathLength)
        {
            idleStart = idleFrom;
            idleCount = idleLength;
            hitStart = hitFrom;
            hitCount = hitLength;
            deathStart = deathFrom;
            deathCount = deathLength;
        }

        public void PlayIdle()
        {
            if (!UsesSheet || sheet == null || !sheet.HasFrames || idleCount <= 0)
                return;
            Pulse = false;
            sheet.PlayRange(idleStart, idleCount, true, null);
        }

        public void PlayHit()
        {
            if (!HasHit)
                return;
            sheet.PlayRange(hitStart, hitCount, false, null, false);
        }

        public void PlayDeath()
        {
            if (!HasDeath)
                return;
            Pulse = false;
            sheet.PlayRange(deathStart, deathCount, false, null, false);
        }

        GameConfig config;
        Vector3 baseScale = Vector3.one;

        public void Bind(GameConfig gameConfig)
        {
            config = gameConfig;
        }

        public event Action<VirusView> Clicked;

        void OnEnable()
        {
            if (image == null)
                image = GetComponent<Image>();
            Button button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (image != null)
                image.raycastTarget = true;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        void OnDisable()
        {
            Button button = GetComponent<Button>();
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void NotifyClick()
        {
            Clicked?.Invoke(this);
        }

        void HandleClick()
        {
            NotifyClick();
        }

        void Awake()
        {
            if (image == null)
                image = GetComponent<Image>();
            if (sheet == null)
                sheet = GetComponent<SpriteSheetPlayer>();
            EnsureFlash();
            baseScale = Vector3.one;
        }

        public void Apply(VirusType type, Sprite sprite, bool playSheet)
        {
            Type = type;
            UsesSheet = playSheet;
            if (image == null)
                image = GetComponent<Image>();
            if (sheet == null)
                sheet = GetComponent<SpriteSheetPlayer>();
            if (image == null)
                return;

            image.color = Color.white;
            Tint = Color.white;
            if (playSheet && sheet != null && sheet.HasFrames)
            {
                if (idleCount > 0)
                    PlayIdle();
                else
                    sheet.PlayLoop();
                return;
            }

            Pulse = true;
            if (sheet != null)
            {
                sheet.Stop(false);
                sheet.SetIdle(sprite);
            }

            image.sprite = sprite;
        }

        void Update()
        {
            if (!Pulse || config == null)
                return;

            float duration = Mathf.Max(0.01f, config.virusPulseSeconds);
            float wave = (Mathf.Sin(Time.time * (Mathf.PI * 2f / duration)) + 1f) * 0.5f;
            float scale = Mathf.Lerp(config.virusPulseMin, config.virusPulseMax, wave);
            Rect.localScale = baseScale * scale;
        }

        public void SetTint(Color color)
        {
            Tint = color;
            if (image != null)
                image.color = color;
        }

        public void SetScale(Vector3 scale)
        {
            baseScale = scale;
            Rect.localScale = scale;
        }

        void EnsureFlash()
        {
            if (flash != null)
                return;

            Transform existing = transform.Find("Flash");
            if (existing != null)
            {
                flash = existing.GetComponent<Image>();
                return;
            }

            var go = new GameObject("Flash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            flash = go.GetComponent<Image>();
            flash.sprite = FluMixUi.White;
            flash.color = new Color(1f, 1f, 1f, 0f);
            flash.raycastTarget = false;
        }
    }
}
