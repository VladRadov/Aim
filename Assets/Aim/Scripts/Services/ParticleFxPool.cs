using Aim.Views;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Services
{
    public sealed class ParticleFxPool : ObjectPool<ParticleFxView>
    {
        readonly ParticleFxView _prefab;
        readonly Transform _parent;

        public ParticleFxPool(ParticleFxView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        public bool HasPrefab => _prefab != null;

        protected override ParticleFxView CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            StripExternalAutoLifetimeHandlers(instance.gameObject);
            instance.Initialize(Return, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        protected override void OnBeforeRent(ParticleFxView instance)
        {
            // Keep inactive until Play/PlayAttached places the FX (avoids first flash at pool root).
        }

        protected override void OnBeforeReturn(ParticleFxView instance)
        {
            base.OnBeforeReturn(instance);
            instance.ResetState();
            instance.gameObject.SetActive(false);
        }

        static void StripExternalAutoLifetimeHandlers(GameObject root)
        {
            // WarFX CFX_AutoDestructShuriken destroys pooled instances (OnlyDeactivate=0).
            // Lifetime is owned by ParticleFxView instead.
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                var typeName = behaviour.GetType().Name;
                if (typeName == "CFX_AutoDestructShuriken")
                    Object.Destroy(behaviour);
            }
        }
    }
}
