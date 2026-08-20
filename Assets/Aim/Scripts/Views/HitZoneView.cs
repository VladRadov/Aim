using System;
using Aim.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Views
{
    [RequireComponent(typeof(Collider))]
    public sealed class HitZoneView : MonoBehaviour, IHittable
    {
        [SerializeField] HitZoneKind zoneKind = HitZoneKind.Custom;
        [SerializeField] bool countsAsScore = true;
        [SerializeField] Renderer zoneRenderer;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color hitColor = new(1f, 0.85f, 0.2f, 1f);
        [SerializeField] float hitFlashDuration = 0.12f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        MaterialPropertyBlock _propertyBlock;
        bool _isActive = true;
        Action<HitZoneView, Vector3, Vector3> _onHitCallback;

        public HitZoneKind ZoneKind => zoneKind;
        public bool IsActive => _isActive && gameObject.activeInHierarchy;
        public bool CountsAsScore => countsAsScore && IsActive;

        public void Configure(HitZoneKind kind, bool score)
        {
            zoneKind = kind;
            countsAsScore = score;
        }

        public void SetHitCallback(Action<HitZoneView, Vector3, Vector3> callback)
        {
            _onHitCallback = callback;
        }

        public void SetActiveState(bool active)
        {
            _isActive = active;
        }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            ApplyColor(normalColor);
        }

        public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsActive)
                return;

            FlashAsync().Forget();
            _onHitCallback?.Invoke(this, hitPoint, hitNormal);
        }

        public void ResetState()
        {
            _isActive = true;
            ApplyColor(normalColor);
        }

        async UniTaskVoid FlashAsync()
        {
            ApplyColor(hitColor);
            await UniTask.Delay(TimeSpan.FromSeconds(hitFlashDuration));
            if (_isActive)
                ApplyColor(normalColor);
        }

        void ApplyColor(Color color)
        {
            if (zoneRenderer == null)
                return;

            zoneRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            zoneRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
