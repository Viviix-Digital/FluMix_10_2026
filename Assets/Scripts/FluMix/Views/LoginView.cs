using System.Collections;
using FluMix.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public class LoginView : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] AppServices services;
        [SerializeField] TMP_InputField username;
        [SerializeField] TMP_InputField password;
        [SerializeField] Button startButton;
        [SerializeField] Button eyeButton;
        [SerializeField] Image eyeImage;
        [SerializeField] Sprite eyeOpen;
        [SerializeField] Sprite eyeClosed;
        [SerializeField] Button forgotButton;
        [SerializeField] TMP_Text forgotMessage;
        [SerializeField] RectTransform shakeTarget;

        bool passwordHidden = true;
        Vector2 shakeBase;

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
            username = userField;
            password = passwordField;
            startButton = start;
            eyeButton = eye;
            eyeImage = eyeIcon;
            eyeOpen = open;
            eyeClosed = closed;
            forgotButton = forgot;
            forgotMessage = message;
            shakeTarget = panel;
            Wire();
        }

        void Awake()
        {
            if (shakeTarget != null)
                shakeBase = shakeTarget.anchoredPosition;
            Wire();
        }

        void Wire()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(Submit);
                startButton.onClick.AddListener(Submit);
            }

            if (eyeButton != null)
            {
                eyeButton.onClick.RemoveListener(TogglePassword);
                eyeButton.onClick.AddListener(TogglePassword);
            }

            if (forgotButton != null)
            {
                forgotButton.onClick.RemoveListener(ShowForgot);
                forgotButton.onClick.AddListener(ShowForgot);
            }

            ApplyPasswordMode();
            if (forgotMessage != null)
                forgotMessage.gameObject.SetActive(false);
        }

        public void Submit()
        {
            string user = username != null ? username.text : string.Empty;
            string pass = password != null ? password.text : string.Empty;
            if (services != null && services.Players.TryLogin(user, pass, out PlayerRecord player))
            {
                flow.OnLoggedIn(player);
                return;
            }

            StartCoroutine(Shake());
        }

        public void TogglePassword()
        {
            passwordHidden = !passwordHidden;
            ApplyPasswordMode();
        }

        public void ShowForgot()
        {
            if (forgotMessage == null)
                return;
            forgotMessage.gameObject.SetActive(true);
            forgotMessage.text = "Mật khẩu lưu trên máy này";
        }

        void ApplyPasswordMode()
        {
            if (password != null)
            {
                password.contentType = passwordHidden
                    ? TMP_InputField.ContentType.Password
                    : TMP_InputField.ContentType.Standard;
                password.ForceLabelUpdate();
            }

            if (eyeImage != null)
                eyeImage.sprite = passwordHidden ? eyeClosed : eyeOpen;
        }

        IEnumerator Shake()
        {
            if (shakeTarget == null)
                yield break;

            float elapsed = 0f;
            float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - Mathf.Clamp01(elapsed / duration);
                float offset = Mathf.Sin(elapsed * 48f) * 14f * fade;
                shakeTarget.anchoredPosition = shakeBase + new Vector2(offset, 0f);
                yield return null;
            }

            shakeTarget.anchoredPosition = shakeBase;
        }
    }
}
