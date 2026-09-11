using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class DoubleTapLevelRunner : ILevelRunner
    {
        readonly MultiHitTargetPool _pool;
        readonly Camera _aimCamera;
        readonly List<MultiHitTargetView> _active = new();
        SpawnOccupancyTracker _occupancy;
        CancellationTokenSource _cts;
        int _alive;

        public DoubleTapLevelRunner(MultiHitTargetPool pool, Camera aimCamera)
        {
            _pool = pool;
            _aimCamera = aimCamera;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not DoubleTapLevelDefinition doubleTapLevel)
            {
                Debug.LogError("DoubleTapLevelRunner requires DoubleTapLevelDefinition.");
                return;
            }

            if (_pool == null || doubleTapLevel.MultiHitPrefab == null)
            {
                Debug.LogError("DoubleTapLevelRunner: multi-hit prefab or pool is missing.");
                return;
            }

            Stop();
            _occupancy = new SpawnOccupancyTracker(doubleTapLevel.MinSpawnDistance);
            _cts = new CancellationTokenSource();

            for (var i = 0; i < doubleTapLevel.MaxAlive; i++)
                SpawnOne(doubleTapLevel);

            SpawnLoopAsync(doubleTapLevel, _cts.Token).Forget();
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

        async UniTaskVoid SpawnLoopAsync(DoubleTapLevelDefinition definition, CancellationToken token)
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

        void SpawnOne(DoubleTapLevelDefinition definition)
        {
            if (_pool == null || _occupancy == null)
                return;

            if (!_occupancy.TryGetFreePoint(definition, out var spawnPoint))
                spawnPoint = definition.GetRandomSpawnPoint();

            var target = _pool.Rent();
            _alive++;
            _active.Add(target);
            _occupancy.Register(target.transform);

            target.Activate(
                spawnPoint,
                definition.HitPoints,
                definition.LifeTime,
                _aimCamera,
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
