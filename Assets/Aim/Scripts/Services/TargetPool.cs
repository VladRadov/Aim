using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class TargetPool : ObjectPool<TargetView>
    {
        readonly TargetView _prefab;
        readonly Transform _parent;

        public TargetPool(TargetView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        protected override TargetView CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.Initialize(this);
            return instance;
        }

        protected override void OnBeforeReturn(TargetView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
        }
    }
}
