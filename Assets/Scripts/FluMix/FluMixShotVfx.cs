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
        [SerializeField] GameObject lineLaserPrefab;
        [SerializeField] GameObject splashPrefabA;
        [SerializeField] GameObject splashPrefabG;
        [SerializeField] GameObject splashPrefabT;
        [SerializeField] GameObject slashOncePrefab;

        class LineShot
        {
            public GameObject root;
            public LineRenderer[] lines;
            public GameObject hit;
        }

        Transform beamsRoot;
        Transform shield;
        Coroutine shotRoutine;
        LineShot shotA;
        LineShot shotG;
        LineShot shotT;

        static readonly Color BeamCore = new Color(0.82f, 1f, 1f, 1f);
        static readonly Color BeamEdge = new Color(0.25f, 0.86f, 1f, 1f);
        static readonly Color SplashHot = new Color(1.45f, 2.05f, 2.55f, 1f);
        static readonly Color SplashCool = new Color(0.45f, 1.25f, 2.35f, 1f);
        static readonly Quaternion SplashLocalRotation = Quaternion.Euler(0f, 180f, 105f);

        public void Bind(GameObject beams, GameObject winShield)
        {
            beamPrefab = beams;
            shieldPrefab = winShield;
        }

        void Awake()
        {
            ResolveBeams();
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
                DestroyObject(existing.gameObject);

            ResolveBeams();
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
                if (beamsRoot != null)
                {
                    start.z = beamsRoot.position.z;
                    end.z = beamsRoot.position.z;
                }

                PlaceShot(shot, heroes[i], start, end, true);
                if (shot.hit != null)
                    SimulateHit(shot.hit.transform);
            }
        }

        public bool PlayOverUi(HeroId hero, RectTransform space, Vector2 fromLocal, Vector2 toLocal, bool connect, bool impactSound = true)
        {
            ResolveBeams();
            if (space == null)
                return false;

            SilenceOldBeams();
            Vector3 start = space.TransformPoint(new Vector3(fromLocal.x, fromLocal.y, 0f));
            Vector3 end = space.TransformPoint(new Vector3(toLocal.x, toLocal.y, 0f));
            if (beamsRoot != null)
            {
                start.z = beamsRoot.position.z;
                end.z = beamsRoot.position.z;
            }

            PlayShot(hero, start, end, connect, impactSound);
            return true;
        }

        void SilenceOldBeams()
        {
            if (beamsRoot == null)
                return;

            for (int i = 0; i < beamsRoot.childCount; i++)
            {
                Transform child = beamsRoot.GetChild(i);
                if (child.name == "PrismStar" || child.name == "RainbowLance" || child.name == "ElectricSword" || child.name == "Impact" || child.name == "EnergyBloom")
                    child.gameObject.SetActive(false);
            }
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
            if (hero == HeroId.T)
            {
                LineShot shot = BuildShot(lineLaserPrefab, null, name, null);
                if (shot != null && slashOncePrefab != null)
                {
                    shot.hit = Instantiate(slashOncePrefab, shot.root.transform);
                    shot.hit.name = "SlashOnce";
                    PaintWhite(shot.hit);
                    shot.hit.SetActive(false);
                }

                return shot;
            }

            GameObject splash = hero == HeroId.A ? splashPrefabA : splashPrefabG;
            return BuildShot(lineLaserPrefab, splash, name, "Hit");
        }

        LineShot BuildShot(GameObject laserPrefab, GameObject splashPrefab, string name, string splashChild)
        {
            if (laserPrefab == null)
                return null;

            var root = Instantiate(laserPrefab, transform);
            root.name = name;
            var hovl = root.GetComponent<Hovl_Laser>();
            if (hovl != null)
                hovl.enabled = false;
            var line = root.GetComponent<LineRenderer>();
            SilenceLaserParticles(root, hovl);
            GameObject hit = TakeSplash(splashPrefab, splashChild);
            if (hit != null)
                hit.transform.SetParent(root.transform, true);
            LineRenderer[] lines = StackLines(root, line);
            for (int i = 0; i < lines.Length; i++)
                PaintLine(lines[i], i);
            if (hit != null)
                PaintParticles(hit);
            root.SetActive(false);
            return new LineShot { root = root, lines = lines, hit = hit };
        }

        static void SilenceLaserParticles(GameObject root, Hovl_Laser hovl)
        {
            if (hovl != null && hovl.HitEffect != null)
                hovl.HitEffect.SetActive(false);
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
                systems[i].gameObject.SetActive(false);
        }

        static LineRenderer[] StackLines(GameObject root, LineRenderer source)
        {
            var lines = new LineRenderer[3];
            lines[0] = source;
            lines[1] = CopyLine(root.transform, source, "LineGlow");
            lines[2] = CopyLine(root.transform, source, "LineCore");
            return lines;
        }

        static LineRenderer CopyLine(Transform parent, LineRenderer source, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = source.sharedMaterial;
            line.textureMode = source.textureMode;
            line.alignment = source.alignment;
            line.numCapVertices = 5;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
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
                PaintLine(line, i);
            }
        }

        static void PaintLine(LineRenderer line, int layer)
        {
            if (line == null)
                return;

            Color color = layer == 0 ? new Color(0.35f, 0.78f, 1f, 0.55f)
                : layer == 2 ? new Color(0.95f, 1f, 1f, 1f)
                : BeamEdge;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.Lerp(color, BeamCore, 0.35f), 0f),
                    new GradientColorKey(color, 0.7f),
                    new GradientColorKey(Color.Lerp(color, Color.white, 0.25f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(layer == 0 ? 0.45f : 1f, 0f),
                    new GradientAlphaKey(layer == 0 ? 0.35f : 1f, 0.8f),
                    new GradientAlphaKey(0.7f, 1f)
                });
            line.colorGradient = gradient;
        }

        static void PaintWhite(GameObject root)
        {
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                string particleName = systems[i].name.ToLowerInvariant();
                if (particleName.Contains("smoke") || particleName.Contains("shadow"))
                {
                    systems[i].gameObject.SetActive(false);
                    continue;
                }

                ParticleSystem.MainModule main = systems[i].main;
                main.startColor = Color.white;
                main.loop = false;
                ParticleSystem.ColorOverLifetimeModule life = systems[i].colorOverLifetime;
                if (life.enabled)
                {
                    var gradient = new Gradient();
                    gradient.SetKeys(
                        new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                        new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                    life.color = new ParticleSystem.MinMaxGradient(gradient);
                }

                ParticleSystemRenderer renderer = systems[i].GetComponent<ParticleSystemRenderer>();
                if (renderer == null)
                    continue;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", Color.white);
                renderer.SetPropertyBlock(block);
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

        static GameObject TakeSplash(GameObject splashPrefab, string childName)
        {
            if (splashPrefab == null)
                return null;

            var copy = Instantiate(splashPrefab);
            var hovl = copy.GetComponent<Hovl_Laser>();
            if (hovl != null)
                hovl.enabled = false;
            Transform found = FindDeep(copy.transform, childName);
            if (found == null && hovl != null && hovl.HitEffect != null)
                found = hovl.HitEffect.transform;
            GameObject splash = null;
            if (found != null)
            {
                found.SetParent(null, false);
                splash = found.gameObject;
            }

            DestroyObject(copy);
            return splash;
        }

        static void DestroyObject(Object target)
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

        static void PaintParticles(GameObject root)
        {
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
                main.startColor = new ParticleSystem.MinMaxGradient(SplashHot, SplashCool);
            }
        }

        static void HideLine(LineShot shot)
        {
            if (shot == null || shot.root == null)
                return;
            StopParticles(shot.root.transform);
            shot.root.SetActive(false);
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
            if (beamsRoot != null)
                return;

            Transform located = sceneBeams;
            if (located == null)
                located = FindNamed("EnergyBeams", true);
            if (located == null)
                return;

            beamsRoot = located;
            sceneBeams = located;
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

        static void SetActive(Transform target, bool active)
        {
            if (target != null && target.gameObject.activeSelf != active)
                target.gameObject.SetActive(active);
        }
    }
}
