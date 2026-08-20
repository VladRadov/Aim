using UnityEngine;

namespace Aim.Views
{
    public sealed class WeaponView : MonoBehaviour
    {
        [SerializeField] Transform weaponMount;
        [SerializeField] Transform muzzlePoint;

        Vector3 _baseLocalPosition;
        Quaternion _baseLocalRotation = Quaternion.identity;

        public Transform Mount => weaponMount;
        public Transform MuzzlePoint => muzzlePoint != null ? muzzlePoint : weaponMount;

        public Vector3 GetProjectileSpawnPosition()
        {
            return MuzzlePoint != null ? MuzzlePoint.position : transform.position;
        }

        public Vector3 GetMuzzleForward()
        {
            return MuzzlePoint != null ? MuzzlePoint.forward : transform.forward;
        }

        public void ConfigureMountPosition(Vector3 localPosition)
        {
            _baseLocalPosition = localPosition;
            ApplyPose(Vector3.zero, Vector3.zero);
        }

        public void SetRecoilOffset(Vector3 rotationEuler, Vector3 positionOffset)
        {
            ApplyPose(rotationEuler, positionOffset);
        }

        public void AttachWeapon(GameObject weaponPrefab)
        {
            if (weaponPrefab == null)
                return;

            for (var i = weaponMount.childCount - 1; i >= 0; i--)
                Destroy(weaponMount.GetChild(i).gameObject);

            var instance = Instantiate(weaponPrefab, weaponMount);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            muzzlePoint = ResolveMuzzlePoint(instance.transform);
        }

        Transform ResolveMuzzlePoint(Transform weaponRoot)
        {
            var namedMuzzle = FindNamedPart(weaponRoot, "Muzzle", excludeSubstring: "Guard");
            if (namedMuzzle != null)
                return CreateTipAnchor(namedMuzzle);

            var barrel = FindNamedPart(weaponRoot, "Barrel", excludeSubstring: "Guard");
            if (barrel != null)
                return CreateTipAnchor(barrel);

            return CreateTipAnchor(weaponRoot);
        }

        static Transform FindNamedPart(Transform root, string nameContains, string excludeSubstring)
        {
            Transform best = null;
            var bestDepth = -1;
            var transforms = root.GetComponentsInChildren<Transform>(true);

            for (var i = 0; i < transforms.Length; i++)
            {
                var candidate = transforms[i];
                var name = candidate.name;
                if (name.IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (!string.IsNullOrEmpty(excludeSubstring) &&
                    name.IndexOf(excludeSubstring, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                var depth = GetDepth(root, candidate);
                if (depth < bestDepth)
                    continue;

                best = candidate;
                bestDepth = depth;
            }

            return best;
        }

        static int GetDepth(Transform root, Transform node)
        {
            var depth = 0;
            var current = node;
            while (current != null && current != root)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        Transform CreateTipAnchor(Transform part)
        {
            var existing = part.Find("MuzzleFxPoint");
            if (existing != null)
                return existing;

            var tip = new GameObject("MuzzleFxPoint").transform;
            tip.SetParent(part, false);
            tip.localRotation = Quaternion.identity;
            tip.localPosition = EstimateLocalTipOffset(part);
            return tip;
        }

        Vector3 EstimateLocalTipOffset(Transform part)
        {
            var renderers = part.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
                return Vector3.forward * 0.05f;

            var forward = weaponMount != null ? weaponMount.forward : transform.forward;
            var maxDot = float.NegativeInfinity;
            var tipWorld = part.position;

            for (var i = 0; i < renderers.Length; i++)
            {
                var bounds = renderers[i].bounds;
                var corners = GetBoundsCorners(bounds);
                for (var c = 0; c < corners.Length; c++)
                {
                    var dot = Vector3.Dot(corners[c] - part.position, forward);
                    if (dot <= maxDot)
                        continue;

                    maxDot = dot;
                    tipWorld = corners[c];
                }
            }

            // Sit slightly past the mesh tip so flash clears the barrel.
            tipWorld += forward.normalized * 0.01f;
            return part.InverseTransformPoint(tipWorld);
        }

        static Vector3[] GetBoundsCorners(Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            return new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
            };
        }

        void ApplyPose(Vector3 rotationEuler, Vector3 positionOffset)
        {
            if (weaponMount == null)
                return;

            weaponMount.localPosition = _baseLocalPosition + positionOffset;
            weaponMount.localRotation = _baseLocalRotation * Quaternion.Euler(rotationEuler);
        }
    }
}
