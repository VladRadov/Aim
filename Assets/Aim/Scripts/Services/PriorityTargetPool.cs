using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class PriorityTargetPool : ObjectPool<PriorityTargetView>
    {
        readonly PriorityTargetView _prefab;
        readonly Transform _parent;

        public PriorityTargetPool(PriorityTargetView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        protected override PriorityTargetView CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            instance.Initialize(this);
            return instance;
        }

        protected override void OnBeforeReturn(PriorityTargetView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
        }
    }
}
