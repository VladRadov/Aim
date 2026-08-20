using System;
using Aim.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class TrackingTargetView : MonoBehaviour
    {
        const float FloorNormalThreshold = 0.5f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] Renderer targetRenderer;
        [SerializeField] Rigidbody body;
        [SerializeField] Image healthFill;
        [SerializeField] Image healthBackground;
        [SerializeField] Canvas healthCanvas;
        [SerializeField] Color normalColor = new(0.2f, 0.95f, 0.75f, 1f);
        [SerializeField] Color trackedColor = new(1f, 0.45f, 0.55f, 1f);
        [SerializeField] Color healthFullColor = new(0.25f, 1f, 0.7f, 1f);
        [SerializeField] Color healthLowColor = new(1f, 0.25f, 0.45f, 1f);
        [SerializeField] float mass = 0.7f;
        [SerializeField] float healthBarWorldWidth = 2.85f;
        [SerializeField] float healthBarWorldHeight = 0.27f;
        [SerializeField] float healthBarHeightOffset = 1.65f;

        static Sprite _whiteSprite;

        TrackingTargetPool _pool;
        MaterialPropertyBlock _propertyBlock;
        PhysicsMaterial _bounceMaterial;
        SphereCollider _collider;
        Camera _billboardCamera;
        Action<TrackingTargetView> _onDespawn;
        float _maxHealth;
        float _health;
        float _bounceSpeed;
        bool _isActive;
        bool _isTracked;

        public bool IsActive => _isActive && gameObject.activeInHierarchy;
        public float HealthNormalized => _maxHealth > 0f ? Mathf.Clamp01(_health / _maxHealth) : 0f;

        public Collider PhysicsCollider => _collider != null ? _collider : (_collider = GetComponent<SphereCollider>());

        public void Initialize(TrackingTargetPool pool)
        {
            _pool = pool;
            EnsurePhysics();
            EnsureHealthBar();
        }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            EnsurePhysics();
            EnsureHealthBar();
            ApplyColor(normalColor);
            UpdateHealthBar();
        }

        public void Activate(
            Vector3 position,
            Vector3 velocity,
            float maxHealth,
            float bounciness,
            Camera billboardCamera,
            Action<TrackingTargetView> onDespawn)
        {
            EnsurePhysics();
            EnsureHealthBar();

            _onDespawn = onDespawn;
            _billboardCamera = billboardCamera;
            _maxHealth = Mathf.Max(1f, maxHealth);
            _health = _maxHealth;
            _bounceSpeed = Mathf.Max(0.5f, Mathf.Abs(velocity.y));
            _isActive = true;
            _isTracked = false;

            gameObject.SetActive(true);

            body.isKinematic = true;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            body.position = position;
            body.rotation = Quaternion.identity;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezePositionZ;
            body.linearVelocity = velocity;
            body.WakeUp();

            ApplyBounceMaterial(bounciness);
            ApplyColor(normalColor);
            UpdateHealthBar();
        }

        public bool ApplyAimDamage(float amount)
        {
            if (!IsActive || amount <= 0f)
                return false;

            SetTracked(true);
            _health = Mathf.Max(0f, _health - amount);
            UpdateHealthBar();

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
            NotifyDespawn();
            _pool?.Return(this);
        }

        public void ResetState()
        {
            _isActive = false;
            _isTracked = false;
            _onDespawn = null;
            _health = 0f;
            _bounceSpeed = 0f;
            ApplyColor(normalColor);
            UpdateHealthBar();

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.useGravity = false;
            }
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

        void OnCollisionEnter(Collision collision) => SustainFloorBounce(collision);

        void OnCollisionStay(Collision collision) => SustainFloorBounce(collision);

        void SustainFloorBounce(Collision collision)
        {
            if (!_isActive || body == null || _bounceSpeed <= 0f)
                return;

            if (!HasFloorContact(collision))
                return;

            var velocity = body.linearVelocity;
            if (velocity.y >= _bounceSpeed * 0.85f)
                return;

            velocity.y = _bounceSpeed;
            body.linearVelocity = velocity;
        }

        static bool HasFloorContact(Collision collision)
        {
            var count = collision.contactCount;
            for (var i = 0; i < count; i++)
            {
                if (collision.GetContact(i).normal.y > FloorNormalThreshold)
                    return true;
            }

            return false;
        }

        void NotifyDespawn()
        {
            var callback = _onDespawn;
            _onDespawn = null;
            callback?.Invoke(this);
        }

        void EnsurePhysics()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();

            if (_collider == null)
                _collider = GetComponent<SphereCollider>();

            if (_collider != null && _bounceMaterial == null)
            {
                _bounceMaterial = new PhysicsMaterial("TrackingBallMaterial")
                {
                    bounciness = 1f,
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounceCombine = PhysicsMaterialCombine.Maximum,
                    frictionCombine = PhysicsMaterialCombine.Minimum
                };
            }

            if (_collider != null)
                _collider.sharedMaterial = _bounceMaterial;

            body.mass = mass;
            body.linearDamping = 0f;
            body.angularDamping = 0.05f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionZ;
            body.sleepThreshold = 0f;
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

        void ApplyBounceMaterial(float bounciness)
        {
            if (_bounceMaterial == null)
                return;

            _bounceMaterial.bounciness = Mathf.Clamp01(Mathf.Max(bounciness, 0.95f));
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
