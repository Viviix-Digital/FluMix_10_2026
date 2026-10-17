using UnityEngine;

namespace FluMix
{
    /// <summary>
    /// Sustained energy shot built only from particle billboards.
    /// A smooth white core, cyan sheath, electric filaments and a whip spiral at the impact.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(200)]
    public class EnergyBeamFx : MonoBehaviour
    {
        [SerializeField] Vector3 start = new Vector3(-3.85f, 2.28f, 0f);
        [SerializeField] Vector3 end = new Vector3(3.05f, -1.92f, 0f);
        [SerializeField] float spiralTurns = 0.82f;
        [SerializeField] float spiralRadius = 0.78f;
        [SerializeField] float cycleSeconds = 3.4f;
        [SerializeField] float extendSeconds = 0.22f;

        [SerializeField] ParticleSystem aura;
        [SerializeField] ParticleSystem glow;
        [SerializeField] ParticleSystem arcs;
        [SerializeField] ParticleSystem filaments;
        [SerializeField] ParticleSystem core;
        [SerializeField] ParticleSystem sparks;
        [SerializeField] ParticleSystem muzzle;

        [SerializeField] Material auraMat;
        [SerializeField] Material glowMat;
        [SerializeField] Material arcMat;
        [SerializeField] Material filamentMat;
        [SerializeField] Material coreMat;
        [SerializeField] Material sparkMat;
        [SerializeField] Material muzzleMat;

        ParticleSystem.Particle[] buffer;
        bool missingShader;

        public Vector3 StartPoint => start;
        public Vector3 EndPoint => end;

        void OnEnable()
        {
            if (core == null)
                BuildHierarchy();
        }

        void LateUpdate()
        {
            if (core == null)
                return;

            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            RebuildNow(time);
        }

        public void RebuildNow(float time)
        {
            if (core == null)
                BuildHierarchy();
            if (core == null)
                return;

            if (buffer == null || buffer.Length < 512)
                buffer = new ParticleSystem.Particle[512];

            float cycle = Mathf.Max(0.4f, cycleSeconds);
            float clock = Mathf.Repeat(time, cycle);
            float extend = Mathf.Clamp01(clock / Mathf.Max(0.05f, extendSeconds));
            float fade = 1f;
            float fadeStart = cycle * 0.9f;
            if (clock > fadeStart)
                fade = Mathf.InverseLerp(cycle, fadeStart, clock);

            RefreshMaterials();
            ConfigureRenderer(aura, false);
            ConfigureRenderer(glow, false);
            ConfigureRenderer(arcs, true);
            ConfigureRenderer(filaments, true);
            ConfigureRenderer(core, true);
            ConfigureRenderer(sparks, false);
            ConfigureRenderer(muzzle, false);
            float spin = time * 1.7f;
            FillRibbon(aura, extend, fade, time, spin, 0.016f, 1.55f, 1f, true, false);
            FillRibbon(glow, extend, fade, time, spin, 0.011f, 0.88f, 1f, true, false);
            FillRibbon(core, extend, fade, time, spin, 0.007f, 0.09f, 1f, false, true);
            FillFilaments(extend, fade, time, spin);
            FillArcs(extend, fade, time, spin);
            FillSparks(extend, fade, time, spin);
            FillMuzzle(extend, fade, time);
        }

        public void BuildHierarchy()
        {
            Shader shader = Shader.Find("FluMix/EnergyParticle");
            if (shader == null)
            {
                if (!missingShader)
                {
                    missingShader = true;
                    Debug.LogError("FluMix/EnergyParticle shader is missing.");
                }

                return;
            }

            RefreshMaterials();

            aura = PrepareSystem("Fx Aura", aura, auraMat);
            glow = PrepareSystem("Fx Glow", glow, glowMat);
            arcs = PrepareSystem("Fx Arcs", arcs, arcMat);
            filaments = PrepareSystem("Fx Filaments", filaments, filamentMat);
            core = PrepareSystem("Fx Core", core, coreMat);
            sparks = PrepareSystem("Fx Sparks", sparks, sparkMat);
            muzzle = PrepareSystem("Fx Muzzle", muzzle, muzzleMat);
        }

