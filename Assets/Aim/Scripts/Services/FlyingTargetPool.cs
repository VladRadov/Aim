using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class FlyingTargetPool : ObjectPool<FlyingTargetView>
    {
        readonly FlyingTargetView _prefab;
        readonly Transform _parent;

        public FlyingTargetPool(FlyingTargetView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        protected override FlyingTargetView CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.Initialize(this);
            return instance;
        }

        protected override void OnBeforeReturn(FlyingTargetView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
        }
    }
}
