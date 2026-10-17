using System;
using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public class HeroView : MonoBehaviour
    {
        [SerializeField] HeroId id;
        [SerializeField] Image image;
        [SerializeField] Button button;
        [SerializeField] RectTransform muzzle;
        [SerializeField] SpriteSheetPlayer sheet;

        public HeroId Id => id;
        public Image Image => image;
        public RectTransform Muzzle => muzzle != null ? muzzle : (RectTransform)transform;
        public SpriteSheetPlayer Sheet =>
            sheet != null ? sheet : (sheet = GetComponent<SpriteSheetPlayer>());
        public event Action<HeroView> Clicked;

        public void Bind(HeroId heroId, RectTransform muzzlePoint)
        {
            id = heroId;
            muzzle = muzzlePoint;
            if (image == null)
                image = GetComponent<Image>();
            if (button == null)
                button = GetComponent<Button>();
        }

        void Awake()
        {
            if (image == null)
                image = GetComponent<Image>();
            if (button == null)
                button = GetComponent<Button>();
            if (sheet == null)
                sheet = GetComponent<SpriteSheetPlayer>();
        }

        void OnEnable()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button == null)
                return;
            if (image != null)
                button.targetGraphic = image;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        void HandleClick()
        {
            Clicked?.Invoke(this);
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;
        }
    }
}
