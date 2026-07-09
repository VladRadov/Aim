using System;
using Aim.Config;
using Aim.Models;
using Aim.Presenters;
using Aim.Services;
using Aim.Views;
using UnityEngine;

namespace Aim.Controllers
{
    public sealed class AimTrainerController : MonoBehaviour
    {
        [SerializeField] AimTrainerConfig config;
        [SerializeField] InputView inputView;
        [SerializeField] CameraLookView cameraLookView;
        [SerializeField] WeaponView weaponView;
        [SerializeField] CrosshairView crosshairView;
        [SerializeField] Camera shootCamera;
        [SerializeField] Transform projectilesRoot;
        [SerializeField] GameObject weaponPrefab;

        AimModel _aimModel;
        ShootModel _shootModel;
        ProjectilePool _projectilePool;
        AimPresenter _aimPresenter;
        ShootPresenter _shootPresenter;
        CrosshairPresenter _crosshairPresenter;

        void Awake()
        {
            if (shootCamera == null)
                shootCamera = Camera.main;

            if (projectilesRoot == null)
            {
                var rootGo = new GameObject("Projectiles");
                projectilesRoot = rootGo.transform;
            }

            _aimModel = new AimModel(config.MinPitch, config.MaxPitch);
            _shootModel = new ShootModel();

            var bulletPrefab = config.BulletPrefab;
            if (bulletPrefab == null)
                Debug.LogWarning("AimTrainerController: bullet prefab is not assigned in AimTrainerConfig.");

            _projectilePool = new ProjectilePool(bulletPrefab, projectilesRoot);
            var projectileService = new ProjectileService(
                _projectilePool,
                config.BulletSpeed,
                config.ShootMaxDistance,
                config.TargetLayerMask);

            var aimDirectionService = new AimDirectionService(config.ShootMaxDistance, config.AimPointLayerMask);

            weaponView.ConfigureMountPosition(config.WeaponMountLocalPosition);
            crosshairView.Configure(
                config.CrosshairDefaultColor,
                config.CrosshairHitColor,
                config.CrosshairHitFlashDuration);

            _aimPresenter = new AimPresenter(
                _aimModel,
                inputView,
                cameraLookView,
                weaponView,
                config.MouseSensitivity);

            _shootPresenter = new ShootPresenter(
                _shootModel,
                projectileService,
                aimDirectionService,
                inputView,
                weaponView,
                shootCamera);

            _crosshairPresenter = new CrosshairPresenter(_shootModel, crosshairView);

            _aimPresenter.Initialize();
            _shootPresenter.Initialize();
            _crosshairPresenter.Initialize();

            if (weaponPrefab != null)
                weaponView.AttachWeapon(weaponPrefab);
        }

        void OnDestroy()
        {
            _aimPresenter?.Dispose();
            _shootPresenter?.Dispose();
            _crosshairPresenter?.Dispose();
            _aimModel?.Dispose();
            _shootModel?.Dispose();
            _projectilePool?.Dispose();
        }
    }
}
