using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class MovingRailsLevelRunner : ILevelRunner
    {
        readonly FlyingTargetPool _pool;
        readonly List<FlyingTargetView> _active = new();
        CancellationTokenSource _cts;
        int _alive;

        public MovingRailsLevelRunner(FlyingTargetPool pool)
        {
            _pool = pool;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not MovingRailsLevelDefinition railsLevel)
            {
                Debug.LogError("MovingRailsLevelRunner requires MovingRailsLevelDefinition.");
                return;
            }

            if (_pool == null || railsLevel.RailPrefab == null)
            {
                Debug.LogError("MovingRailsLevelRunner: rail prefab or pool is missing.");
                return;
            }

            Stop();
            _cts = new CancellationTokenSource();
            SpawnLoopAsync(railsLevel, _cts.Token).Forget();
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
        }

        async UniTaskVoid SpawnLoopAsync(MovingRailsLevelDefinition definition, CancellationToken token)
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

        void SpawnOne(MovingRailsLevelDefinition definition)
        {
            var railIndex = UnityEngine.Random.Range(0, definition.RailCount);
            var center = definition.SpawnCenter;
            var halfWidth = definition.RailWidth * 0.5f;
            var y = center.y - (definition.RailCount - 1) * definition.RailSpacingY * 0.5f
                    + railIndex * definition.RailSpacingY;

            var left = new Vector3(center.x - halfWidth, y, center.z);
            var right = new Vector3(center.x + halfWidth, y, center.z);
            var startFromLeft = UnityEngine.Random.value > 0.5f;
            var start = startFromLeft ? left : right;
            var end = startFromLeft ? right : left;
            var speed = UnityEngine.Random.Range(definition.SpeedMin, definition.SpeedMax);

            var target = _pool.Rent();
            _alive++;
            _active.Add(target);

            target.ActivateRail(
                start,
                end,
                speed,
                definition.Lifetime,
                despawned =>
                {
                    _active.Remove(despawned);
                    _alive = Mathf.Max(0, _alive - 1);
                });
        }

        public void Dispose() => Stop();
    }
}
