using UnityEngine;

namespace Aim.Models
{
    public readonly struct ShootResult
    {
        public static ShootResult Miss => new(false, Vector3.zero, Vector3.zero, null);

        public bool DidHit { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public IHittable Hittable { get; }

        public ShootResult(bool didHit, Vector3 hitPoint, Vector3 hitNormal, IHittable hittable)
        {
            DidHit = didHit;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Hittable = hittable;
        }
    }
}
