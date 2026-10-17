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
            public float progress;
            public int lane;
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

            float step = Time.deltaTime / Mathf.Max(0.01f, config.virusMoveSeconds);
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                ActiveVirus virus = alive[i];
                if (virus.dying || virus.view == null)
                    continue;

                virus.progress += step;
                PlaceVirus(virus);
                if (virus.progress >= 1f)
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
            virus.progress = 1f;
            if (!virus.dying)
                StartCoroutine(ResolveWall(virus));
        }

        public void FillIfNeeded()
        {
            if (running && !paused)
                EnsurePopulation();
        }

        void OnHeroClicked(HeroView hero)
        {
            if (!running || paused || inputLocked || hero == null || alive.Count == 0)
                return;

            ActiveVirus virus = FrontVirus();
            if (virus == null || virus.dying)
                return;

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

            Vector2 from = fx.ArenaPoint(hero.Muzzle);
            Vector2 to = virus.view.Rect.anchoredPosition;
            int delta = correct ? config.correctHeroScore : config.wrongHeroScore;
            score.Add(delta);

            yield return fx.PlayBeam(hero.Id, from, to, correct);
            fx.PlayScorePopup(delta, to);

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
                fx.ShowWrongMark(MissPoint(from, to));
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
                SpawnOne(type);
            }
        }

        void SpawnOne(VirusType type)
        {
            if (virusPrefab == null || arena == null)
                return;

            VirusView view = Instantiate(virusPrefab, arena);
            view.gameObject.SetActive(true);
            view.Bind(config);
            if (view.Sheet != null)
                view.Sheet.AdoptConfig(config);
            view.Apply(type, SpriteFor(type), UsesSheet(type));
            int lane = alive.Count;
            int target = Mathf.Max(1, config != null ? config.simultaneousViruses : 1);
            var active = new ActiveVirus
            {
                view = view,
                lane = lane,
                progress = target > 1 ? Mathf.Repeat(lane * 0.18f, 0.42f) : 0f
            };
            view.Rect.localScale = Vector3.one;
            view.Pulse = !UsesSheet(type);
            alive.Add(active);
            PlaceVirus(active);
        }

        void PlaceVirus(ActiveVirus virus)
        {
            if (virus == null || virus.view == null)
                return;

            if (stage != null && stage.Ready)
            {
                stage.PlaceVirus(virus.view, stage.LanePoint(virus.lane, virus.progress));
                return;
            }

            virus.view.Rect.anchoredPosition = Vector2.Lerp(Point(spawnPoint, virus.lane), Point(wallPoint, virus.lane), Mathf.Clamp01(virus.progress));
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
                if (best == null || alive[i].progress > best.progress)
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

        Vector2 MissPoint(Vector2 from, Vector2 to)
        {
            Vector2 direction = to - from;
            float distance = direction.magnitude;
            if (distance < 1f)
                return to;
            float travel = Mathf.Max(config.beamThickness, distance - config.wrongBeamGap);
            return from + direction.normalized * travel;
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
