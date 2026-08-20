using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class PrecisionCirclesLevelRunner : ILevelRunner
    {
        readonly FlyingTargetPool _pool;
        readonly List<FlyingTargetView> _active = new();
        SpawnOccupancyTracker _occupancy;
        CancellationTokenSource _cts;
        int _alive;

        public PrecisionCirclesLevelRunner(FlyingTargetPool pool)
        {
            _pool = pool;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not PrecisionCirclesLevelDefinition precisionLevel)
            {
                Debug.LogError("PrecisionCirclesLevelRunner requires PrecisionCirclesLevelDefinition.");
                return;
            }

            if (_pool == null || precisionLevel.CirclePrefab == null)
            {
                Debug.LogError("PrecisionCirclesLevelRunner: circle prefab or pool is missing.");
                return;
            }

            Stop();
            _occupancy = new SpawnOccupancyTracker(precisionLevel.MinSpawnDistance);
            _cts = new CancellationTokenSource();

            for (var i = 0; i < precisionLevel.MaxAlive; i++)
                SpawnOne(precisionLevel);

            SpawnLoopAsync(precisionLevel, _cts.Token).Forget();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (_active.Count > 0)
            {
                var snapshot = _active.ToArray();
                _active.Clear();
                for (var i = 0; i < snapshot.Length; i++)
                    snapshot[i]?.Despawn();
            }

            _alive = 0;
            _occupancy?.Clear();
            _occupancy = null;
        }

        async UniTaskVoid SpawnLoopAsync(PrecisionCirclesLevelDefinition definition, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(definition.SpawnInterval), cancellationToken: token);
                    if (_alive < definition.MaxAlive)
                        SpawnOne(definition);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        void SpawnOne(PrecisionCirclesLevelDefinition definition)
        {
            if (_pool == null || _occupancy == null)
                return;

            var center = definition.SpawnCenter;
            var halfX = definition.SpawnSize.x * 0.5f;
            var halfY = definition.SpawnSize.y * 0.5f;
            var z = UnityEngine.Random.Range(definition.NearDistance, definition.FarDistance);
            var point = new Vector3(
                center.x + UnityEngine.Random.Range(-halfX, halfX),
                center.y + UnityEngine.Random.Range(-halfY, halfY),
                z);

            var scale = UnityEngine.Random.Range(definition.ScaleMin, definition.ScaleMax);
            // Farther targets are a bit smaller.
            var distanceFactor = Mathf.InverseLerp(definition.NearDistance, definition.FarDistance, z);
            scale *= Mathf.Lerp(1f, 0.75f, distanceFactor);

            var target = _pool.Rent();
            _alive++;
            _active.Add(target);
            _occupancy.Register(target.transform);

            target.Activate(
                point,
                Vector3.up,
                0f,
                definition.LifeTime,
                scale,
                despawned =>
                {
                    _active.Remove(despawned);
                    _occupancy?.Unregister(despawned.transform);
                    _alive = Mathf.Max(0, _alive - 1);
                });
        }

        public void Dispose() => Stop();
    }
}
