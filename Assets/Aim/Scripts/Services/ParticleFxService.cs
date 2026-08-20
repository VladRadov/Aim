using System;
using System.Collections.Generic;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services
{
    public sealed class ParticleFxService : IDisposable
    {
        const float HitFxDuration = 0.55f;

        readonly ParticleFxView _muzzlePrefab;
        readonly Transform _hitFxRoot;
        readonly Queue<ParticleSystem> _hitPool = new();

        ParticleFxView _muzzleInstance;

        public ParticleFxService(ParticleFxView muzzlePrefab, Transform hitFxRoot)
        {
            _muzzlePrefab = muzzlePrefab;
            _hitFxRoot = hitFxRoot;
        }

        public void BindMuzzle(Transform muzzlePoint)
        {
            ClearMuzzle();

            if (_muzzlePrefab == null || muzzlePoint == null)
                return;

            // Spawn under an inactive holder so Awake/PlayOnAwake cannot fire during Instantiate.
            var spawnHolder = new GameObject("MuzzleFxSpawn");
            spawnHolder.SetActive(false);
            spawnHolder.transform.SetParent(muzzlePoint, false);

            _muzzleInstance = UnityEngine.Object.Instantiate(_muzzlePrefab, spawnHolder.transform);
            _muzzleInstance.transform.localPosition = Vector3.zero;
            _muzzleInstance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            _muzzleInstance.transform.localScale = Vector3.one;
            _muzzleInstance.InitializeEmbedded();

            _muzzleInstance.transform.SetParent(muzzlePoint, false);
            _muzzleInstance.transform.localPosition = Vector3.zero;
            _muzzleInstance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            _muzzleInstance.transform.localScale = Vector3.one;
            _muzzleInstance.gameObject.SetActive(false);

            UnityEngine.Object.Destroy(spawnHolder);
        }

        public void PlayMuzzle()
        {
            _muzzleInstance?.PlayInPlace();
        }

        public void PlayHit(Vector3 position, Vector3 normal, Camera viewCamera = null)
        {
            var point = position;
            if (normal.sqrMagnitude > 0.0001f)
                point += normal.normalized * 0.2f;

            if (viewCamera != null)
            {
                var toCamera = viewCamera.transform.position - point;
                if (toCamera.sqrMagnitude > 0.0001f)
                    point += toCamera.normalized * 0.15f;
            }

            var spark = RentHitSpark();
            var sparkTransform = spark.transform;
            sparkTransform.SetParent(null, false);
            sparkTransform.position = point;
            sparkTransform.rotation = Quaternion.identity;
            spark.gameObject.SetActive(true);
            spark.Clear(true);
            spark.Play(true);
            ReturnHitSparkAsync(spark).Forget();
        }

        public void ClearMuzzle()
        {
            if (_muzzleInstance == null)
                return;

            UnityEngine.Object.Destroy(_muzzleInstance.gameObject);
            _muzzleInstance = null;
        }

        public void Dispose()
        {
            ClearMuzzle();

            while (_hitPool.Count > 0)
            {
                var spark = _hitPool.Dequeue();
                if (spark != null)
                    UnityEngine.Object.Destroy(spark.gameObject);
            }
        }

        ParticleSystem RentHitSpark()
        {
            while (_hitPool.Count > 0)
            {
                var spark = _hitPool.Dequeue();
                if (spark != null)
                    return spark;
            }

            return CreateHitSpark();
        }

        async UniTaskVoid ReturnHitSparkAsync(ParticleSystem spark)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(HitFxDuration));
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (spark == null)
                return;

            spark.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            spark.Clear(true);
            spark.gameObject.SetActive(false);
            if (_hitFxRoot != null)
                spark.transform.SetParent(_hitFxRoot, false);
            _hitPool.Enqueue(spark);
        }

        ParticleSystem CreateHitSpark()
        {
            var go = new GameObject("HitSpark");
            if (_hitFxRoot != null)
                go.transform.SetParent(_hitFxRoot, false);

            var spark = go.AddComponent<ParticleSystem>();
            go.SetActive(false);

            var main = spark.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = 0.35f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.85f, 0.25f, 1f),
                new Color(1f, 0.35f, 0.1f, 1f));
            main.gravityModifier = 0.6f;
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = spark.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18, 28) });

            var shape = spark.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var colorOverLifetime = spark.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = spark.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateSparkMaterial();

            return spark;
        }

        static Material CreateSparkMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Additive")
                ?? Shader.Find("Sprites/Default");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            return material;
        }
    }
}
