using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class StaticBallsLevelRunner : ILevelRunner
    {
        readonly FlyingTargetPool _pool;
        readonly List<FlyingTargetView> _active = new();
        SpawnOccupancyTracker _occupancy;
        CancellationTokenSource _cts;
        int _alive;

        public StaticBallsLevelRunner(FlyingTargetPool pool)
        {
            _pool = pool;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not StaticBallsLevelDefinition staticLevel)
            {
                Debug.LogError("StaticBallsLevelRunner requires StaticBallsLevelDefinition.");
                return;
            }

            if (_pool == null || staticLevel.StaticPrefab == null)
            {
                Debug.LogError("StaticBallsLevelRunner: flying/static target prefab or pool is missing.");
                return;
            }

            Stop();
            _occupancy = new SpawnOccupancyTracker(staticLevel.MinSpawnDistance);
            _cts = new CancellationTokenSource();

            for (var i = 0; i < staticLevel.MaxAlive; i++)
                SpawnOne(staticLevel);

            SpawnLoopAsync(staticLevel, _cts.Token).Forget();
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

        async UniTaskVoid SpawnLoopAsync(StaticBallsLevelDefinition definition, CancellationToken token)
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

        void SpawnOne(StaticBallsLevelDefinition definition)
        {
            if (definition.StaticPrefab == null || _pool == null || _occupancy == null)
                return;

            if (!_occupancy.TryGetFreePoint(definition, out var spawnPoint))
                spawnPoint = definition.GetRandomSpawnPoint();

            var target = _pool.Rent();
            _alive++;
            _active.Add(target);
            _occupancy.Register(target.transform);

            // Speed 0 => stays in place; lifetime still despawns if not shot.
            target.Activate(
                spawnPoint,
                Vector3.up,
                0f,
                definition.LifeTime,
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
