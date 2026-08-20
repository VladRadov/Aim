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
        readonly float _sensitivity;
        readonly CompositeDisposable _disposables = new();

        public AimPresenter(
            AimModel aimModel,
            InputView inputView,
            CameraLookView cameraLookView,
            float sensitivity)
        {
            _aimModel = aimModel;
            _inputView = inputView;
            _cameraLookView = cameraLookView;
            _sensitivity = sensitivity;
        }

        public void Initialize()
        {
            _inputView.LookStream
                .Subscribe(delta =>
                {
                    _aimModel.ApplyLookDelta(delta, _sensitivity);
                    _cameraLookView.SetAimAngles(_aimModel.Yaw.Value, _aimModel.Pitch.Value);
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
