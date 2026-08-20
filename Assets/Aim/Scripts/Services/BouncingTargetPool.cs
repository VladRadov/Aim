using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class BouncingTargetPool : ObjectPool<BouncingTargetView>
    {
        readonly BouncingTargetView _prefab;
        readonly Transform _parent;

        public BouncingTargetPool(BouncingTargetView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        protected override BouncingTargetView CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.Initialize(this);
            return instance;
        }

        protected override void OnBeforeReturn(BouncingTargetView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
        }
    }
}
