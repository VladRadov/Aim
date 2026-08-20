using System;
using UniRx;
using UnityEngine;

namespace Aim.Models
{
    public sealed class RecoilModel : IDisposable
    {
        readonly ReactiveProperty<Vector3> _rotationOffset = new(Vector3.zero);
        readonly ReactiveProperty<Vector3> _positionOffset = new(Vector3.zero);

        public IReadOnlyReactiveProperty<Vector3> RotationOffset => _rotationOffset;
        public IReadOnlyReactiveProperty<Vector3> PositionOffset => _positionOffset;

        public void ApplyKick(Vector3 rotationKick, Vector3 positionKick)
        {
            _rotationOffset.Value += rotationKick;
            _positionOffset.Value += positionKick;
        }

        public void Tick(float deltaTime, float recoverySpeed)
        {
            var t = 1f - Mathf.Exp(-recoverySpeed * deltaTime);
            _rotationOffset.Value = Vector3.Lerp(_rotationOffset.Value, Vector3.zero, t);
            _positionOffset.Value = Vector3.Lerp(_positionOffset.Value, Vector3.zero, t);
        }

        public void Dispose()
        {
            _rotationOffset.Dispose();
            _positionOffset.Dispose();
        }
    }
}
