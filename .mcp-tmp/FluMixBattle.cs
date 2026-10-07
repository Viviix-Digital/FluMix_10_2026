using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluMix
{
    public class Virus : MonoBehaviour
    {
        public VirusArchetype Archetype;
        public int Tier;
        public float NormalizedX;
        SpriteRenderer _body;
        SpriteRenderer _outline;
        Transform _eyes;
        Transform _mote;
        bool _dead;
        float _bob;

        public void Setup(VirusArchetype archetype, Sprite body, Sprite outline)
        {
            Archetype = archetype;
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(transform, false);
            _body = bodyGo.AddComponent<SpriteRenderer>();
            _body.sprite = body;
            _body.color = archetype.color;
            _body.sortingOrder = 10;
            var outGo = new GameObject("Outline");
            outGo.transform.SetParent(transform, false);
            outGo.transform.localScale = Vector3.one * 1.18f;
            _outline = outGo.AddComponent<SpriteRenderer>();
            _outline.sprite = outline;
            _outline.color = new Color(archetype.color.r, archetype.color.g, archetype.color.b, 0f);
            _outline.sortingOrder = 9;
            _eyes = new GameObject("Eyes").transform;
            _eyes.SetParent(transform, false);
            MakeEye(new Vector3(-0.12f, 0.08f, -0.1f));
            MakeEye(new Vector3(0.12f, 0.08f, -0.1f));
            _mote = new GameObject("Mote").transform;
            _mote.SetParent(transform, false);
            var mote = _mote.gameObject.AddComponent<SpriteRenderer>();
            mote.sprite = body;
            mote.color = Color.white;
            mote.sortingOrder = 11;
            _mote.localScale = Vector3.one * 0.18f;
            _mote.gameObject.SetActive(false);
            transform.localScale = Vector3.zero;
            StartCoroutine(Pop());
        }

        void MakeEye(Vector3 pos)
        {
            var eye = new GameObject("Eye");
            eye.transform.SetParent(_eyes, false);
            eye.transform.localPosition = pos;
            eye.transform.localScale = Vector3.one * 0.16f;
            var r = eye.AddComponent<SpriteRenderer>();
            r.sprite = _body.sprite;
            r.color = new Color(0.05f, 0.08f, 0.15f);
            r.sortingOrder = 12;
        }

        IEnumerator Pop()
        {
            float t = 0f;
            while (t < 0.28f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.28f);
                float s = EaseOutBack(k);
                transform.localScale = Vector3.one * s;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        public void Tick(float dt, float gateX)
        {
            if (_dead || Archetype == null) return;
            float speed = Archetype.baseSpeed;
            if (Tier >= 2) speed *= 1.5f;
            else if (Tier == 1) speed *= 1.25f;
            var rules = FluMixFlow.Instance != null ? FluMixFlow.Instance.Rules : null;
            if (rules != null)
            {
                if (Tier >= 2) speed = Archetype.baseSpeed * rules.tier2Speed;
                else if (Tier == 1) speed = Archetype.baseSpeed * rules.tier1Speed;
            }
            transform.position += Vector3.left * speed * dt;
            _bob += dt * 6f;
            _body.transform.localPosition = new Vector3(0f, Mathf.Sin(_bob) * 0.08f, 0f);
            float blink = Mathf.Repeat(_bob, 3.2f) > 3.05f ? 0.15f : 1f;
            _eyes.localScale = new Vector3(1f, blink, 1f);
            if (Tier >= 2 && _mote != null)
            {
                _mote.gameObject.SetActive(true);
                float a = Time.time * 4f;
                _mote.localPosition = new Vector3(Mathf.Cos(a) * 0.45f, Mathf.Sin(a) * 0.45f, -0.2f);
            }
            float min = -8f;
            float max = 8f;
            NormalizedX = Mathf.InverseLerp(max, min, transform.position.x);
        }

        public bool ReachedGate(float gateX) => !_dead && transform.position.x <= gateX;

        public void Evolve()
        {
            if (Tier >= 2) return;
            Tier++;
            FluMixLog.Info("Combat", "Evolve " + Archetype.guardian + " tier " + Tier);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("virus_evolve");
            StartCoroutine(EvolvePunch());
        }

        IEnumerator EvolvePunch()
        {
            _outline.color = new Color(Archetype.color.r, Archetype.color.g, Archetype.color.b, Tier > 0 ? 0.9f : 0f);
            if (Tier >= 2)
            {
                foreach (Transform eye in _eyes)
                {
                    var r = eye.GetComponent<SpriteRenderer>();
                    if (r != null) r.color = new Color(0.8f, 0.1f, 0.15f);
                }
            }
            float t = 0f;
            Color flash = Color.white;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                float k = t / 0.35f;
                float s = k < 0.4f ? Mathf.Lerp(1f, 1.25f, k / 0.4f) : Mathf.Lerp(1.25f, 1.1f, (k - 0.4f) / 0.6f);
                transform.localScale = Vector3.one * s;
                _body.color = Color.Lerp(flash, Archetype.color, k);
                yield return null;
            }
            _body.color = Archetype.color;
            transform.localScale = Vector3.one * (Tier >= 1 ? 1.1f : 1f);
        }

        public void Die()
        {
            if (_dead) return;
            _dead = true;
            StartCoroutine(Shrink());
        }

        IEnumerator Shrink()
        {
            float t = 0f;
            Vector3 start = transform.localScale;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.25f);
                transform.localScale = Vector3.Lerp(start, Vector3.zero, k);
                var c = _body.color;
                c.a = 1f - k;
                _body.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }

    public class Guardian : MonoBehaviour
    {
        public GuardianId Id;
        public Transform Muzzle;
        Vector3 _base;
        float _recoil;

        void Awake()
        {
            _base = transform.localPosition;
            if (Muzzle == null)
            {
                var m = new GameObject("Muzzle");
                m.transform.SetParent(transform, false);
                m.transform.localPosition = new Vector3(0.7f, 0.2f, 0f);
                Muzzle = m.transform;
            }
        }

        void Update()
        {
            float bob = Mathf.Sin(Time.time * 2.2f + (int)Id) * 0.06f;
            float kick = _recoil > 0f ? -0.18f * (_recoil / 0.35f) : 0f;
            transform.localPosition = _base + new Vector3(kick, bob, 0f);
            if (_recoil > 0f) _recoil -= Time.deltaTime;
        }

        public void Recoil(float duration)
        {
            _recoil = duration;
        }
    }

    public class Beam : MonoBehaviour
    {
        LineRenderer _core;
        LineRenderer _fringe;
        bool _alive;

        public void Build(Color color)
        {
            _fringe = gameObject.AddComponent<LineRenderer>();
            Setup(_fringe, 0.22f, new Color(color.r, color.g, color.b, 0.85f));
            var coreGo = new GameObject("Core");
            coreGo.transform.SetParent(transform, false);
            _core = coreGo.AddComponent<LineRenderer>();
            Setup(_core, 0.07f, Color.white);
        }

        static void Setup(LineRenderer line, float width, Color color)
        {
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width * 0.4f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.numCapVertices = 4;
            line.sortingOrder = 20;
            line.useWorldSpace = true;
        }

        public void Fire(Vector3 from, Vector3 to, Color color, bool correct, float duration)
        {
            if (_fringe == null) Build(color);
            _alive = true;
            StopAllCoroutines();
            StartCoroutine(Run(from, to, color, correct, duration));
        }

        IEnumerator Run(Vector3 from, Vector3 to, Color color, bool correct, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                Vector3 tip = Vector3.Lerp(from, to, Mathf.Clamp01(k * 1.4f));
                float n = 1f + Mathf.Sin(Time.unscaledTime * 6f * Mathf.PI * 2f) * 0.25f;
                _fringe.startWidth = 0.22f * n;
                _core.startWidth = 0.07f * n;
                if (!correct && k > 0.8f)
                {
                    _fringe.startColor = Color.gray;
                    _fringe.endColor = Color.gray;
                }
                _fringe.SetPosition(0, from);
                _fringe.SetPosition(1, tip);
                _core.SetPosition(0, from);
                _core.SetPosition(1, tip);
                yield return null;
            }
            _alive = false;
            _fringe.positionCount = 0;
            _core.positionCount = 0;
            _fringe.positionCount = 2;
            _core.positionCount = 2;
        }

        public bool Alive => _alive;
    }

    public class BattleController : MonoBehaviour
    {
        public Transform VirusRoot;
        public Transform Gate;
        public Guardian[] Guardians;
        public Sprite VirusSprite;
        public Sprite RingSprite;
        public BattleHud Hud;
        public QuizPresenter Quiz;

        readonly List<Virus> _alive = new List<Virus>();
        float _spawnTimer;
        float _interval = 2.2f;
        bool _ended;
        Beam _beam;
        bool _quizOpen;

        public IReadOnlyList<Virus> Alive => _alive;
        public bool QuizOpen => _quizOpen;

        void Start()
        {
            if (FluMixFlow.Instance != null && FluMixFlow.Instance.Rules != null)
                _interval = FluMixFlow.Instance.Rules.spawnIntervalStart;
            var beamGo = new GameObject("Beam");
            _beam = beamGo.AddComponent<Beam>();
            if (!GameSession.RoundActive && FluMixFlow.Instance != null && FluMixFlow.Instance.Rules != null)
                GameSession.ResetRound(FluMixFlow.Instance.Rules.roundSeconds);
            GameSession.RoundActive = true;
            if (Hud != null) Hud.Bind(this);
        }

        void Update()
        {
            if (_ended) return;
            float dt = Time.deltaTime;
            if (!_quizOpen && GameSession.RoundActive)
            {
                GameSession.TimeLeft -= dt;
                if (GameSession.TimeLeft <= 0f)
                {
                    GameSession.TimeLeft = 0f;
                    if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("timer_end");
                    EndRound();
                    return;
                }
                if (GameSession.TimeLeft <= 10f && Mathf.FloorToInt(GameSession.TimeLeft + dt) != Mathf.FloorToInt(GameSession.TimeLeft))
                {
                    if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("timer_tick");
                }
                _spawnTimer += dt;
                if (_spawnTimer >= _interval)
                {
                    _spawnTimer = 0f;
                    SpawnRandom();
                }
            }
            float gateX = Gate != null ? Gate.position.x : -5.2f;
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                var v = _alive[i];
                if (v == null) { _alive.RemoveAt(i); continue; }
                v.Tick(dt, gateX);
                if (v.ReachedGate(gateX)) Leak(v);
            }
            if (Hud != null) Hud.Refresh();
        }

        public void SelectGuardian(int index)
        {
            GameSession.SelectedGuardian = index;
            FluMixLog.Info("Combat", "Select " + (GuardianId)index);
        }

        public void ConfirmShot()
        {
            if (_quizOpen || _ended) return;
            if (GameSession.SelectedGuardian < 0)
            {
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_deny");
                return;
            }
            var virus = Front();
            Fire((GuardianId)GameSession.SelectedGuardian, virus, false);
        }

        public void SpawnRandom()
        {
            var flow = FluMixFlow.Instance;
            if (flow == null) return;
            var list = flow.Archetypes;
            var pick = list[Random.Range(0, list.Length)];
            Spawn(pick, false);
        }

        public Virus Spawn(VirusArchetype archetype, bool force)
        {
            if (archetype == null) return null;
            var go = new GameObject("Virus_" + archetype.guardian);
            go.transform.SetParent(VirusRoot, false);
            go.transform.position = new Vector3(7.4f, Random.Range(-1.4f, 1.1f), 0f);
            var virus = go.AddComponent<Virus>();
            virus.Setup(archetype, VirusSprite, RingSprite);
            _alive.Add(virus);
            GameSession.SpawnCount++;
            FluMixLog.Info("Combat", "Spawn " + archetype.guardian);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("virus_spawn");
            var rules = FluMixFlow.Instance.Rules;
            _interval = Mathf.Max(rules.spawnIntervalMin, _interval - rules.spawnIntervalStep);
            if (!force && rules.quizEveryNthSpawn > 0 && GameSession.SpawnCount % rules.quizEveryNthSpawn == 0)
                OpenQuiz(false);
            return virus;
        }

        public void SpawnMatch()
        {
            int sel = GameSession.SelectedGuardian < 0 ? 0 : GameSession.SelectedGuardian;
            Spawn(FluMixFlow.Instance.ArchetypeFor((GuardianId)sel), true);
        }

        public void SpawnMismatchAndEvolve()
        {
            int sel = GameSession.SelectedGuardian < 0 ? 0 : GameSession.SelectedGuardian;
            int other = (sel + 1) % 3;
            var v = Spawn(FluMixFlow.Instance.ArchetypeFor((GuardianId)other), true);
            if (v != null) v.Evolve();
        }

        void Fire(GuardianId id, Virus virus, bool fromQuiz)
        {
            var guardian = Guardians[(int)id];
            Vector3 from = guardian.Muzzle != null ? guardian.Muzzle.position : guardian.transform.position;
            Vector3 to = virus != null ? virus.transform.position : from + Vector3.right * 4f;
            var color = FluMixFlow.Instance.ArchetypeFor(id).color;
            bool match = virus != null && virus.Archetype.guardian == id;
            string cue = id == GuardianId.G ? "beam_green" : id == GuardianId.A ? "beam_blue" : "beam_pink";
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play(cue);
            guardian.Recoil(0.35f);
            float dur = FluMixFlow.Instance.Rules != null ? FluMixFlow.Instance.Rules.beamDuration : 0.35f;
            _beam.Fire(from, to, color, match, dur);
            if (virus == null) return;
            if (match)
            {
                AddScore(FluMixFlow.Instance.Rules.scoreHit, "hit");
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("impact_correct");
                SpawnBurst(virus.transform.position, color, true);
                virus.Die();
                _alive.Remove(virus);
            }
            else
            {
                AddScore(FluMixFlow.Instance.Rules.scoreMiss, "miss");
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("impact_wrong");
                SpawnBurst(virus.transform.position, Color.gray, false);
                virus.Evolve();
            }
        }

        void Leak(Virus virus)
        {
            int penalty = virus.Tier >= 2 ? 2 : 1;
            GameSession.Leaks += penalty;
            int delta = FluMixFlow.Instance.Rules.scoreLeak * penalty;
            AddScore(delta, "leak");
            FluMixLog.Info("Combat", "Leak tier " + virus.Tier);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("virus_leak");
            _alive.Remove(virus);
            Destroy(virus.gameObject);
        }

        public void AddScore(int delta, string reason)
        {
            GameSession.Score += delta;
            GameSession.LastScoreDelta = delta;
            FluMixLog.Info("Score", reason + " " + delta + " => " + GameSession.Score);
            if (Hud != null) Hud.PunchScore(delta);
            CheckWarning();
        }

        void CheckWarning()
        {
            var rules = FluMixFlow.Instance.Rules;
            if (GameSession.NextWarningIndex >= rules.warningThresholds.Length) return;
            int threshold = rules.warningThresholds[GameSession.NextWarningIndex];
            if (GameSession.Score < threshold) return;
            GameSession.NextWarningIndex++;
            if (GameSession.SuppressNextWarning && Time.unscaledTime < GameSession.SuppressUntil)
            {
                GameSession.SuppressNextWarning = false;
                FluMixLog.Info("Quiz", "Warning delayed at " + threshold);
                return;
            }
            GameSession.SuppressNextWarning = false;
            if (Hud != null) Hud.ShowWarning();
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("warning_alarm");
            OpenQuiz(true);
        }

        public void OpenQuiz(bool fromWarning)
        {
            if (_quizOpen || Quiz == null) return;
            if (GameSession.ResolvedQuestions >= FluMixFlow.Instance.Rules.maxQuestions)
            {
                EndRound();
                return;
            }
            _quizOpen = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            FluMixLog.Info("Quiz", "Open index " + GameSession.QuestionIndex);
            Quiz.ShowQuestion(FluMixFlow.Instance.Questions.Get(GameSession.QuestionIndex));
        }

        public void ResolveQuiz(int selected)
        {
            var q = FluMixFlow.Instance.Questions.Get(GameSession.QuestionIndex);
            bool correct = selected == q.correctIndex;
            GameSession.SelectedOption = selected;
            GameSession.QuestionIndex++;
            GameSession.ResolvedQuestions++;
            if (correct)
            {
                AddScore(FluMixFlow.Instance.Rules.scoreQuiz, "quiz");
                GameSession.SuppressNextWarning = true;
                GameSession.SuppressUntil = Time.unscaledTime + FluMixFlow.Instance.Rules.warningDelaySeconds;
                var front = Front();
                if (front != null) Fire(front.Archetype.guardian, front, true);
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("quiz_correct");
                Quiz.ShowCorrect(q);
            }
            else
            {
                AddScore(FluMixFlow.Instance.Rules.scoreMiss, "quiz-miss");
                var front = Front();
                if (front != null) front.Evolve();
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("quiz_wrong");
                Quiz.ShowWrong(q, selected);
            }
            if (GameSession.ResolvedQuestions >= FluMixFlow.Instance.Rules.maxQuestions)
                StartCoroutine(EndAfterQuiz());
        }

        public void CloseQuiz()
        {
            _quizOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            FluMixLog.Info("Quiz", "Close");
        }

        IEnumerator EndAfterQuiz()
        {
            yield return new WaitForSecondsRealtime(FluMixFlow.Instance.Rules.explainAutoContinue + 0.2f);
            EndRound();
        }

        public void EndRound()
        {
            if (_ended) return;
            _ended = true;
            GameSession.RoundActive = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            FluMixFlow.Instance.State = FlowState.Result;
            FluMixLog.Info("Flow", "Result");
            string name = string.IsNullOrEmpty(GameSession.Account) ? "NGƯỜI CHƠI" : GameSession.Account;
            LeaderboardStore.Submit(name, GameSession.Score);
            if (Hud != null) Hud.ShowResult();
        }

        public Virus Front()
        {
            Virus best = null;
            float x = float.MaxValue;
            for (int i = 0; i < _alive.Count; i++)
            {
                if (_alive[i] == null) continue;
                if (_alive[i].transform.position.x < x)
                {
                    x = _alive[i].transform.position.x;
                    best = _alive[i];
                }
            }
            return best;
        }

        void SpawnBurst(Vector3 pos, Color color, bool star)
        {
            var go = new GameObject(star ? "Hit" : "Fizzle");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;
            main.startLifetime = 0.35f;
            main.startSpeed = star ? 3.5f : 1.5f;
            main.startSize = star ? 0.25f : 0.15f;
            main.maxParticles = 24;
            main.duration = 0.2f;
            main.loop = false;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(star ? 16 : 8)) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            ps.Play();
            Destroy(go, 1f);
        }

        public void SetTimer(float seconds)
        {
            GameSession.TimeLeft = seconds;
        }
    }
}
