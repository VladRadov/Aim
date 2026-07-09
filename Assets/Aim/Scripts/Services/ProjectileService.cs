using System;
using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Services
{
    public sealed class ProjectileService
    {
        readonly ProjectilePool _pool;
        readonly float _speed;
        readonly float _maxDistance;
        readonly LayerMask _hitMask;

        public ProjectileService(
            ProjectilePool pool,
            float speed,
            float maxDistance,
            LayerMask hitMask)
        {
            _pool = pool;
            _speed = speed;
            _maxDistance = maxDistance;
            _hitMask = hitMask;
        }

        public void Launch(Vector3 origin, Vector3 direction, Action<ShootResult> onComplete)
        {
            var projectile = _pool.Rent();
            var launchParams = new ProjectileLaunchParams(origin, direction, _speed, _maxDistance, _hitMask);
            projectile.Launch(launchParams, onComplete);
        }
    }
}
