using Aim.Models;
using UnityEngine;

namespace Aim.Services
{
    public static class HitResolver
    {
        public static ShootResult Resolve(RaycastHit hit)
        {
            var hittable = hit.collider.GetComponent<IHittable>()
                ?? hit.collider.GetComponentInParent<IHittable>();

            if (hittable == null || !hittable.IsActive)
            {
                return new ShootResult(
                    didHit: false,
                    countsAsScore: false,
                    hasImpact: true,
                    hit.point,
                    hit.normal,
                    null);
            }

            // Capture score flag before OnHit; OnHit is applied after hit FX plays.
            return new ShootResult(
                didHit: true,
                countsAsScore: hittable.CountsAsScore,
                hasImpact: true,
                hit.point,
                hit.normal,
                hittable);
        }

        public static void ApplyOnHit(ShootResult result)
        {
            if (!result.DidHit || result.Hittable == null)
                return;

            result.Hittable.OnHit(result.HitPoint, result.HitNormal);
        }
    }
}
