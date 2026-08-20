using System;
using Aim.Config;
using Aim.Models;
using Aim.Presenters;
using Aim.Views;
using UnityEngine;

namespace Aim.Services
{
    public sealed class GameplayService : IDisposable
    {
        readonly AimModel _aimModel;
        readonly ShootModel _shootModel;
        readonly RecoilModel _recoilModel;
        readonly ProjectilePool _projectilePool;
        readonly ParticleFxService _fxService;
        readonly AimPresenter _aimPresenter;
        readonly ShootPresenter _shootPresenter;
        readonly CrosshairPresenter _crosshairPresenter;
        readonly RecoilPresenter _recoilPresenter;
        readonly WeaponView _weaponView;

        public AimModel AimModel => _aimModel;
        public ShootModel ShootModel => _shootModel;

        public GameplayService(
            AimTrainerConfig config,
            SessionModel session,
            SettingsModel settings,
            InputView inputView,
            CameraLookView cameraLookView,
            WeaponView weaponView,
            CrosshairView crosshairView,
            Camera shootCamera,
            Transform projectilesRoot,
            GameObject weaponPrefab)
        {
            _weaponView = weaponView;
            _aimModel = new AimModel(config.MinPitch, config.MaxPitch);
            _shootModel = new ShootModel();
            _recoilModel = new RecoilModel();

            if (config.BulletPrefab == null)
                Debug.LogWarning("GameplayService: bullet prefab is not assigned in AimTrainerConfig.");

            var fxRoot = projectilesRoot.Find("Fx") ?? new GameObject("Fx").transform;
            if (fxRoot.parent != projectilesRoot)
                fxRoot.SetParent(projectilesRoot, false);

            _projectilePool = new ProjectilePool(config.BulletPrefab, projectilesRoot);
            // Hit sparks are runtime-generated (WarFX auto-destruct was breaking pooled hit FX on targets).
            _fxService = new ParticleFxService(config.MuzzleFlashPrefab, fxRoot);

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

            _aimPresenter = new AimPresenter(_aimModel, inputView, cameraLookView, config.MouseSensitivity);
            _shootPresenter = new ShootPresenter(
                _shootModel,
                session,
                settings,
                projectileService,
                aimDirectionService,
                _fxService,
                inputView,
                weaponView,
                shootCamera);
            _crosshairPresenter = new CrosshairPresenter(_shootModel, crosshairView);
            _recoilPresenter = new RecoilPresenter(
                _recoilModel,
                _aimModel,
                weaponView,
                cameraLookView,
                _shootPresenter.AcceptedShotStream,
                config.RecoilWeaponPitch,
                config.RecoilWeaponYawRange,
                config.RecoilWeaponKickback,
                config.RecoilWeaponRollRange,
                config.RecoilRecoverySpeed,
                config.RecoilCameraPitch,
                config.RecoilCameraYawRange);

            _aimPresenter.Initialize();
            _shootPresenter.Initialize();
            _crosshairPresenter.Initialize();
            _recoilPresenter.Initialize();

            if (weaponPrefab != null)
                EquipWeaponPrefab(weaponPrefab);
        }

        public void EquipWeaponPrefab(GameObject weaponPrefab)
        {
            if (_weaponView == null || weaponPrefab == null)
                return;

            _weaponView.AttachWeapon(weaponPrefab);
            _fxService.BindMuzzle(_weaponView.MuzzlePoint);
        }

        public void Dispose()
        {
            _aimPresenter?.Dispose();
            _shootPresenter?.Dispose();
            _crosshairPresenter?.Dispose();
            _recoilPresenter?.Dispose();
            _aimModel?.Dispose();
            _shootModel?.Dispose();
            _recoilModel?.Dispose();
            _fxService?.Dispose();
            _projectilePool?.Dispose();
        }
    }
}
