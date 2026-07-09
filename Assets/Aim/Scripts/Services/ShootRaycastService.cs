using Aim.Models;
using UnityEngine;

namespace Aim.Services
{
    public sealed class ShootRaycastService
    {
        readonly float _maxDistance;
        readonly LayerMask _targetMask;

        public ShootRaycastService(float maxDistance, LayerMask targetMask)
        {
            _maxDistance = maxDistance;
            _targetMask = targetMask;
        }

        public ShootResult Shoot(Camera camera)
        {
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (!Physics.Raycast(ray, out var hit, _maxDistance, _targetMask))
                return ShootResult.Miss;

            return HitResolver.Resolve(hit);
        }
    }
}