        public void SaveGeneratedAssets(string folder)
        {
#if UNITY_EDITOR
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/VFX"))
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "VFX");
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/VFX/EnergyBeam"))
                    UnityEditor.AssetDatabase.CreateFolder("Assets/VFX", "EnergyBeam");
            }

            auraMat = SaveMat(auraMat, folder + "/EnergyAura.mat");
            glowMat = SaveMat(glowMat, folder + "/EnergyGlow.mat");
            arcMat = SaveMat(arcMat, folder + "/EnergyArcs.mat");
            filamentMat = SaveMat(filamentMat, folder + "/EnergyFilament.mat");
            coreMat = SaveMat(coreMat, folder + "/EnergyCore.mat");
            sparkMat = SaveMat(sparkMat, folder + "/EnergySparks.mat");
            muzzleMat = SaveMat(muzzleMat, folder + "/EnergyMuzzle.mat");
            Assign(aura, auraMat);
            Assign(glow, glowMat);
            Assign(arcs, arcMat);
            Assign(filaments, filamentMat);
            Assign(core, coreMat);
            Assign(sparks, sparkMat);
            Assign(muzzle, muzzleMat);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        void RefreshMaterials()
        {
            Shader shader = Shader.Find("FluMix/EnergyParticle");
            if (shader == null)
                return;

            auraMat = PrepareMat(auraMat, shader, "EnergyAura", 0f, new Color(0.06f, 0.36f, 1f, 1f), 0.48f, 1.05f, 0f, 3000);
            glowMat = PrepareMat(glowMat, shader, "EnergyGlow", 0f, new Color(0.18f, 0.72f, 1f, 1f), 1.2f, 1.65f, 0f, 3001);
            arcMat = PrepareMat(arcMat, shader, "EnergyArcs", 1f, new Color(0.75f, 0.96f, 1f, 1f), 2.4f, 3.2f, 1f, 3002);
            filamentMat = PrepareMat(filamentMat, shader, "EnergyFilament", 1f, new Color(0.9f, 0.98f, 1f, 1f), 2.4f, 3.4f, 1f, 3003);
            coreMat = PrepareMat(coreMat, shader, "EnergyCore", 1f, new Color(0.96f, 0.99f, 1f, 1f), 5.6f, 3.6f, 0f, 3004);
            sparkMat = PrepareMat(sparkMat, shader, "EnergySparks", 0f, new Color(0.95f, 0.98f, 1f, 1f), 6.5f, 4.4f, 0f, 3005);
            muzzleMat = PrepareMat(muzzleMat, shader, "EnergyMuzzle", 0f, new Color(0.4f, 0.85f, 1f, 1f), 1.55f, 1.45f, 0f, 3006);
        }

        void FillRibbon(ParticleSystem system, float extend, float fade, float time, float spin, float step, float width, float alpha, bool discs, bool pulse)
        {
            int count = 0;
            float t = 0f;
            Vector3 previous = Sample(0f, time, spin);
            while (t < extend && count < buffer.Length - 1)
            {
                float nextT = Mathf.Min(extend, t + step);
                Vector3 point = Sample(nextT, time, spin);
                float widthScale = WidthScale(nextT);
                float bright = Brightness(nextT, time, fade) * alpha;
                if (pulse)
                {
                    float crack = 0.7f + 0.3f * Noise(nextT * 14f + time * 6f);
                    if (nextT > 0.72f)
                        crack = Mathf.Max(crack, 0.94f);
                    bright *= crack;
                }
                if (discs)
                    PushDisc(ref count, point, width * widthScale, bright);
                else
                    PushStreak(ref count, previous, point, width * widthScale, bright);
                previous = point;
                t = nextT;
            }

            Apply(system, count);
        }

        void FillFilaments(float extend, float fade, float time, float spin)
        {
            int count = 0;
            const int lanes = 2;
            for (int lane = 0; lane < lanes; lane++)
            {
                float seed = 10f + lane * 4.3f;
                float sign = lane == 0 ? 1f : -1f;
                float t = 0f;
                Vector3 previous = FilamentPoint(0f, time, spin, seed, sign, 0f);
                while (t < extend && count < buffer.Length - 1)
                {
                    float nextT = Mathf.Min(extend, t + 0.012f);
                    Vector3 point = FilamentPoint(nextT, time, spin, seed, sign, nextT);
                    float bright = Brightness(nextT, time, fade) * (0.45f + 0.55f * Noise(nextT * 9f + seed));
                    PushStreak(ref count, previous, point, 0.055f * WidthScale(nextT), bright);
                    previous = point;
                    t = nextT;
                }
            }

            Apply(filaments, count);
        }

