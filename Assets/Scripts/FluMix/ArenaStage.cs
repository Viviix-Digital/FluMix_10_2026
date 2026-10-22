using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    /// <summary>
    /// Places heroes and viruses on the 2D arena canvas. Lanes run from the
    /// right side of each floor band toward the castle on the left.
    /// </summary>
    public class ArenaStage : MonoBehaviour
    {
        [SerializeField] RectTransform arena;
        [SerializeField] RectTransform actors;
        [SerializeField] Vector2[] laneSpawn;
        [SerializeField] Vector2[] laneWall;
        [SerializeField] float[] laneScale;
        [SerializeField] float dropHeight = 340f;
        [Header("Scale lúc animation tấn công")]
        [SerializeField] float attackScaleA = 1f;
        [SerializeField] float attackScaleG = 1f;
        [SerializeField] float attackScaleT = 1.39f;

        readonly Dictionary<HeroId, HeroView> heroViews = new Dictionary<HeroId, HeroView>();
        readonly Dictionary<HeroId, Vector3> heroBaseScale = new Dictionary<HeroId, Vector3>();

        public bool Ready =>
            arena != null && laneSpawn != null && laneSpawn.Length > 0 && laneWall != null && laneWall.Length > 0;

        public int LaneCount => laneSpawn != null ? laneSpawn.Length : 0;

        public Transform Model => null;

        HeroId? armedHero;

        void Awake()
        {
            EnsureLanes();
            EnsureActors();
        }

        public void SetArmed(HeroId? id)
        {
            armedHero = id;
            ApplyHeroPose();
        }

        public Vector2 LaneLocal(int lane, float progress)
        {
            EnsureLanes();
            int index = Mathf.Clamp(lane, 0, laneSpawn.Length - 1);
            int wallIndex = Mathf.Clamp(lane, 0, laneWall.Length - 1);
            return Vector2.Lerp(laneSpawn[index], laneWall[wallIndex], Mathf.Clamp01(progress));
        }

        public void LayoutHeroes(HeroView[] views)
        {
            if (views == null)
                return;

            for (int i = 0; i < views.Length; i++)
            {
                HeroView hero = views[i];
                if (hero == null)
                    continue;

                heroViews[hero.Id] = hero;
                RectTransform rect = hero.GetComponent<RectTransform>();
                if (rect != null && !heroBaseScale.ContainsKey(hero.Id))
                    heroBaseScale[hero.Id] = rect.localScale;
                Show(hero.Image);
            }

            ApplyHeroPose();
        }

        public void PlaceVirus(VirusView virus, int lane, float progress, float land)
        {
            if (virus == null)
                return;

            EnsureActors();
            if (actors != null && virus.Rect.parent != actors)
                virus.Rect.SetParent(actors, false);

            float settled = Mathf.Clamp01(land);
            Vector2 landed = LaneLocal(lane, progress);
            virus.Rect.pivot = new Vector2(0.5f, 0.12f);
            virus.Rect.anchoredPosition = landed + Vector2.up * Mathf.Lerp(dropHeight, 0f, settled);
            float scale = ScaleOf(lane) * Mathf.Lerp(1.15f, 1f, settled);
            virus.SetScale(Vector3.one * scale);
            Show(virus.Image);
            if (virus.Image != null)
                virus.Image.raycastTarget = true;
            if (actors != null)
                virus.Rect.SetSiblingIndex(Mathf.Clamp(lane, 0, Mathf.Max(0, actors.childCount - 1)));
        }

        public void ReleaseVirus(VirusView virus)
        {
        }

        public void ClearViruses()
        {
        }

        void LateUpdate()
        {
            ApplyHeroPose();
        }

        void ApplyHeroPose()
        {
            foreach (KeyValuePair<HeroId, HeroView> pair in heroViews)
            {
                HeroView hero = pair.Value;
                if (hero == null)
                    continue;

                RectTransform rect = hero.GetComponent<RectTransform>();
                if (rect == null)
                    continue;

                if (!heroBaseScale.TryGetValue(pair.Key, out Vector3 basis))
                    basis = Vector3.one;

                float attack = AttackScale(hero);
                rect.localScale = basis * attack;
                if (hero.Image == null)
                    continue;

                bool armed = armedHero.HasValue && armedHero.Value == pair.Key;
                Color tint = armed ? new Color(1f, 0.92f, 0.35f, 1f) : Color.white;
                hero.Image.color = tint;
            }
        }

        float ScaleOf(int lane)
        {
            if (laneScale == null || laneScale.Length < 3)
                laneScale = new[] { 0.72f, 1.05f, 1.5f };
            int index = Mathf.Clamp(lane, 0, laneScale.Length - 1);
            float scale = laneScale[index];
            return scale > 0.05f ? scale : 1f;
        }

        float AttackScale(HeroView hero)
        {
            if (hero == null || hero.Sheet == null || !hero.Sheet.IsPlaying)
                return 1f;
            float scale = hero.Id == HeroId.G ? attackScaleG : hero.Id == HeroId.T ? attackScaleT : attackScaleA;
            return scale > 0.05f ? scale : 1f;
        }

        void EnsureLanes()
        {
            if (laneSpawn != null && laneSpawn.Length > 0 && laneWall != null && laneWall.Length > 0)
                return;

            laneSpawn = new[]
            {
                new Vector2(620f, -123f),
                new Vector2(760f, -270f),
                new Vector2(860f, -455f)
            };
            laneWall = new[]
            {
                new Vector2(-210f, -143f),
                new Vector2(-450f, -255f),
                new Vector2(-700f, -425f)
            };
            laneScale = new[] { 0.72f, 1.05f, 1.5f };
        }

        void EnsureActors()
        {
            if (actors != null || arena == null)
                return;

            Transform existing = arena.Find("LaneActors");
            if (existing != null)
            {
                actors = existing as RectTransform;
                return;
            }

            var go = new GameObject("LaneActors", typeof(RectTransform));
            actors = go.GetComponent<RectTransform>();
            actors.SetParent(arena, false);
            actors.anchorMin = new Vector2(0.5f, 0.5f);
            actors.anchorMax = new Vector2(0.5f, 0.5f);
            actors.pivot = new Vector2(0.5f, 0.5f);
            actors.anchoredPosition = Vector2.zero;
            actors.sizeDelta = arena.rect.size;
            actors.SetAsFirstSibling();
        }

        static void Show(Image image)
        {
            if (image == null)
                return;
            Color color = image.color;
            color.a = 1f;
            image.color = color;
        }
    }
}
