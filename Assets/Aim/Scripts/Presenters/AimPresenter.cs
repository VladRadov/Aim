using System;
using Aim.Models;
using Aim.Views;
using UniRx;
using UnityEngine;

namespace Aim.Presenters
{
    public sealed class AimPresenter : IDisposable
    {
        readonly AimModel _aimModel;
        readonly InputView _inputView;
        readonly CameraLookView _cameraLookView;
        readonly WeaponView _weaponView;
        readonly float _sensitivity;
        readonly CompositeDisposable _disposables = new();

        public AimPresenter(
            AimModel aimModel,
            InputView inputView,
            CameraLookView cameraLookView,
            WeaponView weaponView,
            float sensitivity)
        {
            _aimModel = aimModel;
            _inputView = inputView;
            _cameraLookView = cameraLookView;
            _weaponView = weaponView;
            _sensitivity = sensitivity;
        }

        public void Initialize()
        {
            _inputView.LookStream
                .Subscribe(delta =>
                {
                    _aimModel.ApplyLookDelta(delta, _sensitivity);
                    ApplyAimToViews();
                })
                .AddTo(_disposables);
        }

        void ApplyAimToViews()
        {
            _cameraLookView.SetAimAngles(_aimModel.Yaw.Value, _aimModel.Pitch.Value);
            _weaponView.SetLocalRotation(Quaternion.identity);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
