using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Aim.Models;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class FlyingObjectsLevelRunner : ILevelRunner
    {
        readonly FlyingTargetPool _pool;
        readonly List<FlyingTargetView> _active = new();
        SpawnOccupancyTracker _occupancy;
        CancellationTokenSource _cts;
        int _alive;

        public FlyingObjectsLevelRunner(FlyingTargetPool pool)
        {
            _pool = pool;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not FlyingObjectsLevelDefinition flyingLevel)
            {
                Debug.LogError("FlyingObjectsLevelRunner requires FlyingObjectsLevelDefinition.");
                return;
            }

            Stop();
            _occupancy = new SpawnOccupancyTracker(flyingLevel.MinSpawnDistance);
            _cts = new CancellationTokenSource();
            SpawnLoopAsync(flyingLevel, _cts.Token).Forget();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            DespawnAllActive();

            _alive = 0;
            _occupancy?.Clear();
            _occupancy = null;
        }

        void DespawnAllActive()
        {
            if (_active.Count == 0)
                return;

            var snapshot = _active.ToArray();
            _active.Clear();

            for (var i = 0; i < snapshot.Length; i++)
            {
                var target = snapshot[i];
                if (target != null)
                    target.Despawn();
            }
        }

        async UniTaskVoid SpawnLoopAsync(FlyingObjectsLevelDefinition definition, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_alive < definition.MaxFlyingAlive)
                        SpawnOne(definition);

                    await UniTask.Delay(TimeSpan.FromSeconds(definition.FlyingSpawnInterval), cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        void SpawnOne(FlyingObjectsLevelDefinition definition)
        {
            if (definition.FlyingPrefab == null || _pool == null || _occupancy == null)
                return;

            if (!_occupancy.TryGetFreePoint(definition, out var spawnPoint))
                return;

            var target = _pool.Rent();
            _alive++;
            _active.Add(target);
            _occupancy.Register(target.transform);

            var speed = UnityEngine.Random.Range(definition.FlyingSpeedMin, definition.FlyingSpeedMax);
            target.Activate(
                spawnPoint,
                ResolveDirection(definition.FlyingDirection),
                speed,
                definition.FlyingLifetime,
                despawned =>
                {
                    _active.Remove(despawned);
                    _occupancy?.Unregister(despawned.transform);
                    _alive = Mathf.Max(0, _alive - 1);
                });
        }

        static Vector3 ResolveDirection(FlyDirection direction)
        {
            var resolved = direction == FlyDirection.Random
                ? (FlyDirection)UnityEngine.Random.Range(0, 4)
                : direction;

            return resolved switch
            {
                FlyDirection.Up => Vector3.up,
                FlyDirection.Down => Vector3.down,
                FlyDirection.Left => Vector3.left,
                FlyDirection.Right => Vector3.right,
                _ => Vector3.up
            };
        }

        public void Dispose() => Stop();
    }
}
