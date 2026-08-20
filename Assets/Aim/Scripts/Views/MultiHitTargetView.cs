using System;
using System.Threading;
using Aim.Models;
using Aim.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Views
{
    public sealed class MultiHitTargetView : MonoBehaviour, IHittable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] Renderer targetRenderer;
        [SerializeField] Color fullHpColor = new(0.25f, 0.85f, 0.45f, 1f);
        [SerializeField] Color midHpColor = new(0.95f, 0.75f, 0.2f, 1f);
        [SerializeField] Color lowHpColor = new(0.95f, 0.3f, 0.25f, 1f);
        [SerializeField] Color hitColor = Color.white;
        [SerializeField] float hitFlashDuration = 0.08f;

        MultiHitTargetPool _pool;
        MaterialPropertyBlock _propertyBlock;
        CancellationTokenSource _lifeCts;
        Action<MultiHitTargetView> _onDespawn;
        int _hitsRemaining;
        int _hitsMax;
        bool _isActive;

        public bool IsActive => _isActive && gameObject.activeInHierarchy;
        public bool CountsAsScore => IsActive && _hitsRemaining <= 1;

        public void Initialize(MultiHitTargetPool pool)
        {
            _pool = pool;
        }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            ApplyColor(fullHpColor);
        }

        public void Activate(
            Vector3 position,
            int hitPoints,
            float lifetime,
            Action<MultiHitTargetView> onDespawn)
        {
            CancelLife();
            _onDespawn = onDespawn;
            _hitsMax = Mathf.Max(1, hitPoints);
            _hitsRemaining = _hitsMax;
            transform.position = position;
            _isActive = true;
            ApplyHpColor();
            gameObject.SetActive(true);
            LifetimeAsync(lifetime).Forget();
        }

        public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsActive)
                return;

            _hitsRemaining = Mathf.Max(0, _hitsRemaining - 1);
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
            NotifyDespawn();
            _pool?.Return(this);
        }

        public void ResetState()
        {
            CancelLife();
            _isActive = false;
            _hitsRemaining = 0;
            _onDespawn = null;
            ApplyColor(fullHpColor);
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
