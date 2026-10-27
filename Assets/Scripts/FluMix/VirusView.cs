using System;
using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public class VirusView : MonoBehaviour
    {
        [SerializeField] Image image;
        [SerializeField] Image flash;
        [SerializeField] SpriteSheetPlayer sheet;
        [Tooltip("XY offset from virus in local space (trail sits slightly behind/above).")]
        [SerializeField] Vector2 flightSmokeLocalXY = new Vector2(0f, 40f);
        [Tooltip("Preferred world Z. If that point is behind Main Camera, ShotPlaneZ is used instead.")]
        [SerializeField] float flightSmokeWorldZ = -1300f;
        UIEffect uiEffect;
        GameObject flightSmoke;

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
        public float Vanish { get; set; } = 1f;
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
            // Combat uses hero ray aim — virus must not steal hero swipe hits.
            if (image != null)
                image.raycastTarget = false;
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.enabled = false;
            }
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
            Vanish = 1f;
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

        public void SetEntranceFlight(GameObject smokePrefab)
        {
            UIEffect effect = Effect();
            if (effect != null)
            {
                effect.samplingFilter = SamplingFilter.BlurDetail;
                // Intensity is clamped 0–1 by the package; push past default look via width/scale.
                effect.samplingIntensity = 1f;
                effect.samplingWidth = 7f;
                effect.samplingScale = 5f;
                effect.shadowMode = ShadowMode.None;
            }

            StartFlightSmoke(smokePrefab);
        }

        public void SetEntranceLanded()
        {
            StopFlightSmoke();
            UIEffect effect = Effect();
            if (effect == null)
                return;
            effect.samplingFilter = SamplingFilter.None;
            effect.samplingIntensity = 0.5f;
            effect.samplingWidth = 1f;
            effect.samplingScale = 1f;
            effect.shadowMode = ShadowMode.Mirror;
            effect.shadowMirrorScale = 0.6f;
            effect.shadowFade = 0.252f;
            effect.shadowDistance = new Vector2(0.4f, 0f);
        }

        void LateUpdate()
        {
            if (flightSmoke != null)
                SyncFlightSmokePose();
        }

        void StartFlightSmoke(GameObject prefab)
        {
            StopFlightSmoke();
            if (prefab == null)
                return;

            // World space (not UI child): Overlay canvas does not feed ParticleSystemRenderer.
            FluMixShotVfx shots = FindAnyObjectByType<FluMixShotVfx>();
            Transform parent = shots != null ? shots.transform : null;
            flightSmoke = parent != null ? Instantiate(prefab, parent) : Instantiate(prefab);
            flightSmoke.name = prefab.name + " (Flight)";
            EnsureCameraSeesSmokeDepth();
            SyncFlightSmokePose();
            ParticleSystem[] systems = flightSmoke.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                systems[i].Play(true);
            }
        }

        void SyncFlightSmokePose()
        {
            if (flightSmoke == null)
                return;

            Vector3 world = transform.TransformPoint(new Vector3(flightSmokeLocalXY.x, flightSmokeLocalXY.y, 0f));
            world.z = ResolveSmokeWorldZ(world);
            flightSmoke.transform.SetPositionAndRotation(world, Quaternion.identity);
        }

        float ResolveSmokeWorldZ(Vector3 worldXY)
        {
            Camera cam = Camera.main;
            float preferred = flightSmokeWorldZ;
            if (cam == null)
                return preferred;

            Vector3 probe = worldXY;
            probe.z = preferred;
            // Main Camera looks +Z from ~z=17; absolute -1300 is behind it (viewport.z < 0).
            if (cam.WorldToViewportPoint(probe).z > cam.nearClipPlane)
                return preferred;

            FluMixShotVfx shots = FindAnyObjectByType<FluMixShotVfx>();
            float plane = shots != null ? shots.ShotPlaneZ : cam.transform.position.z + 95f;
            return plane;
        }

        void EnsureCameraSeesSmokeDepth()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            float plane = ResolveSmokeWorldZ(cam.transform.position);
            float depth = Mathf.Abs(cam.transform.position.z - plane) + 100f;
            if (cam.farClipPlane < depth)
                cam.farClipPlane = depth;
        }

        void StopFlightSmoke()
        {
            if (flightSmoke == null)
                return;
            Destroy(flightSmoke);
            flightSmoke = null;
        }

        void OnDestroy()
        {
            StopFlightSmoke();
        }

        UIEffect Effect()
        {
            if (uiEffect == null)
                uiEffect = GetComponent<UIEffect>();
            return uiEffect;
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