        void FillArcs(float extend, float fade, float time, float spin)
        {
            int count = 0;
            const int arcCount = 5;
            for (int i = 0; i < arcCount; i++)
            {
                float seed = 20f + i * 5.1f;
                float t0 = Mathf.Lerp(0.08f, 0.62f, Repeat01(Hash(seed) + time * 0.11f));
                if (t0 > extend)
                    continue;

                float length = Mathf.Lerp(0.16f, 0.32f, Hash(seed + 1.2f));
                float t1 = Mathf.Min(extend, t0 + length);
                float sign = Hash(seed + 2.2f) > 0.5f ? 1f : -1f;
                float amp = Mathf.Lerp(0.22f, 0.58f, Hash(seed + 4f));
                int steps = 8;
                Vector3 previous = Sample(t0, time, spin);
                for (int step = 1; step <= steps && count < buffer.Length - 2; step++)
                {
                    float u = step / (float)steps;
                    float t = Mathf.Lerp(t0, t1, u);
                    Vector3 center = Sample(t, time, spin);
                    Vector3 tangent = Tangent(t, time, spin);
                    Vector3 side = new Vector3(-tangent.y, tangent.x, 0f);
                    float env = Mathf.Sin(u * Mathf.PI);
                    float jag = (Noise(t * 28f + seed + time * 7.5f) - 0.5f) * 2f;
                    Vector3 point = center + side * (sign * amp * env + jag * amp * 0.45f);
                    float bright = Brightness(t, time, fade) * Mathf.Lerp(0.7f, 1f, env);
                    PushStreak(ref count, previous, point, Mathf.Lerp(0.022f, 0.05f, env), bright);

                    if (step == 3 && amp > 0.3f && count < buffer.Length - 1)
                    {
                        Vector3 fork = point + side * sign * amp * 0.7f + tangent * 0.08f;
                        PushStreak(ref count, point, fork, 0.016f, bright * 0.85f);
                    }

                    previous = point;
                }
            }

            Apply(arcs, count);
        }

        void FillSparks(float extend, float fade, float time, float spin)
        {
            int count = 0;
            const int sparkCount = 28;
            for (int i = 0; i < sparkCount && count < buffer.Length; i++)
            {
                float seed = 40f + i * 1.7f;
                float t = Mathf.Repeat(Hash(seed) + time * Mathf.Lerp(0.25f, 0.7f, Hash(seed + 0.4f)), 1f);
                if (t > extend)
                    continue;

                Vector3 center = Sample(t, time, spin);
                Vector3 tangent = Tangent(t, time, spin);
                Vector3 side = new Vector3(-tangent.y, tangent.x, 0f);
                float offset = (Hash(seed + time * 0.3f) - 0.5f) * 0.55f * WidthScale(t);
                Vector3 point = center + side * offset;
                float size = Mathf.Lerp(0.035f, 0.11f, Hash(seed + 2f));
                float bright = Brightness(t, time, fade);
                PushDisc(ref count, point, size, bright);
            }

            if (extend > 0.02f && count < buffer.Length)
            {
                Vector3 head = Sample(extend, time, spin);
                PushDisc(ref count, head, 0.16f, fade);
                PushDisc(ref count, head, 0.07f, fade);
            }

            if (extend > 0.95f && count < buffer.Length - 1)
            {
                PushDisc(ref count, end, 0.22f, fade);
                PushDisc(ref count, end, 0.08f, fade);
            }

            Apply(sparks, count);
        }

