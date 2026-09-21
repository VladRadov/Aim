using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class MobileControlsView : MonoBehaviour
    {
        [SerializeField] CanvasGroup rootGroup;
        [SerializeField] RectTransform joystickRoot;
        [SerializeField] RectTransform joystickKnob;
        [SerializeField] RectTransform fireRoot;
        [SerializeField] float joystickRadius = 68f;
        [SerializeField] float lookPixelsPerSecond = 760f;
        [SerializeField] float edgePadding = 28f;

        readonly Subject<Vector2> _lookSubject = new();
        readonly Subject<Unit> _fireSubject = new();

        MobileJoystickPad _joystick;
        MobileFirePad _fire;
        bool _visible;

        public IObservable<Vector2> LookStream => _lookSubject;
        public IObservable<Unit> FireStream => _fireSubject;

        void Awake()
        {
            EnsureBuilt();
            BindPads();
            SetVisible(false, false);
        }

        public static MobileControlsView Create(Transform canvasTransform)
        {
            var go = new GameObject("MobileControlsUI", typeof(RectTransform));
            go.transform.SetParent(canvasTransform, false);
            StretchFull(go.GetComponent<RectTransform>());
            return go.AddComponent<MobileControlsView>();
        }

        public void SetVisible(bool visible, bool showFire)
        {
            _visible = visible;
            if (rootGroup != null)
            {
                rootGroup.alpha = visible ? 1f : 0f;
                rootGroup.interactable = visible;
                rootGroup.blocksRaycasts = visible;
                rootGroup.gameObject.SetActive(visible);
            }

            if (fireRoot != null)
                fireRoot.gameObject.SetActive(visible && showFire);

            if (!visible)
                _joystick?.ResetKnob();

            ApplySafeArea();
        }

        void LateUpdate()
        {
            if (!_visible || _joystick == null || !_joystick.IsHeld)
                return;

            var axis = _joystick.Axis;
            if (axis.sqrMagnitude < 0.0004f)
                return;

            _lookSubject.OnNext(axis * lookPixelsPerSecond * Time.unscaledDeltaTime);
        }

        void OnRectTransformDimensionsChange() => ApplySafeArea();

        void OnDestroy()
        {
            _lookSubject.Dispose();
            _fireSubject.Dispose();
        }

        void EnsureBuilt()
        {
            if (joystickRoot != null && fireRoot != null && rootGroup != null)
                return;

            BuildHierarchy();
        }

        void BindPads()
        {
            if (joystickRoot != null)
            {
                _joystick = joystickRoot.GetComponent<MobileJoystickPad>() ??
                            joystickRoot.gameObject.AddComponent<MobileJoystickPad>();
                _joystick.Configure(joystickKnob, joystickRadius);
            }

            if (fireRoot != null)
            {
                _fire = fireRoot.GetComponent<MobileFirePad>() ??
                        fireRoot.gameObject.AddComponent<MobileFirePad>();
                _fire.Fired = () => _fireSubject.OnNext(Unit.Default);
            }
        }

        void ApplySafeArea()
        {
            if (joystickRoot == null || fireRoot == null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            var scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
            var safe = Screen.safeArea;
            var padLeft = edgePadding + safe.xMin / scale;
            var padRight = edgePadding + (Screen.width - safe.xMax) / scale;
            var padBottom = edgePadding + safe.yMin / scale;

            joystickRoot.anchorMin = Vector2.zero;
            joystickRoot.anchorMax = Vector2.zero;
            joystickRoot.pivot = new Vector2(0.5f, 0.5f);
            var joystickSize = joystickRoot.sizeDelta;
            joystickRoot.anchoredPosition = new Vector2(
                padLeft + joystickSize.x * 0.5f,
                padBottom + joystickSize.y * 0.5f);

            fireRoot.anchorMin = new Vector2(1f, 0f);
            fireRoot.anchorMax = new Vector2(1f, 0f);
            fireRoot.pivot = new Vector2(0.5f, 0.5f);
            var fireSize = fireRoot.sizeDelta;
            fireRoot.anchoredPosition = new Vector2(
                -(padRight + fireSize.x * 0.5f),
                padBottom + fireSize.y * 0.5f);
        }

        void BuildHierarchy()
        {
            var rect = GetComponent<RectTransform>();
            StretchFull(rect);

            rootGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            joystickRoot = CreateCircle(
                transform,
                "AimJoystick",
                new Vector2(168f, 168f),
                new Color(0.08f, 0.11f, 0.16f, 0.55f),
                new Color(0.38f, 0.78f, 1f, 0.55f));

            joystickKnob = CreateCircle(
                joystickRoot,
                "Knob",
                new Vector2(72f, 72f),
                new Color(0.18f, 0.28f, 0.42f, 0.95f),
                new Color(0.55f, 0.86f, 1f, 0.9f),
                raycast: false);
            joystickKnob.anchorMin = new Vector2(0.5f, 0.5f);
            joystickKnob.anchorMax = new Vector2(0.5f, 0.5f);
            joystickKnob.pivot = new Vector2(0.5f, 0.5f);
            joystickKnob.anchoredPosition = Vector2.zero;

            fireRoot = CreateCircle(
                transform,
                "FireButton",
                new Vector2(148f, 148f),
                new Color(0.42f, 0.12f, 0.12f, 0.72f),
                new Color(1f, 0.42f, 0.38f, 0.95f));

            var fireInner = CreateCircle(
                fireRoot,
                "Inner",
                new Vector2(78f, 78f),
                new Color(0.86f, 0.22f, 0.22f, 0.95f),
                new Color(1f, 0.72f, 0.62f, 1f),
                raycast: false);
            fireInner.anchorMin = new Vector2(0.5f, 0.5f);
            fireInner.anchorMax = new Vector2(0.5f, 0.5f);
            fireInner.pivot = new Vector2(0.5f, 0.5f);
            fireInner.anchoredPosition = Vector2.zero;
        }

        static RectTransform CreateCircle(
            Transform parent,
            string name,
            Vector2 size,
            Color fill,
            Color ring,
            bool raycast = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = CircleSprite();
            image.color = fill;
            image.raycastTarget = raycast;
            image.preserveAspect = true;

            var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(go.transform, false);
            var ringRect = ringGo.GetComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.offsetMin = new Vector2(4f, 4f);
            ringRect.offsetMax = new Vector2(-4f, -4f);
            var ringImage = ringGo.GetComponent<Image>();
            ringImage.sprite = image.sprite;
            ringImage.color = ring;
            ringImage.raycastTarget = false;
            ringImage.preserveAspect = true;
            return rect;
        }

        static void StretchFull(RectTransform rect)
        {
            if (rect == null)
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Sprite _circleSprite;

        static Sprite CircleSprite()
        {
            if (_circleSprite != null)
                return _circleSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var center = (size - 1) * 0.5f;
            var radius = center - 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply(false, false);
            _circleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _circleSprite.name = "MobileCircle";
            return _circleSprite;
        }
    }

    public sealed class MobileJoystickPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform _knob;
        float _radius = 68f;
        Vector2 _axis;

        public bool IsHeld { get; private set; }
        public Vector2 Axis => _axis;

        public void Configure(RectTransform knob, float radius)
        {
            _knob = knob;
            _radius = Mathf.Max(24f, radius);
        }

        public void ResetKnob()
        {
            IsHeld = false;
            _axis = Vector2.zero;
            if (_knob != null)
                _knob.anchoredPosition = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            IsHeld = true;
            var rect = transform as RectTransform;
            if (rect == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                eventData.position,
                eventData.pressEventCamera,
                out var local);

            var offset = Vector2.ClampMagnitude(local, _radius);
            _axis = offset / _radius;
            if (_knob != null)
                _knob.anchoredPosition = offset;
        }

        public void OnPointerUp(PointerEventData eventData) => ResetKnob();
    }

    public sealed class MobileFirePad : MonoBehaviour, IPointerDownHandler
    {
        public Action Fired;

        public void OnPointerDown(PointerEventData eventData)
        {
            Fired?.Invoke();
        }
    }
}
