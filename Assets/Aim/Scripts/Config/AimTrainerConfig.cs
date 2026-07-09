using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "AimTrainerConfig", menuName = "Aim/Aim Trainer Config")]
    public class AimTrainerConfig : ScriptableObject
    {
        [Header("Look")]
        [SerializeField] float mouseSensitivity = 0.15f;
        [SerializeField] float minPitch = -80f;
        [SerializeField] float maxPitch = 80f;

        [Header("Shooting")]
        [SerializeField] float shootMaxDistance = 200f;
        [SerializeField] LayerMask targetLayerMask = ~0;
        [SerializeField] LayerMask aimPointLayerMask = ~0;
        [SerializeField] float bulletSpeed = 80f;
        [SerializeField] Views.ProjectileView bulletPrefab;

        [Header("Crosshair")]
        [SerializeField] Color crosshairDefaultColor = Color.white;
        [SerializeField] Color crosshairHitColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] float crosshairHitFlashDuration = 0.12f;

        [Header("Weapon Mount")]
        [SerializeField] Vector3 weaponMountLocalPosition = new(0.25f, -0.2f, 0.45f);

        public float MouseSensitivity => mouseSensitivity;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public float ShootMaxDistance => shootMaxDistance;
        public LayerMask TargetLayerMask => targetLayerMask;
        public LayerMask AimPointLayerMask => aimPointLayerMask;
        public float BulletSpeed => bulletSpeed;
        public Views.ProjectileView BulletPrefab => bulletPrefab;
        public Color CrosshairDefaultColor => crosshairDefaultColor;
        public Color CrosshairHitColor => crosshairHitColor;
        public float CrosshairHitFlashDuration => crosshairHitFlashDuration;
        public Vector3 WeaponMountLocalPosition => weaponMountLocalPosition;
    }
}
