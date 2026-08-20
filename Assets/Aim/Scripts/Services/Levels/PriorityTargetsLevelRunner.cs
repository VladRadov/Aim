using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class PriorityTargetsLevelRunner : ILevelRunner
    {
        readonly PriorityTargetPool _pool;
        readonly List<PriorityTargetView> _active = new();
        SpawnOccupancyTracker _occupancy;
        PriorityTargetsLevelDefinition _definition;
        CancellationTokenSource _cts;
        PriorityTargetView _currentPriority;

        public PriorityTargetsLevelRunner(PriorityTargetPool pool)
        {
            _pool = pool;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not PriorityTargetsLevelDefinition priorityLevel)
            {
                Debug.LogError("PriorityTargetsLevelRunner requires PriorityTargetsLevelDefinition.");
                return;
            }

            if (_pool == null || priorityLevel.PriorityPrefab == null)
            {
                Debug.LogError("PriorityTargetsLevelRunner: priority prefab or pool is missing.");
                return;
            }

            Stop();
            _definition = priorityLevel;
            _occupancy = new SpawnOccupancyTracker(priorityLevel.MinSpawnDistance);
            _cts = new CancellationTokenSource();

            for (var i = 0; i < priorityLevel.MaxAlive; i++)
                SpawnOne();

            PickPriority(exclude: null);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _currentPriority = null;
            _definition = null;

            if (_active.Count > 0)
            {
                var snapshot = _active.ToArray();
                _active.Clear();
                for (var i = 0; i < snapshot.Length; i++)
                    snapshot[i]?.Despawn();
            }

            _occupancy?.Clear();
            _occupancy = null;
        }

        void SpawnOne()
        {
            if (_definition == null || _pool == null || _occupancy == null)
                return;

            if (!_occupancy.TryGetFreePoint(_definition, out var spawnPoint))
                spawnPoint = _definition.GetRandomSpawnPoint();

            var target = _pool.Rent();
            _active.Add(target);
            _occupancy.Register(target.transform);
            target.Activate(spawnPoint, OnDespawned);
        }

        void OnDespawned(PriorityTargetView target)
        {
            _active.Remove(target);
            _occupancy?.Unregister(target.transform);

            if (_currentPriority == target)
                _currentPriority = null;

            if (_cts == null || _cts.IsCancellationRequested || _definition == null)
                return;

            RespawnAndRetargetAsync(_cts.Token).Forget();
        }

        async UniTaskVoid RespawnAndRetargetAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_definition.RespawnDelay), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_cts == null || _cts.IsCancellationRequested || _definition == null)
                return;

            if (_active.Count < _definition.MaxAlive)
                SpawnOne();

            if (_currentPriority == null || !_currentPriority.IsActive)
                PickPriority(exclude: null);
        }

        void PickPriority(PriorityTargetView exclude)
        {
            for (var i = 0; i < _active.Count; i++)
                _active[i]?.SetPriority(false);

            if (_active.Count == 0)
            {
                _currentPriority = null;
                return;
            }

            PriorityTargetView next = null;
            var attempts = 0;
            do
            {
                next = _active[UnityEngine.Random.Range(0, _active.Count)];
                attempts++;
            } while ((next == null || !next.IsActive || next == exclude) && attempts < 24);

            if (next == null || !next.IsActive)
            {
                _currentPriority = null;
                return;
            }

            _currentPriority = next;
            next.SetPriority(true);
        }

        public void Dispose() => Stop();
    }
}
