using System;
using System.Threading;
using Aim.Models;
using Aim.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class MultiHitTargetView : MonoBehaviour, IHittable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] Renderer targetRenderer;
        [SerializeField] Image healthFill;
        [SerializeField] Image healthBackground;
        [SerializeField] Canvas healthCanvas;
        [SerializeField] Color fullHpColor = new(0.25f, 0.85f, 0.45f, 1f);
        [SerializeField] Color midHpColor = new(0.95f, 0.75f, 0.2f, 1f);
        [SerializeField] Color lowHpColor = new(0.95f, 0.3f, 0.25f, 1f);
        [SerializeField] Color healthFullColor = new(0.25f, 1f, 0.55f, 1f);
        [SerializeField] Color healthLowColor = new(1f, 0.25f, 0.35f, 1f);
        [SerializeField] Color hitColor = Color.white;
        [SerializeField] float hitFlashDuration = 0.08f;
        [SerializeField] float healthBarWorldWidth = 1.35f;
        [SerializeField] float healthBarWorldHeight = 0.16f;
        [SerializeField] float healthBarHeightOffset = 0.62f;

        static Sprite _whiteSprite;

        MultiHitTargetPool _pool;
        MaterialPropertyBlock _propertyBlock;
        CancellationTokenSource _lifeCts;
        Action<MultiHitTargetView> _onDespawn;
        Camera _billboardCamera;
        int _hitsRemaining;
        int _hitsMax;
        bool _isActive;

        public bool IsActive => _isActive && gameObject.activeInHierarchy;
        public bool CountsAsScore => IsActive && _hitsRemaining <= 1;
        public float HealthNormalized => _hitsMax > 0 ? Mathf.Clamp01((float)_hitsRemaining / _hitsMax) : 0f;

        public void Initialize(MultiHitTargetPool pool)
        {
            _pool = pool;
            EnsureHealthBar();
        }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            EnsureHealthBar();
            ApplyColor(fullHpColor);
            UpdateHealthBar();
        }

        public void Activate(
            Vector3 position,
            int hitPoints,
            float lifetime,
            Camera billboardCamera,
            Action<MultiHitTargetView> onDespawn)
        {
            CancelLife();
            EnsureHealthBar();

            _onDespawn = onDespawn;
            _billboardCamera = billboardCamera;
            _hitsMax = Mathf.Max(1, hitPoints);
            _hitsRemaining = _hitsMax;
            transform.position = position;
            _isActive = true;
            ApplyHpColor();
            UpdateHealthBar();
            gameObject.SetActive(true);
            LifetimeAsync(lifetime).Forget();
        }

        public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsActive)
                return;

            _hitsRemaining = Mathf.Max(0, _hitsRemaining - 1);
            UpdateHealthBar();

            if (_hitsRemaining <= 0)
            {
                FlashAndDespawnAsync().Forget();
                return;
            }

            FlashDamageAsync().Forget();
        }

        public void Despawn()
        {
            if (!_isActive)
                return;

            _isActive = false;
            CancelLife();
            UpdateHealthBar();
            NotifyDespawn();
            _pool?.Return(this);
        }

        public void ResetState()
        {
            CancelLife();
            _isActive = false;
            _hitsRemaining = 0;
            _hitsMax = 0;
            _onDespawn = null;
            _billboardCamera = null;
            ApplyColor(fullHpColor);
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
            canvasTransform.rotation = Quaternion.LookRotation(
                canvasTransform.position - cam.transform.position,
                Vector3.up);
        }

        async UniTaskVoid LifetimeAsync(float lifetime)
        {
            if (lifetime <= 0f)
                return;

            _lifeCts = new CancellationTokenSource();
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(lifetime), cancellationToken: _lifeCts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_isActive)
                Despawn();
        }

        async UniTaskVoid FlashDamageAsync()
        {
            ApplyColor(hitColor);
            await UniTask.Delay(TimeSpan.FromSeconds(hitFlashDuration));
            if (_isActive)
                ApplyHpColor();
        }

        async UniTaskVoid FlashAndDespawnAsync()
        {
            _isActive = false;
            CancelLife();
            UpdateHealthBar();
            ApplyColor(hitColor);
            await UniTask.Delay(TimeSpan.FromSeconds(hitFlashDuration));
            NotifyDespawn();
            _pool?.Return(this);
        }

        void ApplyHpColor()
        {
            var t = _hitsMax <= 1 ? 0f : 1f - (_hitsRemaining - 1f) / (_hitsMax - 1f);
            if (t < 0.34f)
                ApplyColor(fullHpColor);
            else if (t < 0.67f)
                ApplyColor(midHpColor);
            else
                ApplyColor(lowHpColor);
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

        void NotifyDespawn()
        {
            var callback = _onDespawn;
            _onDespawn = null;
            callback?.Invoke(this);
        }

        void CancelLife()
        {
            _lifeCts?.Cancel();
            _lifeCts?.Dispose();
            _lifeCts = null;
        }

        void ApplyColor(Color color)
        {
            if (targetRenderer == null)
                return;

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
