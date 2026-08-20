using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class MultiHitTargetPool : ObjectPool<MultiHitTargetView>
    {
        readonly MultiHitTargetView _prefab;
        readonly Transform _parent;

        public MultiHitTargetPool(MultiHitTargetView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        protected override MultiHitTargetView CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            instance.Initialize(this);
            return instance;
        }

        protected override void OnBeforeReturn(MultiHitTargetView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
        }
    }
}
