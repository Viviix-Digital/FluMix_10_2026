using System.Collections;
using UnityEngine;

namespace FluMix
{
    public class FluMixShotVfx : MonoBehaviour
    {
        [SerializeField] GameObject shieldPrefab;
        [SerializeField] Transform sceneShield;
        [SerializeField] float shotPlaneZ = 111.73f;
        [SerializeField] GameObject lineShotAPrefab;
        [SerializeField] GameObject lineShotGPrefab;
        [SerializeField] GameObject lineShotTPrefab;

        class LineShot
        {
            public GameObject root;
            public LineRenderer[] lines;
            public GameObject hit;
        }

        Transform shield;
        Coroutine shotRoutine;
        LineShot shotA;
        LineShot shotG;
        LineShot shotT;

        static readonly Quaternion SplashLocalRotation = Quaternion.Euler(0f, 180f, 105f);

        void Awake()
        {
            ResolveShield();
            Transform preview = transform.Find("Shot Preview");
            if (preview != null)
                preview.gameObject.SetActive(false);
        }

        [ContextMenu("Xem truoc tia ban")]
        public void ShowPreview()
        {
            Transform existing = transform.Find("Shot Preview");
            if (existing != null)
                DestroyOwned(existing.gameObject);

            var preview = new GameObject("Shot Preview");
            preview.transform.SetParent(transform, false);
            RectTransform space = LargestCanvas();
            var heroes = new[] { HeroId.A, HeroId.G, HeroId.T };
            var from = new[] { new Vector2(1100f, 220f), new Vector2(1100f, 0f), new Vector2(1100f, -220f) };
            var to = new[] { new Vector2(2100f, 260f), new Vector2(2160f, 0f), new Vector2(2040f, -260f) };
            for (int i = 0; i < heroes.Length; i++)
            {
                LineShot shot = MakeShot(heroes[i], "Preview" + heroes[i]);
                if (shot == null)
                    continue;
                shot.root.transform.SetParent(preview.transform, true);
                Vector3 start = space != null
                    ? space.TransformPoint(new Vector3(from[i].x, from[i].y, 0f))
                    : preview.transform.position + Vector3.down * i * 2f;
                Vector3 end = space != null
                    ? space.TransformPoint(new Vector3(to[i].x, to[i].y, 0f))
                    : start + Vector3.right * 6f;
                start.z = shotPlaneZ;
                end.z = shotPlaneZ;

                PlaceShot(shot, heroes[i], start, end, true);
                if (shot.hit != null)
                    SimulateHit(shot.hit.transform);
            }
        }

        public bool PlayOverUi(HeroId hero, RectTransform space, Vector2 fromLocal, Vector2 toLocal, bool connect, bool impactSound = true)
        {
            if (space == null)
                return false;

            Vector3 start = space.TransformPoint(new Vector3(fromLocal.x, fromLocal.y, 0f));
            Vector3 end = space.TransformPoint(new Vector3(toLocal.x, toLocal.y, 0f));
            start.z = shotPlaneZ;
            end.z = shotPlaneZ;

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
            EnsureLineShots();
            LineShot shot = hero == HeroId.A ? shotA : hero == HeroId.G ? shotG : shotT;
            if (shot == null || shot.lines == null || shot.lines.Length == 0)
                return;

            if (shotRoutine != null)
                StopCoroutine(shotRoutine);

            HideLine(shotA);
            HideLine(shotG);
            HideLine(shotT);
            PlaceShot(shot, hero, worldStart, worldEnd, connect);

            if (shot.hit != null)
                PlayParticles(shot.hit.transform);
            string cue = hero == HeroId.A ? "beam_blue" : hero == HeroId.G ? "beam_green" : "beam_pink";
            FluMixSfx.Play(cue);
            if (impactSound)
                FluMixSfx.Play(connect ? "impact_correct" : "impact_wrong");
            if (Application.isPlaying && isActiveAndEnabled)
                shotRoutine = StartCoroutine(StopLine(shot, 0.55f));
        }

        IEnumerator StopLine(LineShot shot, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            HideLine(shot);
            shotRoutine = null;
        }

        void PlaceShot(LineShot shot, HeroId hero, Vector3 worldStart, Vector3 worldEnd, bool connect)
        {
            Vector3 end = connect ? worldEnd : ShortEnd(worldStart, worldEnd);
            Vector3 direction = end - worldStart;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.right;

            shot.root.SetActive(true);
            shot.root.transform.position = worldStart;
            shot.root.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float distance = direction.magnitude;
            PlaceLines(shot.lines, worldStart, end, distance);
            if (shot.hit == null)
                return;

            shot.hit.SetActive(true);
            shot.hit.transform.position = end;
            shot.hit.transform.localRotation = SplashLocalRotation;
            if (hero == HeroId.T)
                shot.hit.transform.localScale = Vector3.one * Mathf.Clamp(distance * 0.22f, 1.6f, 4.2f);
        }

