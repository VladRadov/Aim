using System;
using System.Threading;
using Aim.Models;
using Aim.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Views
{
    public sealed class PriorityTargetView : MonoBehaviour, IHittable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] Renderer targetRenderer;
        [SerializeField] Color normalColor = new(0.45f, 0.5f, 0.55f, 1f);
        [SerializeField] Color priorityColor = new(1f, 0.82f, 0.12f, 1f);
        [SerializeField] Color hitColor = Color.white;
        [SerializeField] float hitFlashDuration = 0.1f;

        PriorityTargetPool _pool;
        MaterialPropertyBlock _propertyBlock;
        Action<PriorityTargetView> _onDespawn;
        bool _isActive;
        bool _isPriority;

        public bool IsActive => _isActive && gameObject.activeInHierarchy;
        public bool CountsAsScore => IsActive && _isPriority;
        public bool IsPriority => _isPriority;

        public void Initialize(PriorityTargetPool pool)
        {
            _pool = pool;
        }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            ApplyColor(normalColor);
        }

        public void Activate(Vector3 position, Action<PriorityTargetView> onDespawn)
        {
            _onDespawn = onDespawn;
            transform.position = position;
            _isActive = true;
            _isPriority = false;
            ApplyColor(normalColor);
            gameObject.SetActive(true);
            AudioSettingsService.Current?.PlaySpawn();
        }

        public void SetPriority(bool isPriority)
        {
            _isPriority = isPriority;
            if (_isActive)
                ApplyColor(isPriority ? priorityColor : normalColor);
        }

        public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsActive)
                return;

            if (!_isPriority)
            {
                FlashWrongAsync().Forget();
                return;
            }

            FlashAndDespawnAsync().Forget();
        }

        public void Despawn()
        {
            if (!_isActive)
                return;

            _isActive = false;
            _isPriority = false;
            NotifyDespawn();
            _pool?.Return(this);
        }

        public void ResetState()
        {
            _isActive = false;
            _isPriority = false;
            _onDespawn = null;
            ApplyColor(normalColor);
        }

        async UniTaskVoid FlashWrongAsync()
        {
            ApplyColor(hitColor);
            await UniTask.Delay(TimeSpan.FromSeconds(hitFlashDuration * 0.5f));
            if (_isActive)
                ApplyColor(_isPriority ? priorityColor : normalColor);
        }

        async UniTaskVoid FlashAndDespawnAsync()
        {
            _isActive = false;
            _isPriority = false;
            ApplyColor(hitColor);
            await UniTask.Delay(TimeSpan.FromSeconds(hitFlashDuration));
            NotifyDespawn();
            _pool?.Return(this);
        }

        void NotifyDespawn()
        {
            var callback = _onDespawn;
            _onDespawn = null;
            callback?.Invoke(this);
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
