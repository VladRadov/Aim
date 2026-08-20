using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class CharacterTargetPool : ObjectPool<CharacterTargetView>
    {
        readonly CharacterTargetView _prefab;
        readonly Transform _parent;

        public CharacterTargetPool(CharacterTargetView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        protected override CharacterTargetView CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.Initialize(this);
            return instance;
        }

        protected override void OnBeforeReturn(CharacterTargetView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
        }
    }
}
