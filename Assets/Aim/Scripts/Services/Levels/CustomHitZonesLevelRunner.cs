using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Aim.Services;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class CustomHitZonesLevelRunner : ILevelRunner
    {
        readonly Transform _root;
        readonly List<CustomTargetView> _active = new();
        SpawnOccupancyTracker _occupancy;
        CancellationTokenSource _cts;

        public CustomHitZonesLevelRunner(Transform root)
        {
            _root = root;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not CustomHitZonesLevelDefinition customLevel)
            {
                Debug.LogError("CustomHitZonesLevelRunner requires CustomHitZonesLevelDefinition.");
                return;
            }

            Stop();
            _occupancy = new SpawnOccupancyTracker(customLevel.MinSpawnDistance);
            _cts = new CancellationTokenSource();
            SpawnAll(customLevel);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            foreach (var target in _active)
            {
                if (target != null)
                    UnityEngine.Object.Destroy(target.gameObject);
            }

            _active.Clear();
            _occupancy?.Clear();
            _occupancy = null;
        }

        void SpawnAll(CustomHitZonesLevelDefinition definition)
        {
            if (definition.CustomTargetPrefab == null)
                return;

            var points = definition.CustomSpawnPoints;
            var spawnedFromPoints = false;
            if (points != null && points.Length > 0)
            {
                foreach (var point in points)
                {
                    if (point == null)
                        continue;

                    SpawnAt(definition, SanitizeSpawnPoint(definition, point.position));
                    spawnedFromPoints = true;
                }

                if (spawnedFromPoints)
                    return;
            }

            for (var i = 0; i < 5; i++)
            {
                if (_occupancy != null &&
                    _occupancy.TryGetFreePoint(definition, out var freePoint, groundOnly: true))
                {
                    SpawnAt(definition, freePoint);
                }
                else
                {
                    SpawnAt(definition, SanitizeSpawnPoint(definition, definition.GetRandomGroundSpawnPoint()));
                }
            }
        }

        void SpawnAt(CustomHitZonesLevelDefinition definition, Vector3 position)
        {
            position = SanitizeSpawnPoint(definition, position);
            var prefab = definition.CustomTargetPrefab;
            var target = UnityEngine.Object.Instantiate(
                prefab,
                position,
                prefab.transform.rotation,
                _root);
            _active.Add(target);
            _occupancy?.Register(target.transform);
            target.Activate(position, hitTarget => OnScored(definition, hitTarget));
        }

        static Vector3 SanitizeSpawnPoint(CustomHitZonesLevelDefinition definition, Vector3 position)
        {
            var minY = Mathf.Max(0.75f, definition.SpawnCenter.y);
            if (position.y < minY)
                position.y = minY;
            return position;
        }

        void OnScored(CustomHitZonesLevelDefinition definition, CustomTargetView target)
        {
            if (!definition.RespawnCustomOnHit)
            {
                target.Deactivate();
                _occupancy?.Unregister(target.transform);
                return;
            }

            RespawnAsync(definition, target, _cts.Token).Forget();
        }

        async UniTaskVoid RespawnAsync(
            CustomHitZonesLevelDefinition definition,
            CustomTargetView target,
            CancellationToken token)
        {
            target.Deactivate();
            _occupancy?.Unregister(target.transform);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(definition.CustomRespawnDelay), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (target == null || _occupancy == null)
                return;

            if (!_occupancy.TryGetFreePoint(definition, out var spawnPoint, groundOnly: true))
                spawnPoint = definition.GetRandomGroundSpawnPoint();

            spawnPoint = SanitizeSpawnPoint(definition, spawnPoint);
            _occupancy.Register(target.transform);
            target.Activate(spawnPoint, hitTarget => OnScored(definition, hitTarget));
        }

        public void Dispose() => Stop();
    }
}