        void EnsureLineShots()
        {
            if (shotA != null)
                return;
            shotA = MakeShot(HeroId.A, "LineShotA");
            shotG = MakeShot(HeroId.G, "LineShotG");
            shotT = MakeShot(HeroId.T, "LineShotT");
        }

        LineShot MakeShot(HeroId hero, string name)
        {
            GameObject configured = hero == HeroId.A ? lineShotAPrefab
                : hero == HeroId.G ? lineShotGPrefab
                : lineShotTPrefab;
            return BuildConfiguredShot(configured, name);
        }

        LineShot BuildConfiguredShot(GameObject prefab, string name)
        {
            if (prefab == null)
                return null;

            var root = Instantiate(prefab, transform);
            root.name = name;
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            var hovl = root.GetComponent<Hovl_Laser>();
            if (hovl != null)
                hovl.enabled = false;

            GameObject hit = FindSplash(root.transform);
            SilenceBodyParticles(root, hit);
            if (hit != null)
                hit.SetActive(false);

            LineRenderer[] lines = CollectLines(root);
            root.SetActive(false);
            return new LineShot { root = root, lines = lines, hit = hit };
        }

        static LineRenderer[] CollectLines(GameObject root)
        {
            var ordered = new System.Collections.Generic.List<LineRenderer>(3);
            LineRenderer rootLine = root.GetComponent<LineRenderer>();
            if (rootLine != null)
                ordered.Add(rootLine);
            Transform glow = root.transform.Find("LineGlow");
            Transform core = root.transform.Find("LineCore");
            if (glow != null)
            {
                LineRenderer line = glow.GetComponent<LineRenderer>();
                if (line != null)
                    ordered.Add(line);
            }

            if (core != null)
            {
                LineRenderer line = core.GetComponent<LineRenderer>();
                if (line != null)
                    ordered.Add(line);
            }

            if (ordered.Count > 0)
                return ordered.ToArray();
            return root.GetComponentsInChildren<LineRenderer>(true);
        }

        static GameObject FindSplash(Transform root)
        {
            Transform slash = FindDeep(root, "SlashOnce");
            if (slash != null)
                return slash.gameObject;

            Transform best = null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name != "Hit")
                    continue;
                if (best == null || all[i].gameObject.activeSelf)
                    best = all[i];
            }

            return best != null ? best.gameObject : null;
        }

        static void SilenceBodyParticles(GameObject root, GameObject splash)
        {
            var hovl = root.GetComponent<Hovl_Laser>();
            if (hovl != null && hovl.HitEffect != null && (splash == null || hovl.HitEffect != splash))
                hovl.HitEffect.SetActive(false);

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name != "Hit")
                    continue;
                if (splash != null && all[i] == splash.transform)
                    continue;
                all[i].gameObject.SetActive(false);
            }

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                Transform particle = systems[i].transform;
                if (splash != null && (particle == splash.transform || particle.IsChildOf(splash.transform)))
                    continue;
                systems[i].gameObject.SetActive(false);
            }
        }

        static void PlaceLines(LineRenderer[] lines, Vector3 start, Vector3 end, float distance)
        {
            float[] widths = { distance * 0.11f, distance * 0.055f, distance * 0.022f };
            for (int i = 0; i < lines.Length; i++)
            {
                LineRenderer line = lines[i];
                if (line == null)
                    continue;
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.SetPosition(0, start);
                line.SetPosition(1, end);
                line.widthMultiplier = Mathf.Clamp(i < widths.Length ? widths[i] : distance * 0.04f, 0.05f, 5f);
            }
        }

        static void SimulateHit(Transform root)
        {
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                if (!systems[i].gameObject.activeInHierarchy)
                    continue;
                systems[i].Simulate(0.12f, true, true, true);
            }
        }

        static void DestroyOwned(Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

        static Transform FindDeep(Transform root, string childName)
        {
            if (root.name == childName)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        static void HideLine(LineShot shot)
        {
            if (shot == null || shot.root == null)
                return;
            StopParticles(shot.root.transform);
            shot.root.SetActive(false);
        }

        public void HideWin()
        {
            if (shield == null)
                return;
            StopParticles(shield);
            shield.gameObject.SetActive(false);
        }

        void PlaceShield()
        {
            RectTransform canvas = LargestCanvas();
            Vector3 center = canvas != null ? canvas.position : shield.position;
            center.z = shotPlaneZ;
            shield.position = center;
            shield.rotation = Quaternion.identity;
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

        static RectTransform LargestCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            RectTransform best = null;
            float bestSize = 0f;
            for (int i = 0; i < canvases.Length; i++)
            {
                RectTransform rect = canvases[i].GetComponent<RectTransform>();
                if (rect == null)
                    continue;
                float size = Mathf.Abs(rect.rect.width * rect.rect.height);
                if (size <= bestSize)
                    continue;
                bestSize = size;
                best = rect;
            }

            return best;
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
    }
}
