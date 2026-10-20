using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluMix
{
    public class FluMixShotVfx : MonoBehaviour
    {
        [SerializeField] GameObject beamPrefab;
        [SerializeField] GameObject shieldPrefab;
        [SerializeField] float authoredBeamLength = 12.8f;
        [SerializeField] int vfxSortingOrder = 20;
        [SerializeField] int hudSortingOrder = 40;

        Transform beamsRoot;
        Transform prismStar;
        Transform rainbowLance;
        Transform electricSword;
        Transform impact;
        Transform bloom;
        Transform shield;
        bool canvasReady;

        public void Bind(GameObject beams, GameObject winShield)
        {
            beamPrefab = beams;
            shieldPrefab = winShield;
        }

        void Awake()
        {
            EnsureCameraCanvas();
            SpawnBeams();
            SpawnShield();
        }

        Coroutine shotRoutine;

        public void PlayShot(HeroId hero, Vector3 worldStart, Vector3 worldEnd, bool connect)
        {
            EnsureCameraCanvas();
            if (beamsRoot == null)
                return;

            if (shotRoutine != null)
                StopCoroutine(shotRoutine);

            Vector3 end = connect ? worldEnd : ShortEnd(worldStart, worldEnd);
            Transform weapon = Weapon(hero);
            Color tint = TintFor(hero);
            Aim(weapon, worldStart, end);
            TintParticles(weapon, tint);
            PlayParticles(weapon);
            if (bloom != null)
            {
                bloom.position = worldStart;
                TintParticles(bloom, tint);
                PlayParticles(bloom);
            }

            if (impact != null)
            {
                impact.position = end;
                impact.rotation = Quaternion.identity;
                TintParticles(impact, tint);
                PlayParticles(impact);
            }

            string cue = hero == HeroId.A ? "beam_blue" : hero == HeroId.G ? "beam_green" : "beam_pink";
            FluMixSfx.Play(cue);
            FluMixSfx.Play(connect ? "impact_correct" : "impact_wrong");
            shotRoutine = StartCoroutine(StopShot(weapon, 0.55f));
        }

        IEnumerator StopShot(Transform weapon, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            StopParticles(weapon);
            StopParticles(impact);
            StopParticles(bloom);
            SetActive(weapon, false);
            SetActive(impact, false);
            SetActive(bloom, false);
            shotRoutine = null;
        }

        public IEnumerator PlayWinRoutine()
        {
            if (shield == null)
                yield break;

            ArenaStage stage = FindAnyObjectByType<ArenaStage>();
            if (stage != null && stage.Model != null)
                shield.SetParent(stage.Model, false);
            shield.localPosition = new Vector3(-2.51f, -0.52f, -11.06f);
            shield.localRotation = Quaternion.identity;
            shield.localScale = Vector3.one;
            shield.gameObject.SetActive(true);
            float seconds = PlayParticles(shield);
            FluMixSfx.Play("quiz_correct");
            yield return new WaitForSeconds(seconds);
            HideWin();
        }

        public void HideWin()
        {
            if (shield == null)
                return;
            StopParticles(shield);
            shield.gameObject.SetActive(false);
        }

        void SpawnBeams()
        {
            Transform existing = transform.Find("EnergyBeams");
            GameObject instance = existing != null ? existing.gameObject : beamPrefab != null ? Instantiate(beamPrefab, transform) : null;
            if (instance == null)
                return;
            instance.name = "EnergyBeams";
            instance.SetActive(true);
            beamsRoot = instance.transform;
            prismStar = beamsRoot.Find("PrismStar");
            rainbowLance = beamsRoot.Find("RainbowLance");
            electricSword = beamsRoot.Find("ElectricSword");
            impact = beamsRoot.Find("Impact");
            bloom = beamsRoot.Find("EnergyBloom");
            SetActive(prismStar, false);
            SetActive(rainbowLance, false);
            SetActive(electricSword, false);
            SetActive(impact, false);
            SetActive(bloom, false);
            RaiseSorting(instance);
        }

        void SpawnShield()
        {
            Transform existing = transform.Find("EnergyShield");
            GameObject instance = existing != null ? existing.gameObject : shieldPrefab != null ? Instantiate(shieldPrefab, transform) : null;
            if (instance == null)
                return;
            instance.name = "EnergyShield";
            shield = instance.transform;
            instance.SetActive(false);
            RaiseSorting(instance);
        }

        Transform Weapon(HeroId hero)
        {
            Transform weapon = hero == HeroId.A ? prismStar : hero == HeroId.G ? rainbowLance : electricSword;
            SetActive(prismStar, weapon == prismStar);
            SetActive(rainbowLance, weapon == rainbowLance);
            SetActive(electricSword, weapon == electricSword);
            SetActive(impact, true);
            SetActive(bloom, bloom != null);
            return weapon;
        }

        void Aim(Transform weapon, Vector3 start, Vector3 end)
        {
            if (weapon == null)
                return;
            Vector3 direction = end - start;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.right;
            weapon.SetParent(beamsRoot, true);
            weapon.position = start;
            weapon.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float length = Mathf.Max(0.2f, authoredBeamLength);
            float stretch = direction.magnitude / length;
            weapon.localScale = new Vector3(1f, 1f, stretch);
        }

        readonly Dictionary<int, ParticleSystem.MinMaxGradient> originalColors = new Dictionary<int, ParticleSystem.MinMaxGradient>();

        static Color TintFor(HeroId hero)
        {
            if (hero == HeroId.A)
                return new Color(0.2f, 1f, 0.35f, 1f);
            if (hero == HeroId.G)
                return new Color(1f, 0.52f, 0.05f, 1f);
            return new Color(1f, 0.16f, 0.28f, 1f);
        }

        void TintParticles(Transform root, Color tint)
        {
            if (root == null)
                return;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                int id = system.GetInstanceID();
                if (!originalColors.TryGetValue(id, out ParticleSystem.MinMaxGradient source))
                {
                    source = system.main.startColor;
                    originalColors[id] = source;
                }

                ParticleSystem.MainModule main = system.main;
                main.startColor = Multiply(source, tint);
            }
        }

        static ParticleSystem.MinMaxGradient Multiply(ParticleSystem.MinMaxGradient source, Color tint)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(source.colorMin * tint, source.colorMax * tint);
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(MultiplyGradient(source.gradient, tint));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(MultiplyGradient(source.gradientMin, tint), MultiplyGradient(source.gradientMax, tint));
                default:
                    return new ParticleSystem.MinMaxGradient(source.color * tint);
            }
        }

        static Gradient MultiplyGradient(Gradient source, Color tint)
        {
            Gradient copy = new Gradient();
            GradientColorKey[] colors = source.colorKeys;
            for (int i = 0; i < colors.Length; i++)
                colors[i].color *= tint;
            copy.SetKeys(colors, source.alphaKeys);
            return copy;
        }

        static Vector3 ShortEnd(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance < 0.05f)
                return end;
            float gap = distance * 0.18f;
            return start + direction.normalized * Mathf.Max(0.1f, distance - gap);
        }

        void EnsureCameraCanvas()
        {
            if (canvasReady)
                return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i].name == "Canvas")
                    {
                        canvas = canvases[i];
                        break;
                    }
                }
            }

            Camera cam = Camera.main;
            if (canvas != null && cam != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = PlaneDistance();
            }

            RaiseHud(canvas);
            canvasReady = canvas != null;
        }

        float PlaneDistance()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return 10f;
            return Mathf.Clamp(cam.nearClipPlane + 8f, 2f, cam.farClipPlane - 1f);
        }

        void RaiseHud(Canvas canvas)
        {
            if (canvas == null)
                return;
            string[] names = { "Status Panel", "Boss Warning Panel", "Question Panel", "WinBoss", "WinButton", "Countdown" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform target = FindDeep(canvas.transform, names[i]);
                if (target == null)
                    continue;
                Canvas layer = target.GetComponent<Canvas>();
                if (layer == null)
                    layer = target.gameObject.AddComponent<Canvas>();
                layer.overrideSorting = true;
                layer.sortingOrder = hudSortingOrder;
                if (target.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                    target.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }

        void RaiseSorting(GameObject root)
        {
            ParticleSystemRenderer[] renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sortingOrder = vfxSortingOrder;
        }

        static float PlayParticles(Transform root)
        {
            if (root == null)
                return 0f;
            root.gameObject.SetActive(true);
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            float seconds = 0.6f;
            for (int i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                main.loop = false;
                systems[i].gameObject.SetActive(true);
                systems[i].Clear(true);
                systems[i].Play(true);
                float life = main.duration;
                if (life > seconds)
                    seconds = life;
            }

            return Mathf.Min(seconds, 4.2f);
        }

        static void StopParticles(Transform root)
        {
            if (root == null)
                return;
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        static void SetActive(Transform target, bool active)
        {
            if (target != null && target.gameObject.activeSelf != active)
                target.gameObject.SetActive(active);
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
