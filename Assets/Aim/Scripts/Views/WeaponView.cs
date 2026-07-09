using UnityEngine;

namespace Aim.Views
{
    public sealed class WeaponView : MonoBehaviour
    {
        [SerializeField] Transform weaponMount;

        public Transform Mount => weaponMount;

        public Vector3 GetProjectileSpawnPosition()
        {
            return weaponMount != null ? weaponMount.position : transform.position;
        }

        public void ConfigureMountPosition(Vector3 localPosition)
        {
            weaponMount.localPosition = localPosition;
        }

        public void SetLocalRotation(Quaternion localRotation)
        {
            weaponMount.localRotation = localRotation;
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
        }
    }
}
