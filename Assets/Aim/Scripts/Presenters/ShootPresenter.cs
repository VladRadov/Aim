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
        readonly SessionModel _sessionModel;
        readonly SettingsModel _settingsModel;
        readonly ProjectileService _projectileService;
        readonly AimDirectionService _aimDirectionService;
        readonly ParticleFxService _fxService;
        readonly AudioSettingsService _audioService;
        readonly InputView _inputView;
        readonly WeaponView _weaponView;
        readonly Camera _shootCamera;
        readonly Subject<Unit> _acceptedShotSubject = new();
        readonly CompositeDisposable _disposables = new();

        public IObservable<Unit> AcceptedShotStream => _acceptedShotSubject;

        public ShootPresenter(
            ShootModel shootModel,
            SessionModel sessionModel,
            SettingsModel settingsModel,
            ProjectileService projectileService,
            AimDirectionService aimDirectionService,
            ParticleFxService fxService,
            AudioSettingsService audioService,
            InputView inputView,
            WeaponView weaponView,
            Camera shootCamera)
        {
            _shootModel = shootModel;
            _sessionModel = sessionModel;
            _settingsModel = settingsModel;
            _projectileService = projectileService;
            _aimDirectionService = aimDirectionService;
            _fxService = fxService;
            _audioService = audioService;
            _inputView = inputView;
            _weaponView = weaponView;
            _shootCamera = shootCamera;
        }

        public void Initialize()
        {
            _inputView.AttackStream
                .Subscribe(_ => TryShoot())
                .AddTo(_disposables);
        }

        void TryShoot()
        {
            if (_settingsModel.IsOpen.Value)
                return;

            if (!_sessionModel.IsPlaying)
                return;

            if (!_sessionModel.AllowsShooting.Value)
                return;

            if (!_sessionModel.TryConsumeAmmo())
                return;

            _acceptedShotSubject.OnNext(Unit.Default);
            _audioService?.PlayShoot();

            var origin = _weaponView.GetProjectileSpawnPosition();
            var direction = _aimDirectionService.GetDirection(_shootCamera, origin);

            _fxService.PlayMuzzle();

            _projectileService.Launch(origin, direction, result =>
            {
                // Hit FX first, then target reaction — otherwise ball despawn/culling can hide particles.
                if (result.HasImpact)
                    _fxService.PlayHit(result.HitPoint, result.HitNormal, _shootCamera);

                HitResolver.ApplyOnHit(result);

                _shootModel.RegisterShot(result);
                if (result.CountsAsScore)
                    _audioService?.PlayHit();
                else
                    _audioService?.PlayMiss();

                if (result.CountsAsScore)
                    _sessionModel.RegisterScoreHit();

                _sessionModel.EvaluateAfterShotResolved();
            });
        }

        public void Dispose()
        {
            _acceptedShotSubject.Dispose();
            _disposables.Dispose();
        }
    }
}
