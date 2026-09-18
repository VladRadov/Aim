using System;
using Aim.Config;
using Aim.Models;
using Aim.Presenters;
using Aim.Views;
using UnityEngine;
using Zenject;

namespace Aim.Services
{
    public sealed class GameplayService : MonoBehaviour, IEntityService, IDisposable
    {
        [Inject] AimTrainerConfig _config;
        [Inject] SessionModel _session;
        [Inject] SettingsModel _settings;
        [Inject] AimModel _aimModel;
        [Inject] ShootModel _shootModel;
        [Inject] RecoilModel _recoilModel;
        [Inject] InputView _inputView;
        [Inject] CameraLookView _cameraLookView;
        [Inject] WeaponView _weaponView;
        [Inject] CrosshairView _crosshairView;
        [Inject] Camera _shootCamera;
        [Inject(Id = "ProjectilesRoot")] Transform _projectilesRoot;
        [Inject] ShopModel _shopModel;
            [Inject(Id = "DefaultWeapon", Optional = true)] GameObject _defaultWeapon;

        ProjectilePool _projectilePool;
        ParticleFxService _fxService;
        AimPresenter _aimPresenter;
        ShootPresenter _shootPresenter;
        CrosshairPresenter _crosshairPresenter;
        RecoilPresenter _recoilPresenter;

        public AimModel AimModel => _aimModel;
        public ShootModel ShootModel => _shootModel;

        public void Initialize()
        {
            if (_config.BulletPrefab == null)
                Debug.LogWarning("GameplayService: bullet prefab is not assigned in AimTrainerConfig.");

            var fxRoot = _projectilesRoot.Find("Fx") ?? new GameObject("Fx").transform;
            if (fxRoot.parent != _projectilesRoot)
                fxRoot.SetParent(_projectilesRoot, false);

            _projectilePool = new ProjectilePool(_config.BulletPrefab, _projectilesRoot);
            _fxService = new ParticleFxService(_config.MuzzleFlashPrefab, fxRoot);

            var projectileService = new ProjectileService(
                _projectilePool,
                _config.BulletSpeed,
                _config.ShootMaxDistance,
                _config.TargetLayerMask);
            var aimDirectionService = new AimDirectionService(_config.ShootMaxDistance, _config.AimPointLayerMask);

            _weaponView.ConfigureMountPosition(_config.WeaponMountLocalPosition);
            _crosshairView.Configure(
                _config.CrosshairDefaultColor,
                _config.CrosshairHitColor,
                _config.CrosshairHitFlashDuration);

            _aimPresenter = new AimPresenter(_aimModel, _inputView, _cameraLookView, _config.MouseSensitivity);
            _shootPresenter = new ShootPresenter(
                _shootModel,
                _session,
                _settings,
                projectileService,
                aimDirectionService,
                _fxService,
                _inputView,
                _weaponView,
                _shootCamera);
            _crosshairPresenter = new CrosshairPresenter(_shootModel, _crosshairView);
            _recoilPresenter = new RecoilPresenter(
                _recoilModel,
                _aimModel,
                _weaponView,
                _cameraLookView,
                _shootPresenter.AcceptedShotStream,
                _config.RecoilWeaponPitch,
                _config.RecoilWeaponYawRange,
                _config.RecoilWeaponKickback,
                _config.RecoilWeaponRollRange,
                _config.RecoilRecoverySpeed,
                _config.RecoilCameraPitch,
                _config.RecoilCameraYawRange);

            _aimPresenter.Initialize();
            _shootPresenter.Initialize();
            _crosshairPresenter.Initialize();
            _recoilPresenter.Initialize();

            var startingWeapon = ResolveStartingWeaponPrefab();
            if (startingWeapon != null)
                EquipWeaponPrefab(startingWeapon);
        }

        public void EquipWeaponPrefab(GameObject weaponPrefab)
        {
            if (_weaponView == null || weaponPrefab == null)
                return;

            _weaponView.AttachWeapon(weaponPrefab);
            _fxService.BindMuzzle(_weaponView.MuzzlePoint);
        }

        public void EquipWeaponById(string weaponId)
        {
            var entry = _shopModel?.FindById(weaponId);
            if (entry?.Prefab == null)
                return;

            EquipWeaponPrefab(entry.Prefab);
        }

        GameObject ResolveStartingWeaponPrefab()
        {
            var equipped = _shopModel?.GetEquippedEntry();
            if (equipped?.Prefab != null)
                return equipped.Prefab;
            return _defaultWeapon;
        }

        void OnDestroy() => Dispose();

        public void Dispose()
        {
            _aimPresenter?.Dispose();
            _shootPresenter?.Dispose();
            _crosshairPresenter?.Dispose();
            _recoilPresenter?.Dispose();
            _fxService?.Dispose();
            _projectilePool?.Dispose();
        }
    }
}
