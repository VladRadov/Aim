using System;
using UniRx;
using UnityEngine;

namespace Aim.Models
{
    public sealed class AimModel : IDisposable
    {
        readonly ReactiveProperty<float> _yaw = new(0f);
        readonly ReactiveProperty<float> _pitch = new(0f);
        readonly float _minPitch;
        readonly float _maxPitch;

        public IReadOnlyReactiveProperty<float> Yaw => _yaw;
        public IReadOnlyReactiveProperty<float> Pitch => _pitch;

        public AimModel(float minPitch, float maxPitch)
        {
            _minPitch = minPitch;
            _maxPitch = maxPitch;
        }

        public void ApplyLookDelta(Vector2 delta, float sensitivity)
        {
            _yaw.Value += delta.x * sensitivity;
            _pitch.Value = Mathf.Clamp(_pitch.Value - delta.y * sensitivity, _minPitch, _maxPitch);
        }

        public void Dispose()
        {
            _yaw.Dispose();
            _pitch.Dispose();
        }
    }
}
