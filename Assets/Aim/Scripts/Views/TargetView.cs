using System;
using Aim.Models;
using Aim.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Views
{
    [RequireComponent(typeof(Collider))]
    public sealed class TargetView : MonoBehaviour, IHittable
    {
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Color normalColor = new(0.9f, 0.2f, 0.2f, 1f);
        [SerializeField] Color hitColor = Color.white;
        [SerializeField] float hitFlashDuration = 0.15f;
        [SerializeField] bool deactivateOnHit;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        TargetPool _pool;
        MaterialPropertyBlock _propertyBlock;
        bool _isActive = true;
        bool _isFlashing;

        public bool IsActive => _isActive && gameObject.activeInHierarchy;

        public void Initialize(TargetPool pool)
        {
            _pool = pool;
        }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            ApplyColor(normalColor);
        }

        public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsActive || _isFlashing)
                return;

            FlashHitAsync().Forget();
        }

        async UniTaskVoid FlashHitAsync()
        {
            _isFlashing = true;
            ApplyColor(hitColor);

            await UniTask.Delay(TimeSpan.FromSeconds(hitFlashDuration));

            if (deactivateOnHit)
            {
                _isActive = false;
                if (_pool != null)
                    _pool.Return(this);
                else
                    gameObject.SetActive(false);
            }
            else
            {
                ApplyColor(normalColor);
            }

            _isFlashing = false;
        }

        public void ResetState()
        {
            _isActive = true;
            _isFlashing = false;
            ApplyColor(normalColor);
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
