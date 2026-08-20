using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Views
{
    public sealed class ParticleFxView : MonoBehaviour
    {
        const float DefaultFallbackSeconds = 1.25f;
        const float EmbeddedMaxSeconds = 0.45f;

        [SerializeField] ParticleSystem particleRoot;

        Action<ParticleFxView> _returnToPool;
        Transform _poolParent;
        CancellationTokenSource _lifetimeCts;
        bool _embedded;

        public void Initialize(Action<ParticleFxView> returnToPool, Transform poolParent)
        {
            _returnToPool = returnToPool;
            _poolParent = poolParent;
            _embedded = false;
            CacheParticleRoot();
            DisablePlayOnAwake();
            StopAllEffects();
        }

        public void InitializeEmbedded()
        {
            _returnToPool = null;
            _poolParent = null;
            _embedded = true;
            CacheParticleRoot();
            DisablePlayOnAwake();
            ForceNonLooping();
            StopAllEffects();
            gameObject.SetActive(false);
        }

        public void Play(Vector3 position, Quaternion rotation)
        {
            CancelLifetime();
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, rotation);
            BeginPlay(ParticleSystemSimulationSpace.World);
        }

        public void PlayInPlace()
        {
            CancelLifetime();
            BeginPlay(ParticleSystemSimulationSpace.Local);
        }

        public void PlayAttachedLoop()
        {
            CancelLifetime();
            ApplySimulationSpace(ParticleSystemSimulationSpace.Local);
            gameObject.SetActive(true);
            SetLightsEnabled(true);

            var systems = GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                systems[i].Clear(true);
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].Play(true);
            }
        }

        public void StopAttachedLoop()
        {
            CancelLifetime();
            StopAllEffects();
            gameObject.SetActive(false);
        }

        public void ResetState()
        {
            CancelLifetime();
            StopAllEffects();
            if (!_embedded)
                RestorePoolParent();
        }

        void BeginPlay(ParticleSystemSimulationSpace simulationSpace)
        {
            ApplySimulationSpace(simulationSpace);
            ApplyAlwaysSimulate();
            if (_embedded)
                ForceNonLooping();

            gameObject.SetActive(true);
            SetLightsEnabled(true);

            var systems = GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                systems[i].Clear(true);
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].Play(true);
            }

            var duration = ResolveDuration();
            if (_embedded)
            {
                duration = Mathf.Min(duration, EmbeddedMaxSeconds);
                StopAfterAsync(duration).Forget();
            }
            else
            {
                ReturnAfterAsync(duration).Forget();
            }
        }

        void CacheParticleRoot()
        {
            if (particleRoot == null)
                particleRoot = GetComponent<ParticleSystem>() ?? GetComponentInChildren<ParticleSystem>(true);
        }

        void DisablePlayOnAwake()
        {
            var systems = GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                main.playOnAwake = false;
            }
        }

        void ForceNonLooping()
        {
            var systems = GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                main.loop = false;
            }
        }

        void ApplySimulationSpace(ParticleSystemSimulationSpace simulationSpace)
        {
            var systems = GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                main.simulationSpace = simulationSpace;
            }
        }

        void ApplyAlwaysSimulate()
        {
            var systems = GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                main.playOnAwake = false;
            }
        }

        void StopAllEffects()
        {
            var systems = GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].Clear(true);
            }

            SetLightsEnabled(false);
        }

        void SetLightsEnabled(bool enabled)
        {
            var lights = GetComponentsInChildren<Light>(true);
            for (var i = 0; i < lights.Length; i++)
                lights[i].enabled = enabled;
        }

        void RestorePoolParent()
        {
            if (_poolParent != null)
                transform.SetParent(_poolParent, false);
        }

        float ResolveDuration()
        {
            if (particleRoot == null)
                return DefaultFallbackSeconds;

            var mainModule = particleRoot.main;
            var total = mainModule.duration;
            var startLifetime = mainModule.startLifetime;

            switch (startLifetime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    total += startLifetime.constant;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    total += startLifetime.constantMax;
                    break;
                default:
                    total += DefaultFallbackSeconds * 0.5f;
                    break;
            }

            if (mainModule.loop)
                return Mathf.Max(DefaultFallbackSeconds, 0.35f);

            return Mathf.Max(0.2f, total);
        }

        async UniTaskVoid ReturnAfterAsync(float seconds)
        {
            _lifetimeCts = new CancellationTokenSource();
            var token = _lifetimeCts.Token;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (this == null)
                return;

            RestorePoolParent();
            _returnToPool?.Invoke(this);
        }

        async UniTaskVoid StopAfterAsync(float seconds)
        {
            _lifetimeCts = new CancellationTokenSource();
            var token = _lifetimeCts.Token;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            FinishEmbeddedPlayback();
        }

        void FinishEmbeddedPlayback()
        {
            StopAllEffects();
            gameObject.SetActive(false);
        }

        void CancelLifetime()
        {
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }
    }
}
