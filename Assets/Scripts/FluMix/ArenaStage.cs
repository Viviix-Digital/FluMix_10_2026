using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FluMix
{
    /// <summary>
    /// Places the 2D heroes and viruses on the 3D arena and projects them back
    /// onto the gameplay canvas so clicks, beams, and sheets stay aligned.
    /// </summary>
    public class ArenaStage : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField] RectTransform arena;
        [SerializeField] Transform modelRoot;
        [SerializeField] Transform heroA;
        [SerializeField] Transform heroG;
        [SerializeField] Transform heroT;
        [SerializeField] Vector3[] laneSpawn;
        [SerializeField] Vector3[] laneWall;
        [SerializeField] float heroHeight = 3.4f;
        [SerializeField] float heroLift = 0.75f;
        [SerializeField] float virusHeight = 1.7f;
        [SerializeField] float heroBob = 0.12f;
        [SerializeField] float heroBobSeconds = 1.6f;

        readonly Dictionary<int, WorldBillboard> viruses = new Dictionary<int, WorldBillboard>();
        readonly Dictionary<HeroId, WorldBillboard> heroes = new Dictionary<HeroId, WorldBillboard>();
        readonly Dictionary<HeroId, HeroView> heroViews = new Dictionary<HeroId, HeroView>();
        Transform actorRoot;

        public bool Ready => view != null && arena != null && laneSpawn != null && laneSpawn.Length > 0 && laneWall != null && laneWall.Length > 0;

        public void Adopt(Camera camera, RectTransform arenaRect, Transform model, Transform a, Transform g, Transform t, Vector3[] spawns, Vector3[] walls, float heroesTall, float virusesTall)
        {
            view = camera;
            arena = arenaRect;
            modelRoot = model;
            heroA = a;
            heroG = g;
            heroT = t;
            laneSpawn = spawns;
            laneWall = walls;
            heroHeight = heroesTall;
            virusHeight = virusesTall;
        }

        HeroId? armedHero;

        public void SetArmed(HeroId? id)
        {
            armedHero = id;
        }

        public Vector3 HeroWorld(HeroId id)
        {
            Transform anchor = Anchor(id);
            if (anchor == null)
                return Vector3.zero;
            Vector3 world = anchor.position + Vector3.up * heroLift;
            if (view != null)
            {
                Vector3 toward = view.transform.position - world;
                toward.y = 0f;
                if (toward.sqrMagnitude > 0.01f)
                    world += toward.normalized * 0.45f;
            }

            return world;
        }

        public int LaneCount => laneSpawn != null ? laneSpawn.Length : 0;

        public Transform Model => modelRoot;

        public Vector3 LanePoint(int lane, float progress)
        {
            int index = Mathf.Clamp(lane, 0, laneSpawn.Length - 1);
            int wallIndex = Mathf.Clamp(lane, 0, laneWall.Length - 1);
            return Vector3.Lerp(laneSpawn[index], laneWall[wallIndex], Mathf.Clamp01(progress));
        }

        public bool Project(Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            if (view == null || arena == null)
                return false;

            Camera uiCamera = UiCamera();
            Camera projector = uiCamera != null ? uiCamera : view;
            Vector3 screen = projector.WorldToScreenPoint(world);
            if (screen.z <= 0f)
                return false;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(arena, screen, uiCamera, out local);
        }

        public void LayoutHeroes(HeroView[] views)
        {
            if (views == null)
                return;

            ClearHeroBoards();
            HideCard(heroA);
            HideCard(heroG);
            HideCard(heroT);
            for (int i = 0; i < views.Length; i++)
            {
                HeroView hero = views[i];
                if (hero == null)
                    continue;
                Transform anchor = Anchor(hero.Id);
                if (anchor == null)
                    continue;

                heroViews[hero.Id] = hero;
                Sprite sprite = hero.Image != null ? hero.Image.sprite : null;
                WorldBillboard board = BoardFor(heroes, hero.Id, "HeroBillboard " + hero.Id, heroHeight, sprite);
                Vector3 world = anchor.position + Vector3.up * heroLift;
                if (view != null)
                {
                    Vector3 toward = view.transform.position - world;
                    toward.y = 0f;
                    if (toward.sqrMagnitude > 0.01f)
                        world += toward.normalized * 0.45f;
                }
                board.Present(view, world, sprite, Vector3.one, 0f, Color.white);
                HideUi(hero.Image);
                if (Project(world, out Vector2 local))
                    hero.GetComponent<RectTransform>().anchoredPosition = local;
                FitClick(hero, world);
            }
        }

        public void PlaceVirus(VirusView virus, Vector3 world)
        {
            if (virus == null)
                return;

            Sprite sprite = virus.Image != null ? virus.Image.sprite : null;
            WorldBillboard board = BoardFor(viruses, virus.GetInstanceID(), "VirusBillboard", virusHeight, sprite);
            float flash = virus.Flash != null ? virus.Flash.color.a : 0f;
            Color tint = Color.Lerp(virus.Tint, Color.white, flash);
            board.ClickTarget = virus;
            board.Present(view, world, sprite, Vector3.one, virus.Rect.localEulerAngles.z, tint);
            HideUi(virus.Image);
            FitVirusClick(virus, world);
            if (Project(world, out Vector2 local))
                virus.Rect.anchoredPosition = local;
            virus.Rect.SetAsLastSibling();
        }

        public void ReleaseVirus(VirusView virus)
        {
            if (virus == null)
                return;
            int id = virus.GetInstanceID();
            if (!viruses.TryGetValue(id, out WorldBillboard board))
                return;
            viruses.Remove(id);
            if (board != null)
                Destroy(board.gameObject);
        }

        public void ClearViruses()
        {
            foreach (KeyValuePair<int, WorldBillboard> pair in viruses)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            viruses.Clear();
        }

        void Update()
        {
            TryClickVirus();
        }

        void TryClickVirus()
        {
            if (view == null || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            Vector2 screen = Mouse.current.position.ReadValue();
            if (PointerHitsHero(screen))
                return;

            VirusView target = null;
            float nearest = float.MaxValue;
            foreach (KeyValuePair<int, WorldBillboard> pair in viruses)
            {
                WorldBillboard board = pair.Value;
                if (board == null || !board.HitScreen(view, screen))
                    continue;
                float depth = Vector3.Distance(view.transform.position, board.transform.position);
                if (depth >= nearest)
                    continue;
                nearest = depth;
                target = board.ClickTarget;
            }

            if (target != null)
                target.NotifyClick();
        }

        static bool PointerHitsHero(Vector2 screen)
        {
            if (EventSystem.current == null)
                return false;

            var data = new PointerEventData(EventSystem.current) { position = screen };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject != null && results[i].gameObject.GetComponentInParent<HeroView>() != null)
                    return true;
            }

            return false;
        }

        Camera UiCamera()
        {
            if (arena == null)
                return null;
            Canvas canvas = arena.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;
            return canvas.worldCamera != null ? canvas.worldCamera : view;
        }

        void LateUpdate()
        {
            if (!Ready || heroes.Count == 0)
                return;

            float duration = Mathf.Max(0.01f, heroBobSeconds);
            int index = 0;
            foreach (KeyValuePair<HeroId, WorldBillboard> pair in heroes)
            {
                Transform anchor = Anchor(pair.Key);
                WorldBillboard board = pair.Value;
                if (anchor == null || board == null)
                {
                    index++;
                    continue;
                }

                float phase = index / Mathf.Max(1f, heroes.Count);
                float wave = Mathf.Sin((Time.time / duration + phase) * Mathf.PI * 2f);
                Vector3 world = anchor.position + Vector3.up * (wave * heroBob + heroLift);
                if (view != null)
                {
                    Vector3 toward = view.transform.position - anchor.position;
                    toward.y = 0f;
                    if (toward.sqrMagnitude > 0.01f)
                        world += toward.normalized * 0.45f;
                }
                Sprite sprite = null;
                if (heroViews.TryGetValue(pair.Key, out HeroView hero) && hero != null && hero.Image != null)
                {
                    sprite = hero.Image.sprite;
                    HideUi(hero.Image);
                    if (Project(world, out Vector2 local))
                        hero.GetComponent<RectTransform>().anchoredPosition = local;
                }

                Color tint = armedHero.HasValue && armedHero.Value == pair.Key
                    ? new Color(1f, 0.92f, 0.35f, 1f)
                    : Color.white;
                board.Present(view, world, sprite, Vector3.one, 0f, tint);
                index++;
            }
        }

        Transform Anchor(HeroId id)
        {
            switch (id)
            {
                case HeroId.G: return heroG;
                case HeroId.T: return heroT;
                default: return heroA;
            }
        }

        WorldBillboard BoardFor(Dictionary<HeroId, WorldBillboard> map, HeroId id, string label, float height, Sprite sprite)
        {
            if (map.TryGetValue(id, out WorldBillboard existing) && existing != null)
                return existing;

            WorldBillboard created = CreateBoard(label, height, sprite);
            map[id] = created;
            return created;
        }

        WorldBillboard BoardFor(Dictionary<int, WorldBillboard> map, int id, string label, float height, Sprite sprite)
        {
            if (map.TryGetValue(id, out WorldBillboard existing) && existing != null)
                return existing;

            WorldBillboard created = CreateBoard(label, height, sprite);
            map[id] = created;
            return created;
        }

        WorldBillboard CreateBoard(string label, float height, Sprite sprite)
        {
            if (actorRoot == null)
            {
                var root = new GameObject("StageActors");
                root.transform.SetParent(modelRoot != null ? modelRoot : transform, false);
                actorRoot = root.transform;
            }

            var go = new GameObject(label);
            go.transform.SetParent(actorRoot, false);
            var board = go.AddComponent<WorldBillboard>();
            board.Prepare(height, sprite);
            return board;
        }

        void FitVirusClick(VirusView virus, Vector3 world)
        {
            if (virus == null || view == null || arena == null)
                return;

            Camera uiCamera = UiCamera();
            Camera projector = uiCamera != null ? uiCamera : view;
            Vector3 top = projector.WorldToScreenPoint(world + Vector3.up * (virusHeight * 0.5f));
            Vector3 bottom = projector.WorldToScreenPoint(world - Vector3.up * (virusHeight * 0.5f));
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(arena, top, uiCamera, out Vector2 topLocal))
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(arena, bottom, uiCamera, out Vector2 bottomLocal))
                return;

            float height = Mathf.Abs(topLocal.y - bottomLocal.y);
            virus.Rect.sizeDelta = new Vector2(height, height);
        }

        void FitClick(HeroView hero, Vector3 world)
        {
            if (hero == null || view == null || arena == null)
                return;

            Camera uiCamera = UiCamera();
            Camera projector = uiCamera != null ? uiCamera : view;
            Vector3 top = projector.WorldToScreenPoint(world + Vector3.up * (heroHeight * 0.5f));
            Vector3 bottom = projector.WorldToScreenPoint(world - Vector3.up * (heroHeight * 0.5f));
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(arena, top, uiCamera, out Vector2 topLocal))
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(arena, bottom, uiCamera, out Vector2 bottomLocal))
                return;

            float height = Mathf.Abs(topLocal.y - bottomLocal.y);
            var rect = hero.GetComponent<RectTransform>();
            float aspect = 0.7f;
            if (hero.Image != null && hero.Image.sprite != null)
            {
                Vector2 size = hero.Image.sprite.rect.size;
                if (size.y > 1f)
                    aspect = size.x / size.y;
            }

            rect.sizeDelta = new Vector2(height * aspect, height);
        }

        void ClearHeroBoards()
        {
            heroes.Clear();
            heroViews.Clear();
            if (actorRoot == null)
                return;

            for (int i = actorRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = actorRoot.GetChild(i);
                if (child.name.StartsWith("HeroBillboard"))
                    Destroy(child.gameObject);
            }
        }

        static void HideCard(Transform card)
        {
            if (card == null)
                return;
            var renderers = card.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = false;
        }

        static void HideUi(Image image)
        {
            if (image == null)
                return;
            Color color = image.color;
            color.a = 0f;
            image.color = color;
            image.raycastTarget = true;
            if (image.canvasRenderer != null)
                image.canvasRenderer.cullTransparentMesh = false;
        }
    }
}
