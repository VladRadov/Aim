using Aim.Models;
using UnityEngine;

namespace Aim.Services
{
    public static class HitResolver
    {
        public static ShootResult Resolve(RaycastHit hit)
        {
            var hittable = hit.collider.GetComponentInParent<IHittable>();
            if (hittable != null && hittable.IsActive)
                hittable.OnHit(hit.point, hit.normal);

            return new ShootResult(hittable != null && hittable.IsActive, hit.point, hit.normal, hittable);
        }
    }
}
