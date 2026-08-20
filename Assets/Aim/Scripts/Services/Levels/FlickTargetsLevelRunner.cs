using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class FlickTargetsLevelRunner : ILevelRunner
    {
        readonly FlyingTargetPool _pool;
        readonly List<FlyingTargetView> _active = new();
        SpawnOccupancyTracker _occupancy;
        CancellationTokenSource _cts;
        int _alive;

        public FlickTargetsLevelRunner(FlyingTargetPool pool)
        {
            _pool = pool;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not FlickTargetsLevelDefinition flickLevel)
            {
                Debug.LogError("FlickTargetsLevelRunner requires FlickTargetsLevelDefinition.");
                return;
            }

            if (_pool == null || flickLevel.FlickPrefab == null)
            {
                Debug.LogError("FlickTargetsLevelRunner: flick target prefab or pool is missing.");
                return;
            }

            Stop();
            _occupancy = new SpawnOccupancyTracker(flickLevel.MinSpawnDistance);
            _cts = new CancellationTokenSource();
            SpawnLoopAsync(flickLevel, _cts.Token).Forget();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            DespawnAll();
            _alive = 0;
            _occupancy?.Clear();
            _occupancy = null;
        }

        async UniTaskVoid SpawnLoopAsync(FlickTargetsLevelDefinition definition, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_alive < definition.MaxAlive)
                        SpawnOne(definition);

                    await UniTask.Delay(TimeSpan.FromSeconds(definition.SpawnInterval), cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        void SpawnOne(FlickTargetsLevelDefinition definition)
        {
            if (definition.FlickPrefab == null || _pool == null || _occupancy == null)
                return;

            if (!_occupancy.TryGetFreePoint(definition, out var spawnPoint))
                spawnPoint = definition.GetRandomSpawnPoint();

            var target = _pool.Rent();
            _alive++;
            _active.Add(target);
            _occupancy.Register(target.transform);

            // Stationary flash: speed 0, short random lifetime for flick + reaction.
            target.Activate(
                spawnPoint,
                Vector3.up,
                0f,
                definition.RollLifeTime(),
                despawned =>
                {
                    _active.Remove(despawned);
                    _occupancy?.Unregister(despawned.transform);
                    _alive = Mathf.Max(0, _alive - 1);
                });
        }

        void DespawnAll()
        {
            if (_active.Count == 0)
                return;

            var snapshot = _active.ToArray();
            _active.Clear();
            for (var i = 0; i < snapshot.Length; i++)
                snapshot[i]?.Despawn();
        }

        public void Dispose() => Stop();
    }
}
