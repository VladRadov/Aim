using System.Collections.Generic;
using Aim.Config;
using UnityEngine;

namespace Aim.Services
{
    public sealed class SpawnOccupancyTracker
    {
        readonly List<Transform> _occupied = new();
        readonly float _minDistance;
        readonly int _maxAttempts;

        public SpawnOccupancyTracker(float minDistance, int maxAttempts = 24)
        {
            _minDistance = Mathf.Max(0.1f, minDistance);
            _maxAttempts = Mathf.Max(1, maxAttempts);
        }

        public void Register(Transform target)
        {
            if (target != null && !_occupied.Contains(target))
                _occupied.Add(target);
        }

        public void Unregister(Transform target)
        {
            if (target != null)
                _occupied.Remove(target);
        }

        public void Clear() => _occupied.Clear();

        public bool TryGetFreePoint(LevelDefinition definition, out Vector3 point, bool groundOnly = false)
        {
            var minSqr = _minDistance * _minDistance;

            for (var attempt = 0; attempt < _maxAttempts; attempt++)
            {
                var candidate = groundOnly
                    ? definition.GetRandomGroundSpawnPoint()
                    : definition.GetRandomSpawnPoint();
                if (IsFree(candidate, minSqr))
                {
                    point = candidate;
                    return true;
                }
            }

            point = default;
            return false;
        }

        bool IsFree(Vector3 candidate, float minSqr)
        {
            for (var i = _occupied.Count - 1; i >= 0; i--)
            {
                var occupied = _occupied[i];
                if (occupied == null)
                {
                    _occupied.RemoveAt(i);
                    continue;
                }

                if (!occupied.gameObject.activeInHierarchy)
                    continue;

                if ((occupied.position - candidate).sqrMagnitude < minSqr)
                    return false;
            }

            return true;
        }
    }
}
