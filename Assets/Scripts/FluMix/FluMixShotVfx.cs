using System.Collections;
using UnityEngine;

namespace FluMix
{
    public class FluMixShotVfx : MonoBehaviour
    {
        [SerializeField] GameObject beamPrefab;
        [SerializeField] GameObject shieldPrefab;
        [SerializeField] Transform sceneBeams;
        [SerializeField] Transform sceneShield;
        [SerializeField] float authoredBeamLength = 12.8f;

        Transform beamsRoot;
        Transform prismStar;
        Transform rainbowLance;
        Transform electricSword;
        Transform impact;
        Transform bloom;
        Transform shield;
        Coroutine shotRoutine;

        public void Bind(GameObject beams, GameObject winShield)
        {
            beamPrefab = beams;
            shieldPrefab = winShield;
        }

        void Awake()
        {
            ResolveBeams();
            ResolveShield();
        }

        public bool PlayOverUi(HeroId hero, RectTransform space, Vector2 fromLocal, Vector2 toLocal, bool connect, bool impactSound = true)
        {
            ResolveBeams();
            if (space == null || beamsRoot == null)
                return false;

            Vector3 start = OnPlane(space, fromLocal);
            Vector3 end = OnPlane(space, toLocal);
            beamsRoot.gameObject.SetActive(true);
            PlayShot(hero, start, end, connect, impactSound);
            return true;
        }

        public void PlayWinOverUi()
        {
            ResolveShield();
            if (shield == null)
                return;

            PlaceShield();
            shield.gameObject.SetActive(true);
            PlayParticles(shield);
            FluMixSfx.Play("quiz_correct");
        }

        public void PlayShot(HeroId hero, Vector3 worldStart, Vector3 worldEnd, bool connect, bool impactSound = true)
        {
            ResolveBeams();
            if (beamsRoot == null)
                return;

            if (shotRoutine != null)
                StopCoroutine(shotRoutine);

            Vector3 end = connect ? worldEnd : ShortEnd(worldStart, worldEnd);
            Transform weapon = Weapon(hero);
            Aim(weapon, worldStart, end);
            PlayParticles(weapon);
            bool ownTip = weapon != null && weapon.Find("Tip") != null;
            if (!ownTip && bloom != null)
            {
                bloom.position = worldStart;
                bloom.rotation = Quaternion.identity;
                PlayParticles(bloom);
            }

            if (!ownTip && impact != null)
            {
                impact.position = end;
                impact.rotation = Quaternion.identity;
                PlayParticles(impact);
            }

            string cue = hero == HeroId.A ? "beam_blue" : hero == HeroId.G ? "beam_green" : "beam_pink";
            FluMixSfx.Play(cue);
            if (impactSound)
                FluMixSfx.Play(connect ? "impact_correct" : "impact_wrong");
            if (Application.isPlaying && isActiveAndEnabled)
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
            ResolveShield();
            if (shield == null)
                yield break;

            PlaceShield();
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

        Vector3 OnPlane(RectTransform space, Vector2 local)
        {
            Vector3 world = space.TransformPoint(new Vector3(local.x, local.y, 0f));
            if (beamsRoot != null)
                world.z = beamsRoot.position.z;
            return world;
        }

        void PlaceShield()
        {
            Transform canvas = beamsRoot != null ? beamsRoot.parent : null;
            Vector3 center = canvas != null ? canvas.position : shield.position;
            if (beamsRoot != null)
                center.z = beamsRoot.position.z;
            shield.position = center;
            shield.rotation = Quaternion.identity;
        }

        void ResolveBeams()
        {
            if (prismStar != null || rainbowLance != null || electricSword != null)
                return;

            Transform located = sceneBeams;
            if (located == null)
                located = FindNamed("EnergyBeams", true);
            if (located == null && beamPrefab != null)
            {
                GameObject instance = Instantiate(beamPrefab, transform);
                instance.name = "EnergyBeams";
                located = instance.transform;
            }

            if (located == null)
                return;

            beamsRoot = located;
            sceneBeams = located;
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
        }

        void ResolveShield()
        {
            if (shield != null)
                return;

            Transform located = sceneShield;
            if (located == null)
                located = transform.Find("EnergyShield");
            if (located == null)
                located = FindNamed("EnergyShield", true);
            if (located == null && shieldPrefab != null)
            {
                GameObject instance = Instantiate(shieldPrefab, transform);
                instance.name = "EnergyShield";
                located = instance.transform;
            }

            if (located == null)
                return;

            shield = located;
            sceneShield = located;
            shield.gameObject.SetActive(false);
        }

        static Transform FindNamed(string objectName, bool preferCanvas)
        {
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform fallback = null;
            for (int i = 0; i < all.Length; i++)
            {
                Transform candidate = all[i];
                if (candidate.name != objectName)
                    continue;
                if (preferCanvas && candidate.GetComponentInParent<Canvas>(true) != null)
                    return candidate;
                if (fallback == null)
                    fallback = candidate;
            }

            return fallback;
        }

        Transform Weapon(HeroId hero)
        {
            Transform weapon = hero == HeroId.A ? prismStar : hero == HeroId.G ? rainbowLance : electricSword;
            bool ownTip = weapon != null && weapon.Find("Tip") != null;
            SetActive(prismStar, weapon == prismStar);
            SetActive(rainbowLance, weapon == rainbowLance);
            SetActive(electricSword, weapon == electricSword);
            SetActive(impact, !ownTip);
            SetActive(bloom, bloom != null && !ownTip);
            return weapon;
        }

        void Aim(Transform weapon, Vector3 start, Vector3 end)
        {
            if (weapon == null || beamsRoot == null)
                return;

            Vector3 direction = end - start;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.right;

            weapon.SetParent(beamsRoot, true);
            weapon.position = start;
            weapon.rotation = Quaternion.LookRotation(direction.normalized, Vector3.back);

            float unit = Mathf.Max(0.0001f, beamsRoot.lossyScale.x);
            float stretch = direction.magnitude / Mathf.Max(0.2f, authoredBeamLength);
            weapon.localScale = new Vector3(1f / unit, 1f / unit, stretch / unit);
            UseHierarchyScale(weapon);
            HoldTipScale(weapon);
        }

        static void HoldTipScale(Transform weapon)
        {
            float width = Mathf.Abs(weapon.lossyScale.x);
            float length = Mathf.Abs(weapon.lossyScale.z);
            if (length < 0.0001f)
                return;

            float cancel = width / length;
            for (int i = 0; i < weapon.childCount; i++)
            {
                Transform child = weapon.GetChild(i);
                if (!child.name.StartsWith("Tip"))
                    continue;
                child.localScale = new Vector3(1f, 1f, cancel);
            }
        }

        static void UseHierarchyScale(Transform root)
        {
            if (root == null)
                return;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
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

        static float PlayParticles(Transform root)
        {
            if (root == null)
                return 0f;

            root.gameObject.SetActive(true);
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            float seconds = 0.6f;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
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
    }
}
