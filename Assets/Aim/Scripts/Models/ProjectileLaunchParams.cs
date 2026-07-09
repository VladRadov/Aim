using UnityEngine;

namespace Aim.Models
{
    public readonly struct ProjectileLaunchParams
    {
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public float Speed { get; }
        public float MaxDistance { get; }
        public LayerMask HitMask { get; }

        public ProjectileLaunchParams(Vector3 origin, Vector3 direction, float speed, float maxDistance, LayerMask hitMask)
        {
            Origin = origin;
            Direction = direction;
            Speed = speed;
            MaxDistance = maxDistance;
            HitMask = hitMask;
        }
    }
}
