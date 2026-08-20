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

        [Header("VFX")]
        [SerializeField] Views.ParticleFxView muzzleFlashPrefab;
        [SerializeField] Views.ParticleFxView hitEffectPrefab;
        [SerializeField] float muzzleFxFallbackDuration = 0.6f;
        [SerializeField] float hitFxFallbackDuration = 1.2f;

        [Header("Crosshair")]
        [SerializeField] Color crosshairDefaultColor = Color.white;
        [SerializeField] Color crosshairHitColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] float crosshairHitFlashDuration = 0.12f;

        [Header("Weapon Mount")]
        [SerializeField] Vector3 weaponMountLocalPosition = new(0.25f, -0.2f, 0.45f);

        [Header("Recoil")]
        [SerializeField] float recoilWeaponPitch = 3.5f;
        [SerializeField] float recoilWeaponYawRange = 1.2f;
        [SerializeField] float recoilWeaponRollRange = 1.5f;
        [SerializeField] float recoilWeaponKickback = 0.035f;
        [SerializeField] float recoilRecoverySpeed = 14f;
        [SerializeField] float recoilCameraPitch = 0.55f;
        [SerializeField] float recoilCameraYawRange = 0.2f;

        [Header("Coins")]
        [SerializeField] int coinsPerHit = 1;
        [SerializeField] int coinsWinBonus = 10;

        [Header("Levels")]
        [SerializeField] LevelDefinition[] levels;

        public float MouseSensitivity => mouseSensitivity;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public float ShootMaxDistance => shootMaxDistance;
        public LayerMask TargetLayerMask => targetLayerMask;
        public LayerMask AimPointLayerMask => aimPointLayerMask;
        public float BulletSpeed => bulletSpeed;
        public Views.ProjectileView BulletPrefab => bulletPrefab;
        public Views.ParticleFxView MuzzleFlashPrefab => muzzleFlashPrefab;
        public Views.ParticleFxView HitEffectPrefab => hitEffectPrefab;
        public float MuzzleFxFallbackDuration => muzzleFxFallbackDuration;
        public float HitFxFallbackDuration => hitFxFallbackDuration;
        public Color CrosshairDefaultColor => crosshairDefaultColor;
        public Color CrosshairHitColor => crosshairHitColor;
        public float CrosshairHitFlashDuration => crosshairHitFlashDuration;
        public Vector3 WeaponMountLocalPosition => weaponMountLocalPosition;
        public float RecoilWeaponPitch => recoilWeaponPitch;
        public float RecoilWeaponYawRange => recoilWeaponYawRange;
        public float RecoilWeaponRollRange => recoilWeaponRollRange;
        public float RecoilWeaponKickback => recoilWeaponKickback;
        public float RecoilRecoverySpeed => recoilRecoverySpeed;
        public float RecoilCameraPitch => recoilCameraPitch;
        public float RecoilCameraYawRange => recoilCameraYawRange;
        public int CoinsPerHit => coinsPerHit;
        public int CoinsWinBonus => coinsWinBonus;
        public LevelDefinition[] Levels => levels;
    }
}
