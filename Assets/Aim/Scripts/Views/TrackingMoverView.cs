using System;
using System.Threading;
using Aim.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class TrackingMoverView : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static Sprite _whiteSprite;

        [SerializeField] Renderer targetRenderer;
        [SerializeField] Image healthFill;
        [SerializeField] Image healthBackground;
        [SerializeField] Canvas healthCanvas;
        [SerializeField] Color normalColor = new(0.55f, 0.75f, 1f, 1f);
        [SerializeField] Color trackedColor = new(1f, 0.5f, 0.35f, 1f);
        [SerializeField] Color healthFullColor = new(0.35f, 0.95f, 1f, 1f);
        [SerializeField] Color healthLowColor = new(1f, 0.3f, 0.4f, 1f);
        [SerializeField] float healthBarWorldWidth = 1.8f;
        [SerializeField] float healthBarWorldHeight = 0.18f;
        [SerializeField] float healthBarHeightOffset = 0.56f;

        TrackingMoverPool _pool;
        MaterialPropertyBlock _propertyBlock;
        SphereCollider _collider;
        Camera _billboardCamera;
        CancellationTokenSource _moveCts;
        Action<TrackingMoverView> _onDespawn;
        Bounds _moveBounds;
        Vector3 _direction;
        float _speed;
        float _maxHealth;
        float _health;
        bool _isActive;
        bool _isTracked;

        public bool IsActive => _isActive && gameObject.activeInHierarchy;
        public float HealthNormalized => _maxHealth > 0f ? Mathf.Clamp01(_health / _maxHealth) : 0f;
        public Collider PhysicsCollider => _collider != null ? _collider : (_collider = GetComponent<SphereCollider>());

        public void Initialize(TrackingMoverPool pool)
        {
            _pool = pool;
            EnsureCollider();
            EnsureHealthBar();
        }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            EnsureCollider();
            EnsureHealthBar();
            ApplyColor(normalColor);
            UpdateHealthBar();
        }

        public void Activate(
            Vector3 position,
            Vector3 direction,
            float speed,
            float maxHealth,
            Bounds moveBounds,
            Camera billboardCamera,
            Action<TrackingMoverView> onDespawn)
        {
            CancelMove();
            EnsureCollider();
            EnsureHealthBar();

            _onDespawn = onDespawn;
            _billboardCamera = billboardCamera;
            _moveBounds = moveBounds;
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
            _speed = Mathf.Max(0.1f, speed);
            _maxHealth = Mathf.Max(1f, maxHealth);
            _health = _maxHealth;
            _isActive = true;
            _isTracked = false;

            transform.SetPositionAndRotation(position, Quaternion.identity);
            gameObject.SetActive(true);
            AudioSettingsService.Current?.PlaySpawn();
            ApplyColor(normalColor);
            UpdateHealthBar();
            MoveAsync().Forget();
        }

        public bool ApplyAimDamage(float amount)
        {
            if (!IsActive || amount <= 0f)
                return false;

            SetTracked(true);
            _health = Mathf.Max(0f, _health - amount);
            UpdateHealthBar();
            AudioSettingsService.Current?.PlayHealthDrain();

            if (_health > 0f)
                return false;

            Despawn();
            return true;
        }

        public void SetTracked(bool tracked)
        {
            if (_isTracked == tracked)
                return;

            _isTracked = tracked;
            ApplyColor(tracked ? trackedColor : normalColor);
        }

        public void Despawn()
        {
            if (!_isActive)
                return;

            _isActive = false;
            CancelMove();
            NotifyDespawn();
            _pool?.Return(this);
        }

        public void ResetState()
        {
            CancelMove();
            _isActive = false;
            _isTracked = false;
            _onDespawn = null;
            _health = 0f;
            _speed = 0f;
            ApplyColor(normalColor);
            UpdateHealthBar();
        }

        void LateUpdate()
        {
            if (!_isActive || healthCanvas == null)
                return;

            var cam = _billboardCamera != null ? _billboardCamera : Camera.main;
            if (cam == null)
                return;

            var canvasTransform = healthCanvas.transform;
            canvasTransform.position = transform.position + Vector3.up * healthBarHeightOffset;
            canvasTransform.rotation = Quaternion.LookRotation(canvasTransform.position - cam.transform.position, Vector3.up);
        }

        async UniTaskVoid MoveAsync()
        {
            _moveCts = new CancellationTokenSource();
            var token = _moveCts.Token;

            try
            {
                while (_isActive)
                {
                    var delta = _direction * (_speed * Time.deltaTime);
                    var next = transform.position + delta;
                    next = ReflectInsideBounds(next);
                    next.z = _moveBounds.center.z;
                    transform.position = next;
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        Vector3 ReflectInsideBounds(Vector3 next)
        {
            var min = _moveBounds.min;
            var max = _moveBounds.max;

            if (next.x < min.x)
            {
                next.x = min.x;
                _direction.x = Mathf.Abs(_direction.x);
            }
            else if (next.x > max.x)
            {
                next.x = max.x;
                _direction.x = -Mathf.Abs(_direction.x);
            }

            if (next.y < min.y)
            {
                next.y = min.y;
                _direction.y = Mathf.Abs(_direction.y);
            }
            else if (next.y > max.y)
            {
                next.y = max.y;
                _direction.y = -Mathf.Abs(_direction.y);
            }

            if (_direction.sqrMagnitude < 0.0001f)
                _direction = Vector3.right;
            else
                _direction.Normalize();

            return next;
        }

        void NotifyDespawn()
        {
            var callback = _onDespawn;
            _onDespawn = null;
            callback?.Invoke(this);
        }

        void CancelMove()
        {
            _moveCts?.Cancel();
            _moveCts?.Dispose();
            _moveCts = null;
        }

        void EnsureCollider()
        {
            if (_collider == null)
                _collider = GetComponent<SphereCollider>();
        }

        void EnsureHealthBar()
        {
            if (healthFill == null || healthBackground == null || healthCanvas == null)
                CreateHealthBar();

            ApplyBarSprites();
        }

        void CreateHealthBar()
        {
            var canvasGo = new GameObject("HealthBar");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = Vector3.up * healthBarHeightOffset;

            healthCanvas = canvasGo.AddComponent<Canvas>();
            healthCanvas.renderMode = RenderMode.WorldSpace;
            healthCanvas.sortingOrder = 20;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(160f, 18f);
            canvasRect.localScale = new Vector3(
                healthBarWorldWidth / 160f,
                healthBarWorldHeight / 18f,
                1f);

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            Stretch(bgGo.GetComponent<RectTransform>());
            healthBackground = bgGo.GetComponent<Image>();
            healthBackground.color = new Color(0.04f, 0.06f, 0.1f, 0.85f);
            healthBackground.raycastTarget = false;
            healthBackground.type = Image.Type.Simple;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(canvasGo.transform, false);
            healthFill = fillGo.GetComponent<Image>();
            healthFill.color = healthFullColor;
            healthFill.raycastTarget = false;
            healthFill.type = Image.Type.Simple;
            SetFillWidth(1f);
        }

        void ApplyBarSprites()
        {
            var sprite = WhiteSprite;
            if (healthBackground != null && healthBackground.sprite == null)
                healthBackground.sprite = sprite;
            if (healthFill != null && healthFill.sprite == null)
                healthFill.sprite = sprite;
        }

        static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                    return _whiteSprite;

                var texture = Texture2D.whiteTexture;
                _whiteSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                return _whiteSprite;
            }
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void SetFillWidth(float normalized)
        {
            if (healthFill == null)
                return;

            var fillRect = healthFill.rectTransform;
            var amount = Mathf.Clamp01(normalized);
            fillRect.anchorMin = new Vector2(0.03f, 0.18f);
            fillRect.anchorMax = new Vector2(Mathf.Lerp(0.03f, 0.97f, amount), 0.82f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);
        }

        void UpdateHealthBar()
        {
            if (healthFill == null)
                return;

            var normalized = HealthNormalized;
            SetFillWidth(normalized);
            healthFill.color = Color.Lerp(healthLowColor, healthFullColor, normalized);

            if (healthCanvas != null)
                healthCanvas.enabled = _isActive;
        }

        void ApplyColor(Color color)
        {
            if (targetRenderer == null)
                return;

            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
