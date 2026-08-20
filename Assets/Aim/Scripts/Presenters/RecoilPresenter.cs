using System;
using Aim.Models;
using Aim.Views;
using UniRx;
using UnityEngine;

namespace Aim.Presenters
{
    public sealed class RecoilPresenter : IDisposable
    {
        readonly RecoilModel _recoilModel;
        readonly AimModel _aimModel;
        readonly WeaponView _weaponView;
        readonly CameraLookView _cameraLookView;
        readonly IObservable<Unit> _shotStream;
        readonly float _weaponPitch;
        readonly float _weaponYawRange;
        readonly float _weaponKickback;
        readonly float _weaponRollRange;
        readonly float _recoverySpeed;
        readonly float _cameraPitch;
        readonly float _cameraYawRange;
        readonly CompositeDisposable _disposables = new();

        public RecoilPresenter(
            RecoilModel recoilModel,
            AimModel aimModel,
            WeaponView weaponView,
            CameraLookView cameraLookView,
            IObservable<Unit> shotStream,
            float weaponPitch,
            float weaponYawRange,
            float weaponKickback,
            float weaponRollRange,
            float recoverySpeed,
            float cameraPitch,
            float cameraYawRange)
        {
            _recoilModel = recoilModel;
            _aimModel = aimModel;
            _weaponView = weaponView;
            _cameraLookView = cameraLookView;
            _shotStream = shotStream;
            _weaponPitch = weaponPitch;
            _weaponYawRange = weaponYawRange;
            _weaponKickback = weaponKickback;
            _weaponRollRange = weaponRollRange;
            _recoverySpeed = recoverySpeed;
            _cameraPitch = cameraPitch;
            _cameraYawRange = cameraYawRange;
        }

        public void Initialize()
        {
            _shotStream
                .Subscribe(_ => ApplyKick())
                .AddTo(_disposables);

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    _recoilModel.Tick(Time.deltaTime, _recoverySpeed);
                    _weaponView.SetRecoilOffset(
                        _recoilModel.RotationOffset.Value,
                        _recoilModel.PositionOffset.Value);
                })
                .AddTo(_disposables);
        }

        void ApplyKick()
        {
            var yaw = UnityEngine.Random.Range(-_weaponYawRange, _weaponYawRange);
            var roll = UnityEngine.Random.Range(-_weaponRollRange, _weaponRollRange);

            _recoilModel.ApplyKick(
                new Vector3(-_weaponPitch, yaw, roll),
                new Vector3(0f, 0f, -_weaponKickback));

            var cameraYaw = UnityEngine.Random.Range(-_cameraYawRange, _cameraYawRange);
            _aimModel.ApplyRecoilKick(_cameraPitch, cameraYaw);
            _cameraLookView.SetAimAngles(_aimModel.Yaw.Value, _aimModel.Pitch.Value);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
