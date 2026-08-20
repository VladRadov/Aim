using UnityEngine;

namespace Aim.Models
{
    public readonly struct ShootResult
    {
        public static ShootResult Miss => new(false, false, false, Vector3.zero, Vector3.up, null);

        public bool DidHit { get; }
        public bool CountsAsScore { get; }
        public bool HasImpact { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public IHittable Hittable { get; }

        public ShootResult(
            bool didHit,
            bool countsAsScore,
            bool hasImpact,
            Vector3 hitPoint,
            Vector3 hitNormal,
            IHittable hittable)
        {
            DidHit = didHit;
            CountsAsScore = countsAsScore;
            HasImpact = hasImpact;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Hittable = hittable;
        }
    }
}