        void FillMuzzle(float extend, float fade, float time)
        {
            int count = 0;
            float pulse = 0.85f + 0.15f * Mathf.Sin(time * 28f);
            PushDisc(ref count, start, 1.35f * pulse, fade * Mathf.Clamp01(extend * 4f));
            PushDisc(ref count, start, 0.72f, fade);
            if (extend > 0.9f)
            {
                PushDisc(ref count, end, 1.15f * pulse, fade * 0.9f);
                PushDisc(ref count, end, 0.48f, fade);
            }

            float burst = Mathf.Clamp01(extend / 0.35f);
            for (int i = 0; i < 10 && count < buffer.Length; i++)
            {
                float ang = i / 10f * Mathf.PI * 2f + time * 2.4f;
                float dist = Mathf.Lerp(0.08f, 0.42f, burst) * (0.7f + 0.3f * Hash(i + 3f));
                Vector3 point = start + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * dist;
                PushDisc(ref count, point, Mathf.Lerp(0.16f, 0.05f, burst), fade * (1f - burst * 0.35f));
            }

            Apply(muzzle, count);
        }

        Vector3 FilamentPoint(float t, float time, float spin, float seed, float sign, float along)
        {
            Vector3 center = Sample(t, time, spin);
            Vector3 tangent = Tangent(t, time, spin);
            Vector3 side = new Vector3(-tangent.y, tangent.x, 0f);
            float jag = (Noise(along * 16f + seed + time * 5.5f) - 0.5f) * 2f;
            float amp = 0.055f + 0.04f * Noise(seed + along * 2.2f);
            return center + side * jag * amp * sign;
        }

        Vector3 Sample(float t, float time, float spin)
        {
            const float split = 0.7f;
            Vector3 dir = Direction;
            Vector3 side = Side;
            float radius = spiralRadius * (0.96f + 0.04f * Mathf.Sin(time * 5f));
            Vector3 entry = end + side * radius;
            if (t <= split)
            {
                float u = t / split;
                float wobble = (Mathf.PerlinNoise(u * 1.7f + 2.2f, time * 0.22f) - 0.5f) * 0.1f * Mathf.Sin(u * Mathf.PI);
                return Bezier(start, entry, dir, u) + side * wobble;
            }

            float s = Mathf.InverseLerp(split, 1f, t);
            float ang = s * spiralTurns * Mathf.PI * 2f;
            float rad = radius * Mathf.Lerp(1f, 0.62f, s * s);
            Vector3 point = end + side * Mathf.Cos(ang) * rad + dir * Mathf.Sin(ang) * rad;
            point.z = Mathf.Sin(ang) * 0.16f;
            return point;
        }

        static Vector3 Bezier(Vector3 from, Vector3 to, Vector3 direction, float u)
        {
            float handle = Vector3.Distance(from, to) * 0.38f;
            Vector3 p1 = from + direction * handle;
            Vector3 p2 = to - direction * handle;
            float k = 1f - u;
            return k * k * k * from + 3f * k * k * u * p1 + 3f * k * u * u * p2 + u * u * u * to;
        }

        Vector3 Tangent(float t, float time, float spin)
        {
            float a = Mathf.Max(0f, t - 0.008f);
            float b = Mathf.Min(1f, t + 0.008f);
            Vector3 delta = Sample(b, time, spin) - Sample(a, time, spin);
            if (delta.sqrMagnitude < 0.000001f)
                return Direction;
            return delta.normalized;
        }

        float WidthScale(float t)
        {
            float body = Mathf.Lerp(1.15f, 1f, Mathf.Clamp01(t * 6f));
            if (t <= 0.7f)
                return body;

            float s = Mathf.InverseLerp(0.7f, 1f, t);
            return Mathf.Lerp(1.2f, 0.28f, s * s);
        }

        float Brightness(float t, float time, float fade)
        {
            float flow = Mathf.Repeat(time * 1.25f, 1f);
            float band = Mathf.Exp(-Mathf.Pow((Mathf.Repeat(t - flow + 1f, 1f)) * 6.5f, 2f));
            float muzzle = Mathf.Exp(-t * 14f) * 0.45f;
            float spiral = t > 0.7f ? 0.28f : 0f;
            float tail = t < 0.9f ? 1f : Mathf.InverseLerp(1f, 0.9f, t);
            return fade * tail * (0.7f + muzzle + spiral + band * 0.45f);
        }

        void PushStreak(ref int count, Vector3 a, Vector3 b, float width, float brightness)
        {
            Vector3 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.0001f || count >= buffer.Length)
                return;

