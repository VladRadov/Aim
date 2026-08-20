using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class PeekTargetPool : ObjectPool<PeekTargetView>
    {
        readonly PeekTargetView _prefab;
        readonly Transform _parent;

        public PeekTargetPool(PeekTargetView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        protected override PeekTargetView CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            instance.Initialize(this);
            return instance;
        }

        protected override void OnBeforeReturn(PeekTargetView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
        }
    }
}
