using System;
using Aim.Models;
using Aim.Services;
using Aim.Views;
using UniRx;
using UnityEngine;

namespace Aim.Presenters
{
    public sealed class ShootPresenter : IDisposable
    {
        readonly ShootModel _shootModel;
        readonly ProjectileService _projectileService;
        readonly AimDirectionService _aimDirectionService;
        readonly InputView _inputView;
        readonly WeaponView _weaponView;
        readonly Camera _shootCamera;
        readonly CompositeDisposable _disposables = new();

        public ShootPresenter(
            ShootModel shootModel,
            ProjectileService projectileService,
            AimDirectionService aimDirectionService,
            InputView inputView,
            WeaponView weaponView,
            Camera shootCamera)
        {
            _shootModel = shootModel;
            _projectileService = projectileService;
            _aimDirectionService = aimDirectionService;
            _inputView = inputView;
            _weaponView = weaponView;
            _shootCamera = shootCamera;
        }

        public void Initialize()
        {
            _inputView.AttackStream
                .Subscribe(_ =>
                {
                    var origin = _weaponView.GetProjectileSpawnPosition();
                    var direction = _aimDirectionService.GetDirection(_shootCamera, origin);

                    _projectileService.Launch(origin, direction, _shootModel.RegisterShot);
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
