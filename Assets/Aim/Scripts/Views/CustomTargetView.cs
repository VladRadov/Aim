using System;
using Aim.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Views
{
    public sealed class CustomTargetView : MonoBehaviour
    {
        [SerializeField] HitZoneView[] hitZones;

        Action<CustomTargetView> _onScoredHit;
        bool _isActive = true;

        public bool IsActive => _isActive && gameObject.activeInHierarchy;

        void Awake()
        {
            if (hitZones == null || hitZones.Length == 0)
                hitZones = GetComponentsInChildren<HitZoneView>(true);

            foreach (var zone in hitZones)
            {
                if (zone == null)
                    continue;

                zone.Configure(HitZoneKind.Custom, true);
                zone.SetHitCallback(OnZoneHit);
            }
        }

        public void Activate(Vector3 position, Action<CustomTargetView> onScoredHit)
        {
            transform.position = position;
            _onScoredHit = onScoredHit;
            _isActive = true;
            gameObject.SetActive(true);

            foreach (var zone in hitZones)
                zone?.ResetState();
        }

        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }

        void OnZoneHit(HitZoneView zone, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!_isActive || !zone.CountsAsScore)
                return;

            _onScoredHit?.Invoke(this);
        }
    }
}
