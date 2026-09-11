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
    public sealed class TrackingBallLevelRunner : ILevelRunner
    {
        static readonly PhysicsMaterial ArenaBounceMaterial = new("TrackingArenaMaterial")
        {
            bounciness = 1f,
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };

        readonly TrackingTargetPool _pool;
        readonly Transform _root;
        readonly SessionModel _session;
        readonly SettingsModel _settings;
        readonly Camera _aimCamera;
        readonly LayerMask _targetMask;
        readonly List<GameObject> _arena = new();
        readonly RaycastHit[] _hits = new RaycastHit[8];

        CancellationTokenSource _cts;
        TrackingTargetView _active;
        TrackingBallLevelDefinition _definition;
        Bounds _playBounds;
        Vector3? _lastSpawnPoint;
        bool _respawning;

        public TrackingBallLevelRunner(
            TrackingTargetPool pool,
            Transform root,
            SessionModel session,
            SettingsModel settings,
            Camera aimCamera,
            LayerMask targetMask)
        {
            _pool = pool;
            _root = root;
            _session = session;
            _settings = settings;
            _aimCamera = aimCamera;
            _targetMask = targetMask;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not TrackingBallLevelDefinition trackingLevel)
            {
                Debug.LogError("TrackingBallLevelRunner requires TrackingBallLevelDefinition.");
                return;
            }

            Stop();
            _definition = trackingLevel;
            BuildArena(trackingLevel);
            _cts = new CancellationTokenSource();
            SpawnOne();
            TrackLoopAsync(_cts.Token).Forget();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _respawning = false;
            _lastSpawnPoint = null;

            if (_active != null)
            {
                var current = _active;
                _active = null;
                current.Despawn();
            }

            DestroyArena();
            _definition = null;
        }

        async UniTaskVoid TrackLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                    TickAimDrain(Time.deltaTime);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        void TickAimDrain(float deltaTime)
        {
            if (_definition == null || _session == null || !_session.IsPlaying)
                return;

            if (_settings != null && _settings.IsOpen.Value)
                return;

            if (_respawning || _active == null || !_active.IsActive)
                return;

            if (_aimCamera == null)
                return;

            var ray = _aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var count = Physics.RaycastNonAlloc(
                ray,
                _hits,
                _definition.AimMaxDistance,
                _targetMask,
                QueryTriggerInteraction.Ignore);

            TrackingTargetView tracked = null;
            var bestDistance = float.MaxValue;
            var activeCollider = _active.PhysicsCollider;
            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.collider == null || hit.collider != activeCollider)
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                tracked = _active;
            }

            if (tracked == null)
            {
                _active.SetTracked(false);
                return;
            }

            var killed = tracked.ApplyAimDamage(_definition.DrainPerSecond * deltaTime);
                if (killed)
                    OnBallKilledAsync().Forget();
        }

        async UniTaskVoid OnBallKilledAsync()
        {
            if (_respawning)
                return;

            _respawning = true;
            _active = null;
            _session.RegisterScoreHit();

            if (!_session.IsPlaying)
            {
                _respawning = false;
                return;
            }

            var token = _cts != null ? _cts.Token : CancellationToken.None;

            try
            {
                if (_definition != null && _definition.RespawnDelay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(_definition.RespawnDelay), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                _respawning = false;
                return;
            }

            if (!token.IsCancellationRequested && _session.IsPlaying)
                SpawnOne();

            _respawning = false;
        }

        void SpawnOne()
        {
            if (_pool == null || _definition == null || _definition.TrackingPrefab == null)
                return;

            if (_active != null)
                return;

            var point = PickRandomSpawnPoint();
            _lastSpawnPoint = point;

            var velocity = new Vector3(
                UnityEngine.Random.Range(-_definition.LateralSpeed, _definition.LateralSpeed),
                UnityEngine.Random.Range(_definition.MinUpwardSpeed, _definition.MaxUpwardSpeed),
                0f);

            var target = _pool.Rent();
            _active = target;
            target.Activate(
                point,
                velocity,
                _definition.MaxHealth,
                _definition.Bounciness,
                _aimCamera,
                _playBounds,
                OnDespawn);
        }

        Vector3 PickRandomSpawnPoint()
        {
            var center = _definition.SpawnCenter;
            var halfWidth = Mathf.Max(0.5f, _definition.SpawnSize.x * 0.5f - 1.2f);
            var minDistance = Mathf.Max(1.5f, _definition.MinSpawnDistance);
            var y = center.y + 1.0f;
            var z = center.z;

            for (var attempt = 0; attempt < 16; attempt++)
            {
                var x = center.x + UnityEngine.Random.Range(-halfWidth, halfWidth);
                var candidate = new Vector3(x, y, z);
                if (!_lastSpawnPoint.HasValue ||
                    Mathf.Abs(candidate.x - _lastSpawnPoint.Value.x) >= minDistance)
                {
                    return candidate;
                }
            }

            var side = _lastSpawnPoint.HasValue && _lastSpawnPoint.Value.x >= center.x ? -1f : 1f;
            return new Vector3(center.x + side * halfWidth, y, z);
        }

        void OnDespawn(TrackingTargetView target)
        {
            if (_active == target)
                _active = null;
        }

        void BuildArena(TrackingBallLevelDefinition definition)
        {
            DestroyArena();

            var center = definition.SpawnCenter;
            var size = definition.SpawnSize;
            var half = size * 0.5f;
            var floorY = center.y - 0.1f;
            // Tall enough that high bounces cannot clear the side walls.
            var wallHeight = Mathf.Max(10f, size.y + definition.MaxUpwardSpeed * 0.85f + 3f);
            var wallThickness = 0.4f;
            var depth = Mathf.Max(4f, size.z + 2f);
            var width = Mathf.Max(4f, size.x);

            _playBounds = new Bounds(
                new Vector3(center.x, floorY + wallHeight * 0.5f, center.z),
                new Vector3(width, wallHeight, depth));

            CreateArenaBox(
                "TrackFloor",
                new Vector3(center.x, floorY, center.z),
                new Vector3(width + wallThickness * 2f, 0.2f, depth));

            CreateArenaBox(
                "TrackCeiling",
                new Vector3(center.x, floorY + wallHeight, center.z),
                new Vector3(width + wallThickness * 2f, 0.2f, depth));

            CreateArenaBox(
                "TrackWallLeft",
                new Vector3(center.x - half.x - wallThickness * 0.5f, floorY + wallHeight * 0.5f, center.z),
                new Vector3(wallThickness, wallHeight, depth));

            CreateArenaBox(
                "TrackWallRight",
                new Vector3(center.x + half.x + wallThickness * 0.5f, floorY + wallHeight * 0.5f, center.z),
                new Vector3(wallThickness, wallHeight, depth));
        }

        void CreateArenaBox(string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            if (_root != null)
                go.transform.SetParent(_root, false);
            go.transform.position = position;
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
            collider.sharedMaterial = ArenaBounceMaterial;
            _arena.Add(go);
        }

        void DestroyArena()
        {
            for (var i = 0; i < _arena.Count; i++)
            {
                if (_arena[i] != null)
                    UnityEngine.Object.Destroy(_arena[i]);
            }

            _arena.Clear();
        }

        public void Dispose() => Stop();
    }
}
