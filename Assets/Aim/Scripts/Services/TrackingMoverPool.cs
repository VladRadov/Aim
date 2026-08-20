using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class TrackingMoverPool : ObjectPool<TrackingMoverView>
    {
        readonly TrackingMoverView _prefab;
        readonly Transform _parent;

        public TrackingMoverPool(TrackingMoverView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        protected override TrackingMoverView CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.Initialize(this);
            return instance;
        }

        protected override void OnBeforeReturn(TrackingMoverView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
        }
    }
}
