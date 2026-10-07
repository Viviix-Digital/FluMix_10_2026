using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public class LoginController : MonoBehaviour
    {
        TMP_InputField _account;
        TMP_InputField _password;
        Button _submit;
        Button _eye;
        GameObject _modal;
        bool _hidden = true;

        void Start()
        {
            var card = transform.Find("Card");
            _account = card.Find("Account").GetComponent<TMP_InputField>();
            _password = card.Find("Password").GetComponent<TMP_InputField>();
            _submit = card.Find("Submit").GetComponent<Button>();
            _eye = card.Find("Eye").GetComponent<Button>();
            _modal = transform.Find("ForgotModal").gameObject;
            _submit.onClick.AddListener(Submit);
            _eye.onClick.AddListener(ToggleEye);
            card.Find("Forgot").GetComponent<Button>().onClick.AddListener(OpenForgot);
            _modal.transform.Find("Panel/Ok").GetComponent<Button>().onClick.AddListener(CloseForgot);
            _account.onValueChanged.AddListener(_ => Keystroke());
            _password.onValueChanged.AddListener(_ => Keystroke());
            var group = card.GetComponent<CanvasGroup>();
            var motion = FluMixFlow.Instance != null ? FluMixFlow.Instance.Motion : null;
            StartCoroutine(UiMotion.Enter(group, card.GetComponent<RectTransform>(), motion));
            var title = card.Find("Title");
            var bob = title.GetComponent<TitleBob>();
            if (bob != null)
            {
                var glyphs = new RectTransform[title.childCount];
                for (int i = 0; i < title.childCount; i++) glyphs[i] = title.GetChild(i) as RectTransform;
                bob.Play(glyphs);
            }
        }

        void Keystroke()
        {
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("input_key");
        }

        void ToggleEye()
        {
            _hidden = !_hidden;
            _password.contentType = _hidden ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            _password.ForceLabelUpdate();
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("input_password");
        }

        void Submit()
        {
            bool emptyA = string.IsNullOrWhiteSpace(_account.text);
            bool emptyP = string.IsNullOrWhiteSpace(_password.text);
            if (emptyA || emptyP)
            {
                if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_deny");
                var target = emptyA ? _account.transform as RectTransform : _password.transform as RectTransform;
                StartCoroutine(UiMotion.Shake(target, 0.28f));
                return;
            }
            GameSession.Account = _account.text.Trim();
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_confirm");
            FluMixFlow.Instance.GoLobby();
        }

        void OpenForgot()
        {
            _modal.SetActive(true);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_modal_open");
            var panel = _modal.transform.Find("Panel") as RectTransform;
            var dim = _modal.GetComponent<CanvasGroup>();
            StartCoroutine(UiMotion.ModalIn(dim, panel, FluMixFlow.Instance.Motion));
        }

        void CloseForgot()
        {
            _modal.SetActive(false);
            if (FluMixAudio.Instance != null) FluMixAudio.Instance.Play("ui_modal_close");
        }
    }
}
