using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FluMix
{
    public class HeroView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] HeroId id;
        [SerializeField] Image image;
        [SerializeField] Button button;
        [Tooltip("Vi tri bat dau hieu ung ban (khong phai tam sprite). Keo trong Prefab/Scene de chinh.")]
        [SerializeField] RectTransform muzzle;
        [SerializeField] SpriteSheetPlayer sheet;

        public HeroId Id => id;
        public Image Image => image;
        public RectTransform Muzzle => muzzle != null ? muzzle : (RectTransform)transform;
        public SpriteSheetPlayer Sheet =>
            sheet != null ? sheet : (sheet = GetComponent<SpriteSheetPlayer>());
        public event Action<HeroView> Clicked;
        public event Action<HeroView, PointerEventData> AimBegan;
        public event Action<HeroView, PointerEventData> AimMoved;
        public event Action<HeroView, PointerEventData> AimEnded;

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

        public void OnPointerDown(PointerEventData eventData)
        {
            AimBegan?.Invoke(this, eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AimEnded?.Invoke(this, eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            AimBegan?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            AimMoved?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            AimEnded?.Invoke(this, eventData);
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;
        }

        void OnDrawGizmos()
        {
            DrawTouchGizmo(false);
        }

        void OnDrawGizmosSelected()
        {
            DrawTouchGizmo(true);
        }

        void DrawTouchGizmo(bool selected)
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null)
                return;

            if (image == null)
                image = GetComponent<Image>();
            Vector4 pad = image != null ? image.raycastPadding : Vector4.zero;
            Rect area = rect.rect;
            area.xMin += pad.x;
            area.yMin += pad.y;
            area.xMax -= pad.z;
            area.yMax -= pad.w;

            Vector3 Corner(float x, float y)
            {
                return rect.TransformPoint(new Vector3(x, y, 0f));
            }

            Vector3 bl = Corner(area.xMin, area.yMin);
            Vector3 tl = Corner(area.xMin, area.yMax);
            Vector3 tr = Corner(area.xMax, area.yMax);
            Vector3 br = Corner(area.xMax, area.yMin);
            Gizmos.color = selected ? Color.green : new Color(0.3f, 1f, 0.45f, 0.95f);
            Gizmos.DrawLine(bl, tl);
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);

            Vector3 center = (bl + tr) * 0.5f;
            float unit = Mathf.Max(0.001f, rect.lossyScale.x);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, 16f * unit);
            Gizmos.DrawLine(center + Vector3.left * 22f * unit, center + Vector3.right * 22f * unit);
            Gizmos.DrawLine(center + Vector3.down * 22f * unit, center + Vector3.up * 22f * unit);

            if (muzzle != null)
            {
                Vector3 tip = muzzle.position;
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(tip, 8f * unit);
                Gizmos.DrawLine(center, tip);
            }
        }
    }
}
