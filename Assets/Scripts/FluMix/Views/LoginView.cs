using System.Collections;
using FluMix.Api;
using FluMix.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public class LoginView : MonoBehaviour
    {
        [Header("Roots (panel có sẵn trong scene)")]
        [SerializeField] GameObject loginRoot;
        [SerializeField] GameObject signUpRoot;

        [Header("Shared")]
        [SerializeField] GameFlow flow;
        [SerializeField] AppServices services;

        [Header("Login panel")]
        [SerializeField] TMP_InputField loginUsername;
        [SerializeField] TMP_InputField loginPassword;
        [SerializeField] Button loginStartButton;
        [SerializeField] Button loginEyeButton;
        [SerializeField] Image loginEyeImage;
        [SerializeField] Button goToSignUpButton;
        [SerializeField] TMP_Text loginStatus;
        [SerializeField] RectTransform loginShakeTarget;

        [Header("SignUp panel")]
        [SerializeField] TMP_InputField signUpUsername;
        [SerializeField] TMP_InputField signUpPassword;
        [SerializeField] TMP_InputField signUpPasswordAgain;
        [SerializeField] Button signUpSubmitButton;
        [SerializeField] Button signUpEyeButton;
        [SerializeField] Image signUpEyeImage;
        [SerializeField] Button signUpEyeButtonAgain;
        [SerializeField] Image signUpEyeImageAgain;
        [SerializeField] Button goToLoginButton;
        [SerializeField] TMP_Text signUpStatus;
        [SerializeField] RectTransform signUpShakeTarget;

        [Header("Eye sprites")]
        [SerializeField] Sprite eyeOpen;
        [SerializeField] Sprite eyeClosed;

        [Header("In-game keyboard")]
        [SerializeField] InGameKeyboard keyboard;

        static readonly Color LinkWhite = new Color(1f, 1f, 1f, 0.92f);
        static readonly Color StatusYellow = new Color(1f, 0.92f, 0.4f, 1f);

        bool loginPasswordHidden = true;
        bool signUpPasswordHidden = true;
        bool busy;
        bool fieldsHooked;
        Vector2 loginShakeBase;
        Vector2 signUpShakeBase;

        public void Bind(
            GameFlow gameFlow,
            AppServices appServices,
            TMP_InputField userField,
            TMP_InputField passwordField,
            Button start,
            Button eye,
            Image eyeIcon,
            Sprite open,
            Sprite closed,
            Button forgot,
            TMP_Text message,
            RectTransform panel)
        {
            flow = gameFlow;
            services = appServices;
            loginUsername = userField;
            loginPassword = passwordField;
            loginStartButton = start;
            loginEyeButton = eye;
            loginEyeImage = eyeIcon;
            eyeOpen = open;
            eyeClosed = closed;
            // forgot = chữ nhỏ "đăng ký"
            goToSignUpButton = forgot;
            loginStatus = message;
            loginShakeTarget = panel;
            ResolvePanels();
            Wire();
        }

        void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.mobileKeyboardSupport = false;
#endif
            ResolvePanels();
            if (loginShakeTarget != null)
                loginShakeBase = loginShakeTarget.anchoredPosition;
            if (signUpShakeTarget != null)
                signUpShakeBase = signUpShakeTarget.anchoredPosition;
            EnsureKeyboard();
            Wire();
        }

        void OnEnable()
        {
            busy = false;
        }

        void OnDisable()
        {
            if (keyboard != null)
                keyboard.Hide();
        }

        void LateUpdate()
        {
            if (keyboard == null || !keyboard.IsVisible)
                return;

            bool onLogin = loginRoot != null && loginRoot.activeInHierarchy;
            bool onSignUp = signUpRoot != null && signUpRoot.activeInHierarchy;
            if (!onLogin && !onSignUp)
                keyboard.Hide();
        }

        void ResolvePanels()
        {
            if (loginRoot == null)
            {
                var t = FindNamed("1. Home Login");
                if (t != null)
                    loginRoot = t.gameObject;
            }

            if (signUpRoot == null)
            {
                var t = FindNamed("1. Home SignUp");
                if (t != null)
                    signUpRoot = t.gameObject;
            }

            if (loginRoot != null)
            {
                if (loginUsername == null)
                    loginUsername = FindInput(loginRoot.transform, "Login Panel/User Name");
                if (loginPassword == null)
                    loginPassword = FindInput(loginRoot.transform, "Login Panel/Password");
                if (loginStartButton == null)
                {
                    var start = loginRoot.transform.Find("StartButton");
                    if (start != null)
                        loginStartButton = start.GetComponent<Button>();
                }

                if (loginEyeButton == null)
                {
                    var eye = loginRoot.transform.Find("Login Panel/Password/PreviewPassWord");
                    if (eye != null)
                    {
                        loginEyeButton = eye.GetComponent<Button>();
                        loginEyeImage = eye.GetComponent<Image>();
                    }
                }

                // Ẩn nút SignupButton to (nếu còn) — dùng chữ nhỏ ForgotPassword
                var bigSignUp = loginRoot.transform.Find("Login Panel/SignupButton");
                if (bigSignUp != null)
                    bigSignUp.gameObject.SetActive(false);

                if (goToSignUpButton == null)
                {
                    var link = loginRoot.transform.Find("Login Panel/ForgotPassword");
                    if (link != null)
                        goToSignUpButton = link.GetComponent<Button>();
                }

                if (loginStatus == null)
                {
                    var msg = loginRoot.transform.Find("Login Panel/ForgotMessage");
                    if (msg != null)
                        loginStatus = msg.GetComponent<TMP_Text>();
                }

                if (loginShakeTarget == null)
                {
                    var panel = loginRoot.transform.Find("Login Panel");
                    if (panel != null)
                        loginShakeTarget = panel as RectTransform;
                }
            }

            if (signUpRoot != null)
            {
                if (signUpUsername == null)
                    signUpUsername = FindInput(signUpRoot.transform, "SignUp Panel/User Name");
                if (signUpPassword == null)
                    signUpPassword = FindInput(signUpRoot.transform, "SignUp Panel/Password");
                if (signUpPasswordAgain == null)
                    signUpPasswordAgain = FindInput(signUpRoot.transform, "SignUp Panel/Password Again");
                if (signUpSubmitButton == null)
                {
                    var btn = signUpRoot.transform.Find("SignUp Panel/SignupButton (1)");
                    if (btn == null)
                        btn = signUpRoot.transform.Find("SignUp Panel/SignupButton");
                    if (btn != null)
                        signUpSubmitButton = btn.GetComponent<Button>();
                }

                if (signUpEyeButton == null)
                {
                    var eye = signUpRoot.transform.Find("SignUp Panel/Password/PreviewPassWord");
                    if (eye != null)
                    {
                        signUpEyeButton = eye.GetComponent<Button>();
                        signUpEyeImage = eye.GetComponent<Image>();
                    }
                }

                if (signUpEyeButtonAgain == null)
                {
                    var eyeAgain = signUpRoot.transform.Find("SignUp Panel/Password Again/PreviewPassWord");
                    if (eyeAgain != null)
                    {
                        signUpEyeButtonAgain = eyeAgain.GetComponent<Button>();
                        signUpEyeImageAgain = eyeAgain.GetComponent<Image>();
                    }
                }

                if (signUpStatus == null)
                {
                    var msg = signUpRoot.transform.Find("SignUp Panel/ForgotMessage");
                    if (msg != null)
                        signUpStatus = msg.GetComponent<TMP_Text>();
                }

                if (signUpShakeTarget == null)
                {
                    var panel = signUpRoot.transform.Find("SignUp Panel");
                    if (panel != null)
                        signUpShakeTarget = panel as RectTransform;
                }

                // Link chữ nhỏ về Login — giống ForgotPassword ở màn đăng nhập.
                if (goToLoginButton == null)
                    goToLoginButton = EnsureGoToLoginLink(signUpRoot.transform);
            }
        }

        static Button EnsureGoToLoginLink(Transform signUpRoot)
        {
            Transform panel = signUpRoot.Find("SignUp Panel");
            if (panel == null)
                return null;

            // Dùng object đã setup trong scene — không đổi RectTransform / vị trí.
            Transform link = panel.Find("GoToLogin");
            if (link == null)
                link = panel.Find("ForgotPassword");
            if (link == null)
                return null;

            var image = link.GetComponent<Image>();
            if (image != null)
            {
                Color c = image.color;
                c.a = 0f;
                image.color = c;
                image.raycastTarget = true;
            }

            var button = link.GetComponent<Button>() ?? link.gameObject.AddComponent<Button>();
            if (image != null)
                button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            link.gameObject.SetActive(true);
            return button;
        }

        static Transform FindNamed(string name)
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == name)
                    return all[i];
            }

            return null;
        }

        static TMP_InputField FindInput(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<TMP_InputField>() : null;
        }

        void StyleAsWhiteLink(Button button, string label)
        {
            if (button == null)
                return;

            button.gameObject.SetActive(true);
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                // Trong suốt — chỉ còn chữ
                Color c = image.color;
                c.a = 0f;
                image.color = c;
            }

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
                text.color = LinkWhite;
                if (text.fontSize > 24f)
                    text.fontSize = 22f;
            }
        }

        void Wire()
        {
            if (loginStartButton != null)
            {
                loginStartButton.onClick.RemoveListener(SubmitLogin);
                loginStartButton.onClick.AddListener(SubmitLogin);
            }

            if (signUpSubmitButton != null)
            {
                signUpSubmitButton.onClick.RemoveListener(SubmitRegister);
                signUpSubmitButton.onClick.AddListener(SubmitRegister);
                signUpSubmitButton.gameObject.SetActive(true);
            }

            StyleAsWhiteLink(goToSignUpButton, "Chưa có tài khoản? Đăng ký");
            if (goToSignUpButton != null)
            {
                goToSignUpButton.onClick.RemoveListener(OpenSignUp);
                goToSignUpButton.onClick.AddListener(OpenSignUp);
            }

            StyleAsWhiteLink(goToLoginButton, "Đã có tài khoản? Đăng nhập");
            if (goToLoginButton != null)
            {
                goToLoginButton.onClick.RemoveListener(OpenLogin);
                goToLoginButton.onClick.AddListener(OpenLogin);
            }

            if (loginEyeButton != null)
            {
                loginEyeButton.onClick.RemoveListener(ToggleLoginPassword);
                loginEyeButton.onClick.AddListener(ToggleLoginPassword);
            }

            if (signUpEyeButton != null)
            {
                signUpEyeButton.onClick.RemoveListener(ToggleSignUpPassword);
                signUpEyeButton.onClick.AddListener(ToggleSignUpPassword);
            }

            if (signUpEyeButtonAgain != null)
            {
                signUpEyeButtonAgain.onClick.RemoveListener(ToggleSignUpPassword);
                signUpEyeButtonAgain.onClick.AddListener(ToggleSignUpPassword);
            }

            ApplyPasswordMode(loginPassword, loginEyeImage, loginPasswordHidden);
            ApplySignUpPasswordMode();
            HookInputFields();

            if (loginStatus != null)
            {
                loginStatus.color = StatusYellow;
                loginStatus.gameObject.SetActive(false);
            }

            if (signUpStatus != null)
            {
                signUpStatus.color = StatusYellow;
                signUpStatus.gameObject.SetActive(false);
            }
        }

        void EnsureKeyboard()
        {
            if (keyboard == null)
                keyboard = GetComponent<InGameKeyboard>();
            if (keyboard == null)
                keyboard = gameObject.AddComponent<InGameKeyboard>();

            Transform canvasRoot = null;
            if (loginRoot != null)
            {
                Canvas canvas = loginRoot.GetComponentInParent<Canvas>();
                if (canvas != null)
                    canvasRoot = canvas.transform;
            }

            if (canvasRoot == null && signUpRoot != null)
            {
                Canvas canvas = signUpRoot.GetComponentInParent<Canvas>();
                if (canvas != null)
                    canvasRoot = canvas.transform;
            }

            keyboard.EnsureBuilt(canvasRoot != null ? canvasRoot : transform);
        }

        void HookInputFields()
        {
            if (fieldsHooked)
                return;

            HookField(loginUsername, true);
            HookField(loginPassword, true);
            HookField(signUpUsername, false);
            HookField(signUpPassword, false);
            HookField(signUpPasswordAgain, false);
            fieldsHooked = true;
        }

        void HookField(TMP_InputField field, bool loginPanel)
        {
            if (field == null)
                return;

            field.shouldHideMobileInput = true;
            field.onFocusSelectAll = false;
            if (field.characterLimit <= 0)
            {
                bool isPassword = field == loginPassword
                    || field == signUpPassword
                    || field == signUpPasswordAgain;
                field.characterLimit = isPassword ? 128 : 64;
            }

            field.onSelect.RemoveAllListeners();
            field.onDeselect.RemoveAllListeners();
            field.onSelect.AddListener(_ =>
            {
                EnsureKeyboard();
                RectTransform lift = loginPanel ? loginShakeTarget : signUpShakeTarget;
                keyboard.Attach(field, lift);
            });
            field.onDeselect.AddListener(_ =>
            {
                if (keyboard != null)
                    keyboard.NotifyDeselect(field);
            });
        }

        public void OpenSignUp()
        {
            FluMixSfx.Play("ui_page");
            if (keyboard != null)
                keyboard.Hide();
            ShowStatus(null, true);
            if (flow != null)
                flow.SetScreen(FluMixScreen.Register);
            else
                ShowRoots(false);
        }

        public void OpenLogin()
        {
            FluMixSfx.Play("ui_page");
            if (keyboard != null)
                keyboard.Hide();
            ShowStatus(null, false);
            if (flow != null)
                flow.SetScreen(FluMixScreen.Login);
            else
                ShowRoots(true);
        }

        void ShowRoots(bool login)
        {
            if (loginRoot != null)
                loginRoot.SetActive(login);
            if (signUpRoot != null)
                signUpRoot.SetActive(!login);
        }

        public void ShowStatus(string message, bool onLogin)
        {
            TMP_Text target = onLogin ? loginStatus : signUpStatus;
            if (target == null)
                target = loginStatus != null ? loginStatus : signUpStatus;
            if (target == null)
                return;

            bool show = !string.IsNullOrEmpty(message);
            target.gameObject.SetActive(show);
            if (show)
            {
                target.color = StatusYellow;
                target.text = message;
            }
        }

        public void SubmitLogin()
        {
            if (busy)
                return;

            if (keyboard != null)
                keyboard.Hide();

            string user = loginUsername != null ? loginUsername.text : string.Empty;
            string pass = loginPassword != null ? loginPassword.text : string.Empty;

            if (services != null && services.UseOnlineApi && services.Api != null)
            {
                if (string.IsNullOrEmpty(pass))
                {
                    FailLocal("Mật khẩu không được để trống. Vui lòng thử lại.", true);
                    return;
                }

                StartCoroutine(SubmitOnline(false, user, pass, null));
                return;
            }

            if (services != null && services.Players.TryLogin(user, pass, out PlayerRecord player))
            {
                FluMixSfx.Play("ui_confirm");
                flow.OnLoggedIn(player);
                return;
            }

            FailLocal("Sai tên đăng nhập hoặc mật khẩu. Vui lòng thử lại.", true);
        }

        public void SubmitRegister()
        {
            if (busy)
                return;

            if (keyboard != null)
                keyboard.Hide();

            string user = signUpUsername != null ? signUpUsername.text : string.Empty;
            string pass = signUpPassword != null ? signUpPassword.text : string.Empty;
            string pass2 = signUpPasswordAgain != null ? signUpPasswordAgain.text : string.Empty;

            // Cho phép gõ khoảng trắng; NormUser bỏ khoảng trắng trước khi kiểm / gửi username.
            if (!FluMistApi.ValidUsername(user))
            {
                FailLocal("Tên 3–64 ký tự: chữ không dấu, số, . _ - @ + (khoảng trắng được phép). Vui lòng thử lại.", false);
                return;
            }

            if (!FluMistApi.ValidPassword(pass))
            {
                FailLocal("Mật khẩu từ 6 đến 128 ký tự. Vui lòng thử lại.", false);
                return;
            }

            if (pass != pass2)
            {
                FailLocal("Mật khẩu nhập lại không khớp. Vui lòng thử lại.", false);
                return;
            }

            // fullName giữ khoảng trắng để hiện BXH; username vẫn NormUser trong API.
            string fullName = user != null ? user.Trim() : string.Empty;
            if (string.IsNullOrEmpty(fullName))
                fullName = FluMistApi.NormUser(user);
            StartCoroutine(SubmitOnline(true, user, pass, fullName));
        }

        IEnumerator SubmitOnline(bool register, string user, string pass, string fullName)
        {
            busy = true;
            SetBusyButtons(false);
            ShowStatus(register ? "Đang đăng ký…" : "Đang đăng nhập…", !register);

            AuthDto auth = null;
            long http = 0;
            string message = null;

            if (register)
            {
                yield return services.Api.Register(user, pass, fullName,
                    a => auth = a,
                    (h, c, m) => { http = h; message = m; });
            }
            else
            {
                yield return services.Api.Login(user, pass,
                    a => auth = a,
                    (h, c, m) => { http = h; message = m; });
            }

            if (auth == null || auth.player == null)
            {
                busy = false;
                SetBusyButtons(true);
                FailLocal(FormatAuthError(register, http, message), !register);
                yield break;
            }

            // Register xong = đã login (token trong response) → tải câu hỏi rồi vào game
            ShowStatus("Đang tải câu hỏi…", !register);
            QuestionsDto questions = null;
            yield return services.Api.Questions(
                q => questions = q,
                (h, c, m) => { http = h; message = m; });

            if (questions == null || questions.questions == null || questions.questions.Length == 0)
            {
                busy = false;
                SetBusyButtons(true);
                FailLocal((message ?? "Không tải được câu hỏi") + " Vui lòng thử lại sau.", !register);
                yield break;
            }

            services.SetCachedQuestions(questions);
            var player = new PlayerRecord
            {
                username = auth.player.username,
                displayName = string.IsNullOrEmpty(auth.player.fullName) ? auth.player.username : auth.player.fullName,
                uuid = auth.player.uuid
            };

            busy = false;
            SetBusyButtons(true);
            FluMixSfx.Play("ui_confirm");
            ShowStatus(null, !register);
            flow.OnLoggedIn(player);
        }

        static string FormatAuthError(bool register, long http, string message)
        {
            if (http == 0)
                return "Mất kết nối server. Vui lòng thử lại sau.";
            if (http == 409)
                return "Tên đăng nhập đã có người dùng. Vui lòng thử lại.";
            if (http == 401)
                return (message ?? "Sai tên đăng nhập hoặc mật khẩu") + " Vui lòng thử lại.";
            if (http == 403)
                return (message ?? "Không thể đăng ký") + " Vui lòng thử lại sau.";
            if (http >= 500)
                return "Server đang lỗi. Vui lòng thử lại sau.";
            if (!string.IsNullOrEmpty(message))
                return message + (message.EndsWith(".") ? " " : ". ") + "Vui lòng thử lại.";
            return register
                ? "Đăng ký thất bại. Vui lòng thử lại sau."
                : "Đăng nhập thất bại. Vui lòng thử lại sau.";
        }

        void SetBusyButtons(bool interactable)
        {
            if (loginStartButton != null)
                loginStartButton.interactable = interactable;
            if (signUpSubmitButton != null)
                signUpSubmitButton.interactable = interactable;
            if (goToSignUpButton != null)
                goToSignUpButton.interactable = interactable;
            if (goToLoginButton != null)
                goToLoginButton.interactable = interactable;
        }

        public void ToggleLoginPassword()
        {
            loginPasswordHidden = !loginPasswordHidden;
            FluMixSfx.Play("input_password");
            ApplyPasswordMode(loginPassword, loginEyeImage, loginPasswordHidden);
        }

        public void ToggleSignUpPassword()
        {
            signUpPasswordHidden = !signUpPasswordHidden;
            FluMixSfx.Play("input_password");
            ApplySignUpPasswordMode();
        }

        void ApplySignUpPasswordMode()
        {
            ApplyPasswordMode(signUpPassword, signUpEyeImage, signUpPasswordHidden);
            ApplyPasswordMode(signUpPasswordAgain, signUpEyeImageAgain, signUpPasswordHidden);
        }

        void ApplyPasswordMode(TMP_InputField field, Image eyeImage, bool hidden)
        {
            if (field != null)
            {
                field.contentType = hidden
                    ? TMP_InputField.ContentType.Password
                    : TMP_InputField.ContentType.Standard;
                field.ForceLabelUpdate();
            }

            if (eyeImage != null && eyeClosed != null && eyeOpen != null)
                eyeImage.sprite = hidden ? eyeClosed : eyeOpen;
        }

        void FailLocal(string message, bool onLogin)
        {
            FluMixSfx.Play("ui_deny");
            if (!string.IsNullOrEmpty(message))
                ShowStatus(message, onLogin);
            StartCoroutine(Shake(onLogin));
        }

        IEnumerator Shake(bool onLogin)
        {
            RectTransform target = onLogin ? loginShakeTarget : signUpShakeTarget;
            Vector2 bas = onLogin ? loginShakeBase : signUpShakeBase;
            if (target == null)
                yield break;

            float elapsed = 0f;
            float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - Mathf.Clamp01(elapsed / duration);
                float offset = Mathf.Sin(elapsed * 48f) * 14f * fade;
                target.anchoredPosition = bas + new Vector2(offset, 0f);
                yield return null;
            }

            target.anchoredPosition = bas;
        }
    }
}
