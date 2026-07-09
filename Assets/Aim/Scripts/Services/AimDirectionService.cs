using UnityEngine;

namespace Aim.Services
{
    public sealed class AimDirectionService
    {
        readonly float _maxDistance;
        readonly LayerMask _aimPointMask;

        public AimDirectionService(float maxDistance, LayerMask aimPointMask)
        {
            _maxDistance = maxDistance;
            _aimPointMask = aimPointMask;
        }

        public Vector3 GetDirection(Camera camera, Vector3 spawnOrigin)
        {
            var centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            var aimPoint = Physics.Raycast(centerRay, out var hit, _maxDistance, _aimPointMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : centerRay.GetPoint(_maxDistance);

            var direction = aimPoint - spawnOrigin;
            return direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : centerRay.direction;
        }
    }
}
