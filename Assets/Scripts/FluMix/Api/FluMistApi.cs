using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace FluMix.Api
{
    public class FluMistApi : MonoBehaviour
    {
        [SerializeField] string baseUrl = "https://api.sukientuongtac.com/api";
        [SerializeField] string projectSlug = "flumist-2026";

        public string BaseUrl => baseUrl;
        public string ProjectSlug => projectSlug;
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public PlayerDto Player { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

        public event Action<string> LoggedOut;

        /// <summary>Khi true, ClearSession không fire LoggedOut (dùng lúc boot restore).</summary>
        public bool MuteLoggedOut { get; set; }

        const int TimeoutSec = 10;
        const int MaxAttempts = 3;
        static readonly Regex UsernameRegex = new Regex("^[a-z0-9._@+-]{3,64}$", RegexOptions.Compiled);

        void Awake()
        {
            AccessToken = TokenStore.AccessToken;
            RefreshToken = TokenStore.RefreshToken;
            if (!string.IsNullOrEmpty(TokenStore.Username))
            {
                Player = new PlayerDto
                {
                    uuid = TokenStore.Uuid,
                    username = TokenStore.Username,
                    fullName = TokenStore.FullName,
                    projectSlug = projectSlug
                };
            }
        }

        public static string NormUser(string username) => (username ?? string.Empty).Trim().ToLowerInvariant();

        public static bool ValidUsername(string username) => UsernameRegex.IsMatch(NormUser(username));

        public static bool ValidPassword(string password) =>
            !string.IsNullOrEmpty(password) && password.Length >= 6 && password.Length <= 128;

        public IEnumerator Register(string username, string password, string fullName, Action<AuthDto> onOk, Action<long, string, string> onError)
        {
            var body = JsonConvert.SerializeObject(new
            {
                projectSlug,
                username = NormUser(username),
                password,
                fullName = fullName?.Trim()
            });
            return Send<AuthDto>("POST", "/quiz/player/register", body, false, a =>
            {
                SaveTokens(a);
                onOk?.Invoke(a);
            }, onError);
        }

        public IEnumerator Login(string username, string password, Action<AuthDto> onOk, Action<long, string, string> onError)
        {
            var body = JsonConvert.SerializeObject(new
            {
                projectSlug,
                username = NormUser(username),
                password
            });
            return Send<AuthDto>("POST", "/quiz/player/login", body, false, a =>
            {
                SaveTokens(a);
                onOk?.Invoke(a);
            }, onError);
        }

        public IEnumerator Questions(Action<QuestionsDto> onOk, Action<long, string, string> onError) =>
            Send("GET", $"/quiz/projects/{projectSlug}/questions", null, true, onOk, onError);

        public IEnumerator StartPlay(string clientPlayId, Action<StartPlayDto> onOk, Action<long, string, string> onError) =>
            Send("POST", "/quiz/plays", JsonConvert.SerializeObject(new { clientPlayId }), true, onOk, onError);

        public IEnumerator Progress(string playId, string body, Action<ProgressResultDto> onOk, Action<long, string, string> onError) =>
            Send("POST", $"/quiz/plays/{playId}/progress", body, true, onOk, onError);

        public IEnumerator Leaderboard(int limit, Action<LeaderboardDto> onOk, Action<long, string, string> onError) =>
            Send("GET", $"/quiz/projects/{projectSlug}/leaderboard?limit={limit}", null, true, onOk, onError);

        public IEnumerator Me(Action<MeDto> onOk, Action<long, string, string> onError) =>
            Send("GET", "/quiz/me", null, true, onOk, onError);

        /// <summary>
        /// Boot auto-login: GET /me (401 thường → refresh 1 lần trong Send).
        /// false ⇒ phải hiện màn Login (hết phiên / SESSION_REPLACED / mất mạng).
        /// </summary>
        public IEnumerator TryRestoreSession(Action<MeDto> onOk, Action<string> onNeedLogin)
        {
            if (!TokenStore.HasSession && string.IsNullOrEmpty(AccessToken) && string.IsNullOrEmpty(RefreshToken))
            {
                onNeedLogin?.Invoke(null);
                yield break;
            }

            // Đảm bảo memory có token từ PlayerPrefs
            if (string.IsNullOrEmpty(AccessToken))
                AccessToken = TokenStore.AccessToken;
            if (string.IsNullOrEmpty(RefreshToken))
                RefreshToken = TokenStore.RefreshToken;

            MuteLoggedOut = true;
            MeDto me = null;
            long http = 0;
            string code = null;
            string message = null;
            yield return Me(
                m => me = m,
                (h, c, m) => { http = h; code = c; message = m; });
            MuteLoggedOut = false;

            if (me != null && me.player != null)
            {
                Player = me.player;
                TokenStore.Username = me.player.username;
                TokenStore.FullName = string.IsNullOrEmpty(me.player.fullName) ? me.player.username : me.player.fullName;
                TokenStore.Uuid = me.player.uuid;
                PlayerPrefs.Save();
                onOk?.Invoke(me);
                yield break;
            }

            string reason;
            if (code == "SESSION_REPLACED")
                reason = "Tài khoản đã đăng nhập ở thiết bị khác. Vui lòng đăng nhập lại.";
            else if (http == 0)
                reason = "Mất kết nối server. Vui lòng thử lại sau.";
            else
                reason = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";

            ClearTokensOnly();
            onNeedLogin?.Invoke(reason);
        }

        public void ClearSession(string reason)
        {
            ClearTokensOnly();
            if (!MuteLoggedOut)
                LoggedOut?.Invoke(reason ?? "Đã đăng xuất");
        }

        void ClearTokensOnly()
        {
            AccessToken = null;
            RefreshToken = null;
            Player = null;
            TokenStore.Clear();
        }

        void SaveTokens(AuthDto auth)
        {
            if (auth == null)
                return;
            AccessToken = auth.accessToken;
            RefreshToken = auth.refreshToken;
            Player = auth.player;
            TokenStore.SaveAuth(auth);
        }

        UnityWebRequest Build(string method, string path, string body, bool auth)
        {
            var req = new UnityWebRequest(baseUrl + path, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = TimeoutSec
            };
            if (body != null)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.SetRequestHeader("Content-Type", "application/json");
            }

            if (auth && !string.IsNullOrEmpty(AccessToken))
                req.SetRequestHeader("Authorization", "Bearer " + AccessToken);
            return req;
        }

        static ApiResponse<T> Parse<T>(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<ApiResponse<T>>(text);
            }
            catch
            {
                try
                {
                    var root = JObject.Parse(text);
                    return new ApiResponse<T>
                    {
                        statusCode = root.Value<int?>("statusCode") ?? 0,
                        message = root.Value<string>("message"),
                        code = root.Value<string>("code"),
                        data = default
                    };
                }
                catch
                {
                    return null;
                }
            }
        }

        IEnumerator Refresh(Action<bool> done)
        {
            if (string.IsNullOrEmpty(RefreshToken))
            {
                done(false);
                yield break;
            }

            using (var req = Build("POST", "/quiz/player/refresh", JsonConvert.SerializeObject(new { refreshToken = RefreshToken }), false))
            {
                yield return req.SendWebRequest();
                var res = Parse<AuthDto>(req.downloadHandler != null ? req.downloadHandler.text : null);
                if (req.responseCode == 200 && res?.data != null)
                {
                    SaveTokens(res.data);
                    done(true);
                }
                else
                {
                    done(false);
                }
            }
        }

        IEnumerator Send<T>(string method, string path, string body, bool auth, Action<T> onOk, Action<long, string, string> onError)
        {
            bool refreshed = false;
            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                using (var req = Build(method, path, body, auth))
                {
                    yield return req.SendWebRequest();

                    bool networkFail = req.result == UnityWebRequest.Result.ConnectionError
                        || req.result == UnityWebRequest.Result.DataProcessingError
                        || (req.responseCode == 0 && req.result != UnityWebRequest.Result.Success);

                    if (networkFail)
                    {
                        if (attempt < MaxAttempts)
                        {
                            yield return new WaitForSecondsRealtime(0.5f * attempt);
                            continue;
                        }

                        onError?.Invoke(0, null, "Mất kết nối server");
                        yield break;
                    }

                    string text = req.downloadHandler != null ? req.downloadHandler.text : null;
                    var res = Parse<T>(text);
                    long http = req.responseCode;

                    if (http >= 200 && http < 300)
                    {
                        if (res != null)
                        {
                            onOk?.Invoke(res.data);
                            yield break;
                        }

                        onError?.Invoke(http, null, "Phản hồi server không hợp lệ");
                        yield break;
                    }

                    if (http == 401 && auth)
                    {
                        if (res != null && res.code == "SESSION_REPLACED")
                        {
                            ClearSession("Tài khoản đã đăng nhập ở thiết bị khác");
                            onError?.Invoke(401, res.code, res.message);
                            yield break;
                        }

                        if (!refreshed)
                        {
                            refreshed = true;
                            bool ok = false;
                            yield return Refresh(r => ok = r);
                            if (ok)
                            {
                                attempt--;
                                continue;
                            }
                        }

                        ClearSession("Phiên đăng nhập hết hạn");
                        onError?.Invoke(401, res?.code, res?.message ?? "Phiên đăng nhập hết hạn");
                        yield break;
                    }

                    if (http >= 500 && attempt < MaxAttempts)
                    {
                        yield return new WaitForSecondsRealtime(0.5f * attempt);
                        continue;
                    }

                    string message = res?.message;
                    string errCode = res?.code;
                    if (string.IsNullOrEmpty(errCode) && http == 409)
                    {
                        if (!string.IsNullOrEmpty(text) && text.IndexOf("PLAY_FINISHED", StringComparison.OrdinalIgnoreCase) >= 0)
                            errCode = "PLAY_FINISHED";
                        else if (!string.IsNullOrEmpty(text) && text.IndexOf("EVENT_CONFLICT", StringComparison.OrdinalIgnoreCase) >= 0)
                            errCode = "EVENT_CONFLICT";
                        else if (path != null && path.IndexOf("/progress", StringComparison.OrdinalIgnoreCase) >= 0)
                            errCode = "PLAY_FINISHED";
                    }

                    if (string.IsNullOrEmpty(message))
                        message = $"Lỗi HTTP {http}";
                    onError?.Invoke(http, errCode, message);
                    yield break;
                }
            }
        }
    }
}
