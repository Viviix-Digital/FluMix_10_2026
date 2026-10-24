using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;

namespace FluMix.Api
{
    public class FluMistRoundSession : MonoBehaviour
    {
        FluMistApi api;
        string playId;
        int virus;
        string pendingBody;
        int pendingVirus;
        string lastError;
        string lastErrorCode;
        long lastHttp;
        ProgressResultDto lastFinish;

        public bool HasPlay => !string.IsNullOrEmpty(playId);
        public string PlayId => playId;
        public int PendingVirus => virus;
        public bool HasPendingProgress => pendingBody != null;
        public string LastError => lastError;
        public string LastErrorCode => lastErrorCode;
        public long LastHttp => lastHttp;
        public ProgressResultDto LastFinish => lastFinish;
        public int HudScore { get; private set; }

        public void Bind(FluMistApi fluMistApi)
        {
            api = fluMistApi;
        }

        public void ResetLocal()
        {
            playId = null;
            virus = 0;
            pendingBody = null;
            pendingVirus = 0;
            lastError = null;
            lastErrorCode = null;
            lastHttp = 0;
            lastFinish = null;
            HudScore = 0;
        }

        public void OnVirusDelta(int delta)
        {
            virus += delta;
            HudScore += delta;
        }

        public IEnumerator Begin(Action<string> onError)
        {
            lastError = null;
            lastErrorCode = null;
            lastHttp = 0;
            lastFinish = null;

            if (api == null)
            {
                onError?.Invoke("API chưa sẵn sàng");
                yield break;
            }

            StartPlayDto start = null;
            string err = null;
            yield return api.StartPlay(
                Guid.NewGuid().ToString("N"),
                s => start = s,
                (h, c, m) =>
                {
                    lastHttp = h;
                    lastErrorCode = c;
                    err = m;
                });

            if (start == null || start.play == null)
            {
                lastError = err ?? "Không bắt đầu được ván";
                onError?.Invoke(lastError);
                yield break;
            }

            playId = start.play.playId;
            HudScore = start.play.score;
            virus = 0;
            pendingBody = null;
            pendingVirus = 0;
        }

        public IEnumerator Answer(QuestionDto question, string key, int timeMs, Action<ProgressResultDto> onResult)
        {
            yield return Flush(null);
            if (pendingBody != null)
            {
                lastError = "Mất kết nối server";
                yield break;
            }

            if (question == null || string.IsNullOrEmpty(playId))
            {
                lastError = "Thiếu câu hỏi hoặc playId";
                yield break;
            }

            Queue(new
            {
                eventId = NewEventId(),
                answer = new { codeId = question.codeId, answer = key ?? string.Empty, timeMs },
                extraPoints = virus
            });
            yield return Flush(onResult);
        }

        public IEnumerator Finish(Action<ProgressResultDto> onResult)
        {
            yield return Flush(null);
            if (pendingBody != null)
            {
                lastError = "Mất kết nối server";
                yield break;
            }

            if (string.IsNullOrEmpty(playId))
            {
                lastError = "Chưa có ván đang chơi";
                yield break;
            }

            Queue(new
            {
                eventId = NewEventId(),
                extraPoints = virus,
                isCompleted = true
            });
            yield return Flush(res =>
            {
                lastFinish = res;
                onResult?.Invoke(res);
            });
        }

        public IEnumerator RetryPending(Action<ProgressResultDto> onResult)
        {
            yield return Flush(onResult);
        }

        static string NewEventId() => Guid.NewGuid().ToString("N");

        void Queue(object body)
        {
            pendingBody = JsonConvert.SerializeObject(body);
            pendingVirus = virus;
        }

        IEnumerator Flush(Action<ProgressResultDto> onResult)
        {
            if (pendingBody == null || api == null || string.IsNullOrEmpty(playId))
                yield break;

            ProgressResultDto res = null;
            long http = 0;
            string code = null;
            string message = null;
            yield return api.Progress(playId, pendingBody,
                r => res = r,
                (h, c, m) =>
                {
                    http = h;
                    code = c;
                    message = m;
                });

            lastHttp = http;
            lastErrorCode = code;
            lastError = message;

            if (res != null)
            {
                virus -= pendingVirus;
                if (res.play != null)
                    HudScore = res.play.score + virus;
                pendingBody = null;
                pendingVirus = 0;
                lastError = null;
                onResult?.Invoke(res);
                yield break;
            }

            if (http == 0 || http >= 500)
                yield break;

            // 4xx: drop pending (except keep virus for next send)
            pendingBody = null;
            pendingVirus = 0;
            Debug.LogWarning($"[FluMist] progress {http} {code} {message}");
        }
    }
}
