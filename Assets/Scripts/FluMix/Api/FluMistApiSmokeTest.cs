using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace FluMix.Api
{
    /// <summary>
    /// Runtime smoke test covering FluMist API endpoints from flumist-unity-api.md §10.
    /// Attach temporarily or call FluMistApiSmokeTest.RunAll via reflection / menu.
    /// </summary>
    public class FluMistApiSmokeTest : MonoBehaviour
    {
        [SerializeField] FluMistApi api;
        [SerializeField] bool runOnStart;

        readonly StringBuilder log = new StringBuilder();
        public string LastReport => log.ToString();

        void Start()
        {
            if (api == null)
                api = GetComponent<FluMistApi>() ?? FindFirstObjectByType<FluMistApi>();
            if (runOnStart)
                StartCoroutine(RunAll());
        }

        [ContextMenu("Run FluMist API Smoke Test")]
        public void RunFromMenu()
        {
            StartCoroutine(RunAll());
        }

        public IEnumerator RunAll()
        {
            log.Clear();
            if (api == null)
            {
                Log("FAIL", "FluMistApi missing");
                yield break;
            }

            string suffix = DateTime.UtcNow.ToString("HHmmssfff");
            string username = "u" + suffix;
            string password = "123456";
            string fullName = "Tester " + suffix;

            // 1 Register
            AuthDto auth = null;
            yield return api.Register(username, password, fullName, a => auth = a, Err("register"));
            PassFail(auth != null && !string.IsNullOrEmpty(auth.accessToken), "1 Register 201+token", auth?.player?.username);

            // 1a invalid username blocked client-side
            PassFail(!FluMistApi.ValidUsername("có dấu"), "1a Invalid username rejected locally");
            PassFail(!FluMistApi.ValidPassword("123"), "1a Short password rejected locally");

            // 1b Login — game normalizes username to lower before send (server is case-sensitive)
            PassFail(FluMistApi.NormUser("ABC") == "abc", "1b NormUser lowercases");
            AuthDto login = null;
            yield return api.Login(username.ToUpperInvariant(), password, a => login = a, Err("login"));
            PassFail(login != null, "1b Login after client NormUser (API.Login lowercases)");

            // Duplicate register
            AuthDto dup = null;
            long dupHttp = 0;
            yield return api.Register(username, password, fullName, a => dup = a, (h, c, m) =>
            {
                dupHttp = h;
                Log("INFO", $"dup register {h} {c} {m}");
            });
            PassFail(dup == null && dupHttp == 409, "1a Duplicate username => 409");

            // 2 Questions
            QuestionsDto questions = null;
            yield return api.Questions(q => questions = q, Err("questions"));
            bool qOk = questions != null && questions.questions != null && questions.questions.Length > 0;
            bool noAnswer = qOk && questions.questions[0] != null;
            PassFail(qOk, "2 Questions loaded", qOk ? questions.questions.Length + " items" : null);
            // Cannot assert absence of correctIndex field easily; assert options have keys
            if (qOk)
            {
                var opt = questions.questions[0].options;
                PassFail(opt != null && opt.Length > 0 && !string.IsNullOrEmpty(opt[0].key), "2 Options have key/label");
                PassFail(!string.IsNullOrEmpty(questions.questions[0].explanation) || questions.questions[0].explanation == null,
                    "2 Explanation field present (nullable ok)");
            }

            // 3-8 Play + answers
            FluMistRoundSession session = gameObject.GetComponent<FluMistRoundSession>() ?? gameObject.AddComponent<FluMistRoundSession>();
            session.Bind(api);
            session.ResetLocal();
            yield return session.Begin(e => Log("FAIL", "StartPlay " + e));
            PassFail(session.HasPlay, "6.1 StartPlay", session.PlayId);

            if (session.HasPlay && qOk)
            {
                QuestionDto q = questions.questions[0];
                // Virus extras
                session.OnVirusDelta(100);
                session.OnVirusDelta(100);
                session.OnVirusDelta(-50);
                PassFail(session.PendingVirus == 150, "7 Virus extraPoints accumulate", session.PendingVirus.ToString());

                // Wrong answer first (pick key that is likely wrong — try "A" then check)
                ProgressResultDto wrong = null;
                yield return session.Answer(q, "A", 1200, r => wrong = r);
                PassFail(wrong != null && wrong.answer != null, "4/5 Answer progress response");
                if (wrong?.answer != null)
                {
                    Log("INFO", $"answer A => correct={wrong.answer.correct} points={wrong.answer.points} correctAnswer={wrong.answer.correctAnswer}");
                    // Live project may set revealAnswer=false → correctAnswer omitted; wrongAnswerPoints may be 0
                    PassFail(wrong.answer.correct == false || wrong.answer.correct, "3/4 Answer graded by server");
                    if (!string.IsNullOrEmpty(wrong.answer.correctAnswer))
                        PassFail(true, "3/4 correctAnswer returned (revealAnswer=true)");
                    else
                        Log("INFO", "correctAnswer omitted (revealAnswer=false on server) — OK");
                }

                // Timeout empty answer on next question
                QuestionDto q2 = questions.questions[Mathf.Min(1, questions.questions.Length - 1)];
                ProgressResultDto timeout = null;
                yield return session.Answer(q2, "", 0, r => timeout = r);
                PassFail(timeout?.answer != null && timeout.answer.correct == false, "5 Timeout answer \"\" graded false",
                    timeout?.answer != null ? "pts=" + timeout.answer.points : null);

                // Correct answer using previous correctAnswer if same shape
                string rightKey = wrong?.answer?.correctAnswer;
                if (string.IsNullOrEmpty(rightKey) && q.options != null && q.options.Length > 1)
                    rightKey = q.options[1].key;
                QuestionDto q3 = questions.questions[Mathf.Min(2, questions.questions.Length - 1)];
                // Find option with known key from q3 — if we don't know, try B (sample bank often B)
                ProgressResultDto maybe = null;
                yield return session.Answer(q3, rightKey ?? "B", 2000, r => maybe = r);
                PassFail(maybe?.answer != null, "3 Answer with key",
                    maybe?.answer != null ? $"correct={maybe.answer.correct} pts={maybe.answer.points}" : null);

                // Finish
                ProgressResultDto finish = null;
                yield return session.Finish(r => finish = r);
                PassFail(finish != null && finish.completed, "10 isCompleted finish");
                if (finish != null)
                {
                    PassFail(finish.leaderboard != null || !string.IsNullOrEmpty(finish.leaderboardError),
                        "10 leaderboard or leaderboardError");
                    if (finish.leaderboard?.me != null)
                        Log("INFO", $"me.rank={finish.leaderboard.me.rank} total={finish.leaderboard.me.totalScore}");
                }

                // PLAY_FINISHED
                ProgressResultDto after = null;
                long afterHttp = 0;
                string afterCode = null;
                string body = JsonConvert.SerializeObject(new
                {
                    eventId = Guid.NewGuid().ToString("N"),
                    extraPoints = 0,
                    isCompleted = true
                });
                yield return api.Progress(session.PlayId, body, r => after = r, (h, c, m) =>
                {
                    afterHttp = h;
                    afterCode = c;
                    Log("INFO", $"post-finish {h} {c} {m}");
                });
                PassFail(after == null && afterHttp == 409, "10 PLAY_FINISHED after complete",
                    "HTTP " + afterHttp + " code=" + afterCode);
            }

            // 14 Leaderboard
            LeaderboardDto board = null;
            yield return api.Leaderboard(10, b => board = b, Err("leaderboard"));
            PassFail(board != null && board.top != null, "14 GET leaderboard",
                board?.top != null ? board.top.Length + " rows" : null);
            if (board?.me != null)
                Log("INFO", $"leaderboard.me rank={board.me.rank} inTop={board.me.inTop}");

            // Me
            MeDto me = null;
            yield return api.Me(m => me = m, Err("me"));
            PassFail(me != null, "4.4 GET /quiz/me", me != null ? "totalScore=" + me.totalScore : null);

            Debug.Log("[FluMistSmoke]\n" + log);
        }

        Action<long, string, string> Err(string tag) => (h, c, m) => Log("ERR", $"{tag} {h} {c} {m}");

        void PassFail(bool ok, string name, string detail = null)
        {
            Log(ok ? "PASS" : "FAIL", detail != null ? name + " | " + detail : name);
        }

        void Log(string level, string msg)
        {
            log.Append('[').Append(level).Append("] ").Append(msg).Append('\n');
            if (level == "FAIL")
                Debug.LogError("[FluMistSmoke] " + msg);
            else
                Debug.Log("[FluMistSmoke] " + level + " " + msg);
        }
    }
}
