using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    /// <summary>
    /// Places heroes and viruses on the 2D arena canvas. Lanes run from the
    /// right side of each floor band toward the castle on the left.
    /// Virus entrance uses editable RectTransform anchors (like hero Muzzle):
    /// 3 flight origins + 3 land/spawn points.
    /// </summary>
    public class ArenaStage : MonoBehaviour
    {
        [SerializeField] RectTransform arena;
        [SerializeField] RectTransform actors;
        [SerializeField] Vector2[] laneSpawn;
        [SerializeField] Vector2[] laneWall;
        [SerializeField] float[] laneScale;
        [Header("Virus entrance anchors (kéo trong Scene như Muzzle)")]
        [SerializeField] RectTransform[] virusFlightPoints;
        [SerializeField] RectTransform[] virusLandPoints;
        [Tooltip("Seconds for diagonal flight from Flight point to Land point.")]
        [SerializeField] float dropDuration = 1.4f;
        [Header("Scale lúc animation event công")]
        [SerializeField] float attackScaleA = 1f;
        [SerializeField] float attackScaleG = 1f;
        [SerializeField] float attackScaleT = 1.39f;

        readonly Dictionary<HeroId, HeroView> heroViews = new Dictionary<HeroId, HeroView>();
        readonly Dictionary<HeroId, Vector3> heroBaseScale = new Dictionary<HeroId, Vector3>();

        public bool Ready =>
            arena != null && laneSpawn != null && laneSpawn.Length > 0 && laneWall != null && laneWall.Length > 0;

        public int LaneCount => laneSpawn != null ? laneSpawn.Length : 0;

        public float DropDuration => dropDuration > 0.1f ? dropDuration : 1.4f;

        public Transform Model => null;

        public void SetArmed(HeroId? id)
        {
        }

        void Awake()
        {
            EnsureLanes();
            EnsureActors();
            EnsureEntranceAnchors();
        }

        public Vector2 LaneLocal(int lane, float progress)
        {
            EnsureLanes();
            EnsureEntranceAnchors();
            int index = Mathf.Clamp(lane, 0, laneSpawn.Length - 1);
            int wallIndex = Mathf.Clamp(lane, 0, laneWall.Length - 1);
            Vector2 spawn = LandLocal(index);
            return Vector2.Lerp(spawn, laneWall[wallIndex], Mathf.Clamp01(progress));
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
            EnsureEntranceAnchors();
            if (actors != null && virus.Rect.parent != actors)
                virus.Rect.SetParent(actors, false);

            float settled = Mathf.Clamp01(land);
            // Feet on the lane path (Land → Wall); entrance still flies diagonally from Flight_*.
            virus.Rect.pivot = new Vector2(0.5f, 0.12f);
            if (settled < 1f)
            {
                Vector2 from = FlightLocal(lane);
                Vector2 to = LandLocal(lane);
                virus.Rect.anchoredPosition = Vector2.Lerp(from, to, settled);
            }
            else
            {
                // Ground move: stay on the lane from this lane's Land point toward the castle wall.
                virus.Rect.anchoredPosition = LaneLocal(lane, progress);
            }

            float scale = ScaleOf(lane) * Mathf.Lerp(1.15f, 1f, settled);
            virus.SetScale(Vector3.one * scale);
            Show(virus.Image);
            if (virus.Image != null)
                virus.Image.raycastTarget = false;
            if (actors != null)
                virus.Rect.SetSiblingIndex(Mathf.Clamp(lane, 0, Mathf.Max(0, actors.childCount - 1)));
        }

        Vector2 FlightLocal(int lane)
        {
            EnsureEntranceAnchors();
            int index = Mathf.Clamp(lane, 0, 2);
            if (virusFlightPoints != null && index < virusFlightPoints.Length && virusFlightPoints[index] != null)
                return PointInActors(virusFlightPoints[index]);

            Vector2 land = LandLocal(index);
            return land + new Vector2(80f, 520f - index * 40f);
        }

        Vector2 LandLocal(int lane)
        {
            EnsureLanes();
            EnsureEntranceAnchors();
            int index = Mathf.Clamp(lane, 0, laneSpawn.Length - 1);
            if (virusLandPoints != null && index < virusLandPoints.Length && virusLandPoints[index] != null)
            {
                Vector2 local = PointInActors(virusLandPoints[index]);
                laneSpawn[index] = local;
                return local;
            }

            return laneSpawn[index];
        }

        Vector2 PointInActors(RectTransform point)
        {
            RectTransform space = actors != null ? actors : arena;
            if (space == null || point == null)
                return Vector2.zero;

            Vector3 world = point.TransformPoint(Vector3.zero);
            Vector3 local = space.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
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

        /// <summary>
        /// Creates / wires VirusEntrance/Flight_0..2 and Land_0..2 under arena
        /// so they can be dragged in the Scene view like hero Muzzle.
        /// </summary>
        public void EnsureEntranceAnchors()
        {
            EnsureLanes();
            EnsureActors();
            if (arena == null)
                return;

            Transform root = arena.Find("VirusEntrance");
            RectTransform rootRect;
            if (root == null)
            {
                var go = new GameObject("VirusEntrance", typeof(RectTransform));
                rootRect = go.GetComponent<RectTransform>();
                rootRect.SetParent(arena, false);
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = Vector2.zero;
            }
            else
            {
                rootRect = root as RectTransform;
            }

            if (virusLandPoints == null || virusLandPoints.Length != 3)
                virusLandPoints = new RectTransform[3];
            if (virusFlightPoints == null || virusFlightPoints.Length != 3)
                virusFlightPoints = new RectTransform[3];

            for (int i = 0; i < 3; i++)
            {
                if (virusLandPoints[i] == null)
                {
                    Vector2 pos = i < laneSpawn.Length ? laneSpawn[i] : new Vector2(700f, -200f);
                    virusLandPoints[i] = EnsureAnchor(rootRect, "Land_" + i, pos);
                }

                if (virusFlightPoints[i] == null)
                {
                    // Top-right diagonal entrance (~85% right), not aligned to Land X.
                    Vector2 flight = new Vector2(640f + i * 40f, 540f - i * 55f);
                    virusFlightPoints[i] = EnsureAnchor(rootRect, "Flight_" + i, flight);
                }
            }
        }

        static RectTransform EnsureAnchor(RectTransform parent, string name, Vector2 anchored)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing as RectTransform;

            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(24f, 24f);
            rect.anchoredPosition = anchored;
            return rect;
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
