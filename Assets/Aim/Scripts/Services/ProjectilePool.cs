using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class ProjectilePool : ObjectPool<ProjectileView>
    {
        readonly ProjectileView _prefab;
        readonly Transform _parent;

        public ProjectilePool(ProjectileView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        protected override ProjectileView CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.Initialize(this);
            return instance;
        }

        protected override void OnBeforeReturn(ProjectileView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
        }
    }
}
