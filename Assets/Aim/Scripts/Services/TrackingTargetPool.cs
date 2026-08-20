using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class TrackingTargetPool : ObjectPool<TrackingTargetView>
    {
        readonly TrackingTargetView _prefab;
        readonly Transform _parent;

        public TrackingTargetPool(TrackingTargetView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        protected override TrackingTargetView CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.Initialize(this);
            return instance;
        }

        protected override void OnBeforeReturn(TrackingTargetView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
        }
    }
}
