using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluMix
{
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
            var guardian = Guardians != null && (int)id < Guardians.Length ? Guardians[(int)id] : null;
            if (guardian == null) return;
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
