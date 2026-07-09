using UnityEngine;

namespace Aim.Models
{
    public interface IHittable
    {
        bool IsActive { get; }
        void OnHit(Vector3 hitPoint, Vector3 hitNormal);
    }
}
