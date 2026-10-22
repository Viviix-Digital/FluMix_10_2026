using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FluMix
{
    public class BattleController : MonoBehaviour
    {
        class ActiveVirus
        {
            public VirusView view;
            public int lane;
            public int cell;
            public float stepTimer;
            public float slide;
            public float drop;
            public bool airborne;
            public bool dying;
        }

        class Launcher
        {
            public HeroView hero;
            public AimDashes guide;
            public Vector2 aim;
            public bool dragging;
            public bool flying;
            public float pull;
            public Vector2 lockedOrigin;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ScoreKeeper score;
        [SerializeField] FxPlayer fx;
        [SerializeField] RectTransform arena;
        [SerializeField] RectTransform spawnPoint;
        [SerializeField] RectTransform wallPoint;
        [SerializeField] HeroView[] heroes;
        [SerializeField] VirusView virusPrefab;
        [SerializeField] Sprite greenSprite;
        [SerializeField] Sprite orangeSprite;
        [SerializeField] Sprite redSprite;
        [SerializeField] Sprite greenDrop;
        [SerializeField] Sprite orangeDrop;
        [SerializeField] Sprite redDrop;
        [SerializeField] Sprite[] orangeFrames;
        [SerializeField] Sprite[] redFrames;
        [SerializeField] bool greenUsesSheet;
        [SerializeField] ArenaStage stage;

        readonly List<ActiveVirus> alive = new List<ActiveVirus>();
        readonly List<Launcher> launchers = new List<Launcher>();
        Sprite[] greenFrames;
        RectTransform guideRoot;

        bool running;
        bool paused;
        bool inputLocked;
        VirusType? pendingSpawn;

        public bool IsPaused => paused;
        public bool IsBusy => inputLocked;
        public VirusType? CurrentType => alive.Count > 0 ? FrontVirus()?.view.Type : (VirusType?)null;

        public HeroId CorrectHero =>
            CurrentType.HasValue && config != null ? config.HeroFor(CurrentType.Value) : HeroId.A;

        public string DescribeAlive()
        {
            if (alive.Count == 0)
                return "Virus: không có";

            var parts = new System.Text.StringBuilder();
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i] == null || alive[i].view == null)
                    continue;
                if (parts.Length > 0)
                    parts.Append(" | ");
                VirusType type = alive[i].view.Type;
                parts.Append(type);
                parts.Append("→");
                parts.Append(config != null ? config.HeroFor(type) : HeroId.A);
            }

            return parts.Length == 0 ? "Virus: không có" : parts.ToString();
        }

        public void Bind(
            GameConfig gameConfig,
            ScoreKeeper scoreKeeper,
            FxPlayer effects,
            RectTransform arenaRect,
            RectTransform spawn,
            RectTransform wall,
            HeroView[] heroViews,
            VirusView prefab,
            Sprite green,
            Sprite orange,
            Sprite red)
        {
            config = gameConfig;
            score = scoreKeeper;
            fx = effects;
            arena = arenaRect;
            spawnPoint = spawn;
            wallPoint = wall;
            heroes = heroViews;
            virusPrefab = prefab;
            greenSprite = green;
            orangeSprite = orange;
            redSprite = red;
        }

        public void AdoptConfig(GameConfig gameConfig)
        {
            config = gameConfig;
            if (heroes == null)
                return;

            for (int i = 0; i < heroes.Length; i++)
            {
                if (heroes[i] == null)
                    continue;
                SpriteSheetPlayer sheet = heroes[i].GetComponent<SpriteSheetPlayer>();
                if (sheet != null)
                    sheet.AdoptConfig(gameConfig);
            }
        }

        public void SetGreenSheet(bool enabled)
        {
            greenUsesSheet = enabled;
        }

        void Awake()
        {
            if (stage == null)
                stage = FindFirstObjectByType<ArenaStage>();
            if (virusPrefab != null && virusPrefab.Sheet != null)
                greenFrames = virusPrefab.Sheet.Frames;
        }

        void Start()
        {
            if (heroes == null)
                return;

            for (int i = 0; i < heroes.Length; i++)
            {
                HeroView hero = heroes[i];
                if (hero == null)
                    continue;
                hero.AimBegan -= OnAimBegan;
                hero.AimBegan += OnAimBegan;
                hero.AimMoved -= OnAimMoved;
                hero.AimMoved += OnAimMoved;
                hero.AimEnded -= OnAimEnded;
                hero.AimEnded += OnAimEnded;
            }
        }

        void Update()
        {
            if (!running || paused || config == null)
                return;

            float stepWait = config.virusStepSeconds > 0.1f ? config.virusStepSeconds : 2.8f;
            float slideTime = config.virusStepSlideSeconds > 0.05f ? config.virusStepSlideSeconds : 0.45f;
            int cells = config.virusCellCount > 1 ? config.virusCellCount : 8;
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                ActiveVirus virus = alive[i];
                if (virus.dying || virus.view == null)
                    continue;

                if (virus.airborne)
                {
                    virus.drop = Mathf.Min(1f, virus.drop + Time.deltaTime / 0.7f);
                    PlaceVirus(virus, ProgressOf(virus));
                    if (virus.drop >= 1f)
                        Land(virus);
                    continue;
                }

                if (virus.slide < 1f && virus.cell > 0)
                {
                    virus.slide = Mathf.Min(1f, virus.slide + Time.deltaTime / slideTime);
                }
                else
                {
                    virus.stepTimer += Time.deltaTime;
                    if (virus.stepTimer >= stepWait)
                    {
                        virus.stepTimer = 0f;
                        virus.cell++;
                        virus.slide = 0f;
                    }
                }

                float along = (Mathf.Max(0, virus.cell - 1) + (virus.cell > 0 ? virus.slide : 0f)) / cells;
                PlaceVirus(virus, Mathf.Clamp01(along));
                if (virus.cell >= cells && virus.slide >= 1f)
                    StartCoroutine(ResolveWall(virus));
            }
        }

        public void BeginRound()
        {
            StopAllCoroutines();
            ClearViruses();
            running = true;
            paused = false;
            inputLocked = false;
            pendingSpawn = null;
            if (stage != null)
                stage.LayoutHeroes(heroes);
            EnsureLaunchers();
            ResetAims();
            SetHeroesInteractable(true);
            EnsurePopulation();
        }

        public void StopRound()
        {
            StopAllCoroutines();
            running = false;
            paused = true;
            inputLocked = false;
            if (stage != null)
                stage.SetArmed(null);
            ResetAims();
            HideGuides();
            SetHeroesInteractable(false);
            ClearViruses();
        }

        public void SetPaused(bool value)
        {
            paused = value;
            if (value && stage != null)
                stage.SetArmed(null);
            SetHeroesInteractable(running && !value && !inputLocked);
        }

        public void Spawn(VirusType type)
        {
            ClearViruses();
            pendingSpawn = type;
            SpawnOne(type);
        }

        public void ForceCurrentReachWall()
        {
            if (alive.Count == 0)
                return;
            ActiveVirus virus = alive[0];
            virus.cell = config != null && config.virusCellCount > 1 ? config.virusCellCount : 8;
            virus.slide = 1f;
            if (!virus.dying)
                StartCoroutine(ResolveWall(virus));
        }

        public void FillIfNeeded()
        {
            if (running && !paused)
                EnsurePopulation();
        }

        void LateUpdate()
        {
            bool show = running && !paused && arena != null;
            if (guideRoot != null)
                guideRoot.SetAsLastSibling();

            Rect bounds = arena != null ? arena.rect : new Rect(-960f, -540f, 1920f, 1080f);
            for (int i = 0; i < launchers.Count; i++)
            {
                Launcher launcher = launchers[i];
                if (launcher.hero == null || launcher.guide == null)
                    continue;
                if (!show || !launcher.dragging || launcher.flying)
                {
                    launcher.guide.Hide();
                    continue;
                }

                Vector2 origin = MuzzleOf(launcher.hero);
                Vector2 direction = launcher.aim.sqrMagnitude > 1f ? launcher.aim.normalized : Vector2.right;
                ActiveVirus target = RayHit(origin, direction);
                float reach = AimDashes.ExitDistance(origin, direction, bounds);
                Vector2 end = target != null ? VirusBody(target) : origin + direction * Mathf.Max(reach, 80f);
                launcher.guide.Draw(origin, end);
            }
        }

        void OnAimBegan(HeroView hero, PointerEventData eventData)
        {
            if (!CanAim(hero))
                return;

            Launcher launcher = FindLauncher(hero);
            if (launcher == null || launcher.flying)
                return;

            launcher.dragging = true;
            launcher.pull = 0f;
            for (int i = 0; i < launchers.Count; i++)
            {
                if (launchers[i] != launcher)
                    launchers[i].dragging = false;
            }
            if (stage != null)
                stage.SetArmed(hero.Id);
        }

        void OnAimMoved(HeroView hero, PointerEventData eventData)
        {
            Launcher launcher = FindLauncher(hero);
            if (launcher == null || !launcher.dragging || !CanAim(hero))
                return;
            if (!PointerArena(eventData, out Vector2 pointer))
                return;

            ApplyAim(launcher, hero, pointer);
        }

        void OnAimEnded(HeroView hero, PointerEventData eventData)
        {
            Launcher launcher = FindLauncher(hero);
            if (launcher == null)
                return;

            bool outward = false;
            if (PointerArena(eventData, out Vector2 pointer))
            {
                ApplyAim(launcher, hero, pointer);
                outward = AimingOutward(hero, pointer);
            }

            bool fire = launcher.dragging && CanAim(hero) && launcher.pull >= 96f && outward;
            launcher.dragging = false;
            if (stage != null)
                stage.SetArmed(null);
            if (fire)
                StartCoroutine(FireBeam(launcher));
        }

        void ApplyAim(Launcher launcher, HeroView hero, Vector2 pointer)
        {
            Vector2 origin = MuzzleOf(hero);
            Vector2 pull = pointer - origin;
            float mag = pull.magnitude;
            launcher.pull = Mathf.Max(launcher.pull, mag);
            if (!AimingOutward(hero, pointer) || mag < 36f)
                return;

            float max = config != null && config.eggDragMax > 40f ? config.eggDragMax : 460f;
            float clamped = Mathf.Min(mag, max);
            float minSpeed = config != null ? config.eggMinSpeed : 520f;
            float maxSpeed = config != null ? config.eggMaxSpeed : 1320f;
            float speed = Mathf.Lerp(minSpeed, maxSpeed, clamped / max);
            launcher.aim = pull.normalized * speed;
        }

        bool AimingOutward(HeroView hero, Vector2 pointer)
        {
            RectTransform rect = hero.GetComponent<RectTransform>();
            if (rect == null)
                return true;

            Vector2 center = MuzzleOf(hero);
            if (fx != null)
                center = fx.ArenaPoint(rect);
            float reach = Mathf.Max(rect.rect.width * Mathf.Abs(rect.localScale.x), rect.rect.height * Mathf.Abs(rect.localScale.y)) * 0.46f;
            return Vector2.Distance(pointer, center) > Mathf.Max(72f, reach);
        }

        bool CanAim(HeroView hero)
        {
            return running && !paused && !inputLocked && hero != null && config != null;
        }

        IEnumerator FireBeam(Launcher launcher)
        {
            HeroView hero = launcher.hero;
            if (hero == null)
                yield break;

            inputLocked = true;
            launcher.flying = true;
            launcher.lockedOrigin = MuzzleOf(hero);
            Vector2 origin = launcher.lockedOrigin;
            Vector2 direction = launcher.aim.sqrMagnitude > 1f ? launcher.aim.normalized : Vector2.right;
            if (hero.Sheet != null && hero.Sheet.HasFrames)
                hero.Sheet.PlayOnce(null);
            if (fx != null)
                StartCoroutine(fx.HeroRecoil(hero.GetComponent<RectTransform>()));

            ActiveVirus hit = RayHit(origin, direction);
            Rect bounds = arena != null ? arena.rect : new Rect(-960f, -540f, 1920f, 1080f);
            float distance = AimDashes.ExitDistance(origin, direction, bounds);
            Vector2 end = hit != null ? VirusBody(hit) : origin + direction * Mathf.Max(distance, 80f);
            if (fx != null)
                yield return fx.PlayBeam(hero.Id, origin, end, true, false);

            launcher.flying = false;
            if (!running || hit == null || hit.view == null)
            {
                inputLocked = false;
                yield break;
            }

            bool correct = config.HeroFor(hit.view.Type) == hero.Id;
            yield return ResolveImpact(hero, hit, correct);
        }

        IEnumerator ResolveImpact(HeroView hero, ActiveVirus virus, bool correct)
        {
            inputLocked = true;
            if (correct)
                virus.dying = true;

            Vector2 to = virus.view.Rect.anchoredPosition;
            int delta = correct ? config.correctHeroScore : config.wrongHeroScore;
            score.Add(delta);
            FluMixSfx.Play(correct ? "impact_correct" : "impact_wrong");
            if (fx != null)
                fx.PlayScorePopup(delta, to);

            if (correct)
            {
                if (config.hitStopSeconds > 0f)
                    yield return new WaitForSeconds(config.hitStopSeconds);
                if (virus.view != null && fx != null)
                {
                    virus.view.Pulse = false;
                    yield return fx.PlayVirusDeath(virus.view.Rect);
                }

                Remove(virus);
                yield return new WaitForSeconds(config.spawnDelay);
                if (running && !paused)
                    EnsurePopulation();
            }
            else if (virus.view != null && fx != null)
            {
                VirusType next = OtherType(virus.view.Type);
                Sprite sprite = SpriteFor(next);
                PrepareSheet(virus.view, next);
                yield return fx.PlayRetype(virus.view, () =>
                {
                    if (virus.view != null)
                        virus.view.Apply(next, sprite, UsesSheet(next));
                });
            }

            inputLocked = false;
        }

        IEnumerator ResolveWall(ActiveVirus virus)
        {
            if (virus.dying)
                yield break;

            virus.dying = true;
            FluMixSfx.Play("virus_leak");
            score.Add(config.virusReachScore);
            fx.PlayScorePopup(config.virusReachScore, virus.view.Rect.anchoredPosition);
            virus.view.Pulse = false;
            yield return fx.PlayVirusDeath(virus.view.Rect);
            Remove(virus);
            yield return new WaitForSeconds(config.spawnDelay);
            if (running && !paused)
                EnsurePopulation();
        }

        void EnsurePopulation()
        {
            int target = Mathf.Max(1, config.simultaneousViruses);
            while (alive.Count < target)
            {
                VirusType type = pendingSpawn ?? NextSpawnType();
                pendingSpawn = null;
                if (!SpawnOne(type))
                    break;
            }
        }

        bool SpawnOne(VirusType type)
        {
            if (virusPrefab == null || arena == null)
                return false;

            int lane = FreeLane();
            if (lane < 0)
                return false;

            VirusView view = Instantiate(virusPrefab, arena);
            view.gameObject.SetActive(true);
            view.Bind(config);
            if (view.Sheet != null)
                view.Sheet.AdoptConfig(config);
            PrepareSheet(view, type);
            view.Apply(type, SpriteFor(type), UsesSheet(type));
            FluMixSfx.Play("virus_spawn");
            Sprite falling = DropSprite(type);
            bool airborne = falling != null;
            if (airborne)
                view.Apply(type, falling, false);
            int startCell = lane == 1 ? 3 : lane == 2 ? 1 : 0;
            var active = new ActiveVirus
            {
                view = view,
                lane = lane,
                cell = startCell,
                slide = 1f,
                drop = airborne ? 0f : 1f,
                airborne = airborne
            };
            view.Rect.localScale = Vector3.one;
            view.Pulse = !UsesSheet(type);
            alive.Add(active);
            PlaceVirus(active, ProgressOf(active));
            return true;
        }

        int FreeLane()
        {
            int lanes = stage != null && stage.Ready ? stage.LaneCount : Mathf.Max(1, config != null ? config.simultaneousViruses : 3);
            for (int lane = 0; lane < lanes; lane++)
            {
                bool used = false;
                for (int i = 0; i < alive.Count; i++)
                {
                    if (alive[i].lane == lane && alive[i].view != null)
                    {
                        used = true;
                        break;
                    }
                }

                if (!used)
                    return lane;
            }

            return -1;
        }

        public bool TryVirusSpawnPoint(out Vector2 local)
        {
            local = Vector2.zero;
            if (stage == null || !stage.Ready)
                return false;

            int lane = stage.LaneCount > 1 ? 1 : 0;
            local = stage.LaneLocal(lane, 0f);
            return true;
        }

        void PlaceVirus(ActiveVirus virus, float progress)
        {
            if (virus == null || virus.view == null)
                return;

            if (stage != null && stage.Ready)
            {
                stage.PlaceVirus(virus.view, virus.lane, progress, virus.airborne ? virus.drop : 1f);
                return;
            }

            virus.view.Rect.anchoredPosition = Vector2.Lerp(Point(spawnPoint, virus.lane), Point(wallPoint, virus.lane), Mathf.Clamp01(progress));
        }

        float ProgressOf(ActiveVirus virus)
        {
            int cells = config != null && config.virusCellCount > 1 ? config.virusCellCount : 8;
            return Mathf.Clamp01((Mathf.Max(0, virus.cell - 1) + (virus.cell > 0 ? virus.slide : 0f)) / cells);
        }

        void Remove(ActiveVirus virus)
        {
            alive.Remove(virus);
            if (stage != null && virus.view != null)
                stage.ReleaseVirus(virus.view);
            if (virus.view != null)
                Destroy(virus.view.gameObject);
        }

        public void ClearField()
        {
            ClearViruses();
        }

        void ClearViruses()
        {
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i].view != null)
                    Destroy(alive[i].view.gameObject);
            }

            alive.Clear();
            if (stage != null)
                stage.ClearViruses();
        }

        ActiveVirus FrontVirus()
        {
            ActiveVirus best = null;
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i].dying)
                    continue;
                if (best == null || alive[i].cell > best.cell)
                    best = alive[i];
            }

            return best;
        }

        Vector2 Point(RectTransform anchor, int lane)
        {
            Vector2 origin = anchor != null ? anchor.anchoredPosition : Vector2.zero;
            float offset = (lane - (Mathf.Max(1, config.simultaneousViruses) - 1) * 0.5f) * config.laneSpacing;
            return origin + new Vector2(0f, offset);
        }

        void Land(ActiveVirus virus)
        {
            if (virus == null || virus.view == null)
                return;
            virus.airborne = false;
            virus.drop = 1f;
            VirusType type = virus.view.Type;
            virus.view.Apply(type, SpriteFor(type), UsesSheet(type));
            virus.view.Pulse = !UsesSheet(type);
            PlaceVirus(virus, ProgressOf(virus));
        }

        Sprite SpriteFor(VirusType type)
        {
            switch (type)
            {
                case VirusType.Orange: return orangeSprite;
                case VirusType.Red: return redSprite;
                default: return greenSprite;
            }
        }

        Sprite DropSprite(VirusType type)
        {
            switch (type)
            {
                case VirusType.Orange: return orangeDrop;
                case VirusType.Red: return redDrop;
                default: return greenDrop;
            }
        }

        void PrepareSheet(VirusView view, VirusType type)
        {
            if (view == null || view.Sheet == null)
                return;

            if (type == VirusType.Green)
            {
                view.SetClip(0, 8, 8, 4, 0, 0);
                if (greenFrames != null && greenFrames.Length > 0)
                    view.Sheet.SetFrames(greenFrames, greenSprite);
                return;
            }

            Sprite[] frames = type == VirusType.Orange ? orangeFrames : redFrames;
            if (!HasSprite(frames))
                return;

            int hit = Mathf.Min(4, frames.Length);
            int hitStart = Mathf.Max(0, frames.Length - hit);
            view.SetClip(0, frames.Length, hitStart, hit, 0, 0);
            view.Sheet.SetFrames(frames, frames[0]);
        }

        bool UsesSheet(VirusType type)
        {
            bool sheetsReady = type == VirusType.Green
                ? greenUsesSheet
                : HasSprite(type == VirusType.Orange ? orangeFrames : redFrames);
            return false && sheetsReady;
        }

        static bool HasSprite(Sprite[] frames)
        {
            if (frames == null)
                return false;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                    return true;
            }

            return false;
        }

        VirusType NextSpawnType()
        {
            bool green = false;
            bool orange = false;
            bool red = false;
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i] == null || alive[i].view == null)
                    continue;
                switch (alive[i].view.Type)
                {
                    case VirusType.Orange: orange = true; break;
                    case VirusType.Red: red = true; break;
                    default: green = true; break;
                }
            }

            int missing = 0;
            if (!green) missing++;
            if (!orange) missing++;
            if (!red) missing++;
            if (missing == 0)
                return RandomType();

            int pick = Random.Range(0, missing);
            if (!green)
            {
                if (pick == 0)
                    return VirusType.Green;
                pick--;
            }

            if (!orange)
            {
                if (pick == 0)
                    return VirusType.Orange;
                pick--;
            }

            return VirusType.Red;
        }

        static VirusType RandomType()
        {
            return (VirusType)Random.Range(0, 3);
        }

        static VirusType OtherType(VirusType current)
        {
            int roll = Random.Range(0, 2);
            if (current == VirusType.Green)
                return roll == 0 ? VirusType.Orange : VirusType.Red;
            if (current == VirusType.Orange)
                return roll == 0 ? VirusType.Green : VirusType.Red;
            return roll == 0 ? VirusType.Green : VirusType.Orange;
        }

        void EnsureLaunchers()
        {
            if (arena == null || heroes == null)
                return;
            if (guideRoot == null)
            {
                guideRoot = FluMixUi.CreateRect(arena, "ShotGuides", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                guideRoot.sizeDelta = arena.rect.size;
                guideRoot.gameObject.hideFlags = HideFlags.DontSave;
            }

            int dashes = Mathf.Max(48, config != null ? config.aimDashCount : 48);
            for (int i = 0; i < heroes.Length; i++)
            {
                HeroView hero = heroes[i];
                if (hero == null || FindLauncher(hero) != null)
                    continue;

                launchers.Add(new Launcher
                {
                    hero = hero,
                    guide = new AimDashes(guideRoot, dashes),
                    aim = RestAim(hero.Id)
                });
            }
        }

        void ResetAims()
        {
            for (int i = 0; i < launchers.Count; i++)
            {
                Launcher launcher = launchers[i];
                launcher.dragging = false;
                launcher.flying = false;
                launcher.pull = 0f;
                if (launcher.hero != null)
                    launcher.aim = RestAim(launcher.hero.Id);
            }
        }

        void HideGuides()
        {
            for (int i = 0; i < launchers.Count; i++)
            {
                if (launchers[i].guide != null)
                    launchers[i].guide.Hide();
            }
        }

        Launcher FindLauncher(HeroView hero)
        {
            for (int i = 0; i < launchers.Count; i++)
            {
                if (launchers[i].hero == hero)
                    return launchers[i];
            }

            return null;
        }

        static Vector2 RestAim(HeroId id)
        {
            switch (id)
            {
                case HeroId.G: return new Vector2(920f, 300f);
                case HeroId.T: return new Vector2(540f, 150f);
                default: return new Vector2(840f, 270f);
            }
        }

        ActiveVirus RayHit(Vector2 origin, Vector2 direction)
        {
            ActiveVirus best = null;
            float bestAlong = float.MaxValue;
            float hit = config != null && config.eggHitRadius > 1f ? config.eggHitRadius : 78f;
            for (int i = 0; i < alive.Count; i++)
            {
                ActiveVirus virus = alive[i];
                if (virus == null || virus.dying || virus.view == null)
                    continue;

                Vector2 center = VirusBody(virus);
                Vector2 delta = center - origin;
                float along = Vector2.Dot(delta, direction);
                if (along < 24f)
                    continue;

                float side = Mathf.Abs(delta.x * direction.y - delta.y * direction.x);
                float scale = Mathf.Max(0.35f, virus.view.Rect.localScale.x);
                float radius = hit * scale + 30f;
                if (side > radius || along >= bestAlong)
                    continue;

                best = virus;
                bestAlong = along;
            }

            return best;
        }

        Vector2 VirusBody(ActiveVirus virus)
        {
            float scale = virus != null && virus.view != null ? Mathf.Max(0.35f, virus.view.Rect.localScale.x) : 1f;
            return VirusCenter(virus) + new Vector2(0f, 56f * scale);
        }

        Vector2 MuzzleOf(HeroView hero)
        {
            if (hero == null)
                return Vector2.zero;
            if (fx != null)
                return fx.ArenaPoint(hero.Muzzle);
            if (arena == null)
                return Vector2.zero;
            Vector3 local = arena.InverseTransformPoint(hero.Muzzle.TransformPoint(Vector3.zero));
            return new Vector2(local.x, local.y);
        }

        bool PointerArena(PointerEventData eventData, out Vector2 local)
        {
            local = Vector2.zero;
            if (arena == null || eventData == null)
                return false;

            Canvas canvas = arena.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(arena, eventData.position, cam, out local);
        }

        Vector2 VirusCenter(ActiveVirus virus)
        {
            if (fx != null)
                return fx.ArenaPoint(virus.view.Rect);
            return virus.view.Rect.anchoredPosition;
        }

        void SetHeroesInteractable(bool value)
        {
            if (heroes == null)
                return;
            for (int i = 0; i < heroes.Length; i++)
            {
                if (heroes[i] != null)
                    heroes[i].SetInteractable(value);
            }
        }
    }
}