            var particle = new ParticleSystem.Particle();
            particle.position = (a + b) * 0.5f;
            particle.startLifetime = 5f;
            particle.remainingLifetime = 5f;
            particle.velocity = delta / length * (length * 1.28f);
            particle.startSize3D = Vector3.one * Mathf.Max(0.004f, width);
            particle.startSize = Mathf.Max(0.004f, width);
            particle.rotation = 0f;
            particle.startColor = White(brightness);
            buffer[count++] = particle;
        }

        void PushDisc(ref int count, Vector3 point, float size, float brightness)
        {
            if (count >= buffer.Length)
                return;

            var particle = new ParticleSystem.Particle();
            particle.position = point;
            particle.startLifetime = 5f;
            particle.remainingLifetime = 5f;
            particle.startSize3D = new Vector3(size, size, 1f);
            particle.rotation = 0f;
            particle.startColor = White(brightness);
            buffer[count++] = particle;
        }

        void Apply(ParticleSystem system, int count)
        {
            if (system == null)
                return;

            if (!system.isPlaying)
                system.Play(false);
            system.SetParticles(buffer, count);
        }

        static void ConfigureRenderer(ParticleSystem system, bool stretch)
        {
            if (system == null)
                return;

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.cameraVelocityScale = 0f;
            renderer.enableGPUInstancing = false;
            renderer.maxParticleSize = 5f;
            if (stretch)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 1f;
                renderer.lengthScale = 0f;
            }
            else
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
        }

        static Color32 White(float brightness)
        {
            byte value = (byte)Mathf.Clamp(Mathf.RoundToInt(brightness * 255f), 0, 255);
            return new Color32(255, 255, 255, value);
        }

        Vector3 Direction
        {
            get
            {
                Vector3 dir = end - start;
                if (dir.sqrMagnitude < 0.0001f)
                    return Vector3.right;
                dir.z = 0f;
                return dir.normalized;
            }
        }

        Vector3 Side
        {
            get
            {
                Vector3 dir = Direction;
                return new Vector3(-dir.y, dir.x, 0f);
            }
        }

        static float Hash(float value)
        {
            return Mathf.Repeat(Mathf.Sin(value * 127.1f) * 43758.55f, 1f);
        }

        static float Noise(float value)
        {
            float i = Mathf.Floor(value);
            float f = value - i;
            f = f * f * (3f - 2f * f);
            return Mathf.Lerp(Hash(i), Hash(i + 1f), f);
        }

        static float Repeat01(float value)
        {
            return Mathf.Repeat(value, 1f);
        }

        ParticleSystem PrepareSystem(string systemName, ParticleSystem existing, Material material)
        {
            ParticleSystem system = existing;
            if (system == null)
            {
                Transform child = transform.Find(systemName);
                if (child != null)
                    system = child.GetComponent<ParticleSystem>();
            }

            if (system == null)
            {
                var child = new GameObject(systemName);
                child.transform.SetParent(transform, false);
                system = child.AddComponent<ParticleSystem>();
            }

            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.simulationSpeed = 0f;
            main.maxParticles = 512;
            main.startLifetime = 5f;
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startColor = Color.white;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.enableGPUInstancing = false;
            renderer.maxParticleSize = 5f;
            renderer.minParticleSize = 0f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortMode = ParticleSystemSortMode.None;
            if (!system.isPlaying)
                system.Play(false);
            return system;
        }

        static Material PrepareMat(Material existing, Shader shader, string matName, float shape, Color tint, float intensity, float softness, float flicker, int queue)
        {
            Material material = existing != null ? existing : new Material(shader) { name = matName };
            material.shader = shader;
            material.SetFloat("_Shape", shape);
            material.SetColor("_Tint", tint);
            material.SetFloat("_Intensity", intensity);
            material.SetFloat("_Softness", softness);
            material.SetFloat("_Flicker", flicker);
            material.renderQueue = queue;
            return material;
        }

        static void Assign(ParticleSystem system, Material material)
        {
            if (system == null)
                return;
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        }

#if UNITY_EDITOR
        static Material SaveMat(Material material, string path)
        {
            if (material == null)
                return null;

            var current = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (current != null && current != material)
            {
                current.shader = material.shader;
                current.CopyPropertiesFromMaterial(material);
                current.renderQueue = material.renderQueue;
                UnityEditor.EditorUtility.SetDirty(current);
                return current;
            }

            if (current == null)
                UnityEditor.AssetDatabase.CreateAsset(material, path);
            return material;
        }
#endif
    }
}
