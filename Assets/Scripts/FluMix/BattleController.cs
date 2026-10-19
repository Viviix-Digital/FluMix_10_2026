using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            public bool dying;
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
        [SerializeField] bool greenUsesSheet;
        [SerializeField] ArenaStage stage;

        readonly List<ActiveVirus> alive = new List<ActiveVirus>();

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
                hero.Clicked -= OnHeroClicked;
                hero.Clicked += OnHeroClicked;
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
            SetHeroesInteractable(true);
            EnsurePopulation();
        }

        public void StopRound()
        {
            StopAllCoroutines();
            running = false;
            paused = true;
            inputLocked = false;
            armed = null;
            if (stage != null)
                stage.SetArmed(null);
            SetHeroesInteractable(false);
            ClearViruses();
        }

        public void SetPaused(bool value)
        {
            paused = value;
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

        HeroView armed;
        int virusClickFrame = -1;

        void OnHeroClicked(HeroView hero)
        {
            if (!running || paused || inputLocked || hero == null)
                return;

            armed = armed == hero ? null : hero;
            if (stage != null)
                stage.SetArmed(armed != null ? armed.Id : (HeroId?)null);
        }

        public void OnVirusClicked(VirusView view)
        {
            if (!running || paused || inputLocked || armed == null || view == null)
                return;
            if (virusClickFrame == Time.frameCount)
                return;
            virusClickFrame = Time.frameCount;

            ActiveVirus virus = null;
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i].view == view && !alive[i].dying)
                {
                    virus = alive[i];
                    break;
                }
            }

            if (virus == null)
                return;

            HeroView hero = armed;
            armed = null;
            if (stage != null)
                stage.SetArmed(null);
            bool correct = config.HeroFor(virus.view.Type) == hero.Id;
            StartCoroutine(ResolveShot(hero, virus, correct));
        }

        IEnumerator ResolveShot(HeroView hero, ActiveVirus virus, bool correct)
        {
            inputLocked = true;
            if (correct)
                virus.dying = true;
            SetHeroesInteractable(false);

            if (hero.Sheet != null && hero.Sheet.HasFrames)
                hero.Sheet.PlayOnce(null);
            else
                StartCoroutine(fx.HeroRecoil(hero.GetComponent<RectTransform>()));

            Vector3 worldFrom = stage != null && stage.Ready ? stage.HeroWorld(hero.Id) : Vector3.zero;
            Vector3 worldTo = stage != null && stage.Ready ? stage.LanePoint(virus.lane, ProgressOf(virus)) : Vector3.zero;
            Vector2 from = fx.ArenaPoint(hero.Muzzle);
            Vector2 to = virus.view.Rect.anchoredPosition;
            int delta = correct ? config.correctHeroScore : config.wrongHeroScore;
            score.Add(delta);
            yield return fx.PlayAimArrow(from, to);
            fx.PlayScorePopup(delta, to);

            yield return fx.PlayBeam(hero.Id, from, to, correct, worldFrom, worldTo);

            if (correct)
            {
                yield return new WaitForSeconds(config.hitStopSeconds);
                virus.view.Pulse = false;
                yield return fx.PlayVirusDeath(virus.view.Rect);
                Remove(virus);
                yield return new WaitForSeconds(config.spawnDelay);
                if (running && !paused)
                    EnsurePopulation();
            }
            else if (virus.view != null)
            {
                VirusType next = OtherType(virus.view.Type);
                Sprite sprite = SpriteFor(next);
                yield return fx.PlayRetype(virus.view, () =>
                {
                    if (virus.view != null)
                        virus.view.Apply(next, sprite, UsesSheet(next));
                });
            }

            inputLocked = false;
            SetHeroesInteractable(running && !paused);
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
            view.Apply(type, SpriteFor(type), UsesSheet(type));
            view.Clicked -= OnVirusClicked;
            view.Clicked += OnVirusClicked;
            FluMixSfx.Play("virus_spawn");
            var active = new ActiveVirus
            {
                view = view,
                lane = lane,
                cell = 0,
                slide = 1f
            };
            view.Rect.localScale = Vector3.one;
            view.Pulse = !UsesSheet(type);
            alive.Add(active);
            PlaceVirus(active, 0f);
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
            return stage.Project(stage.LanePoint(lane, 0f), out local);
        }

        void PlaceVirus(ActiveVirus virus, float progress)
        {
            if (virus == null || virus.view == null)
                return;

            if (stage != null && stage.Ready)
            {
                stage.PlaceVirus(virus.view, stage.LanePoint(virus.lane, progress));
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

        Sprite SpriteFor(VirusType type)
        {
            switch (type)
            {
                case VirusType.Orange: return orangeSprite;
                case VirusType.Red: return redSprite;
                default: return greenSprite;
            }
        }

        bool UsesSheet(VirusType type)
        {
            return greenUsesSheet && type == VirusType.Green;
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
