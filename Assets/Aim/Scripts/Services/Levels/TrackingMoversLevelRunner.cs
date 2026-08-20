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
    public sealed class TrackingMoversLevelRunner : ILevelRunner
    {
        readonly TrackingMoverPool _pool;
        readonly SessionModel _session;
        readonly SettingsModel _settings;
        readonly Camera _aimCamera;
        readonly LayerMask _targetMask;
        readonly List<TrackingMoverView> _active = new();
        readonly RaycastHit[] _hits = new RaycastHit[16];

        CancellationTokenSource _cts;
        TrackingMoversLevelDefinition _definition;
        Bounds _moveBounds;
        int _alive;

        public TrackingMoversLevelRunner(
            TrackingMoverPool pool,
            SessionModel session,
            SettingsModel settings,
            Camera aimCamera,
            LayerMask targetMask)
        {
            _pool = pool;
            _session = session;
            _settings = settings;
            _aimCamera = aimCamera;
            _targetMask = targetMask;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not TrackingMoversLevelDefinition moversLevel)
            {
                Debug.LogError("TrackingMoversLevelRunner requires TrackingMoversLevelDefinition.");
                return;
            }

            Stop();
            _definition = moversLevel;
            _moveBounds = new Bounds(moversLevel.SpawnCenter, moversLevel.SpawnSize);
            _cts = new CancellationTokenSource();
            SpawnLoopAsync(moversLevel, _cts.Token).Forget();
            TrackLoopAsync(_cts.Token).Forget();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            DespawnAll();
            _alive = 0;
            _definition = null;
        }

        async UniTaskVoid SpawnLoopAsync(TrackingMoversLevelDefinition definition, CancellationToken token)
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

            if (_aimCamera == null || _active.Count == 0)
                return;

            var ray = _aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var count = Physics.RaycastNonAlloc(
                ray,
                _hits,
                _definition.AimMaxDistance,
                _targetMask,
                QueryTriggerInteraction.Ignore);

            TrackingMoverView tracked = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.collider == null)
                    continue;

                for (var a = 0; a < _active.Count; a++)
                {
                    var candidate = _active[a];
                    if (candidate == null || !candidate.IsActive)
                        continue;
                    if (candidate.PhysicsCollider != hit.collider)
                        continue;
                    if (hit.distance >= bestDistance)
                        continue;

                    bestDistance = hit.distance;
                    tracked = candidate;
                }
            }

            for (var i = 0; i < _active.Count; i++)
            {
                var target = _active[i];
                if (target == null || !target.IsActive)
                    continue;

                if (tracked != null && target == tracked)
                {
                    var killed = target.ApplyAimDamage(_definition.DrainPerSecond * deltaTime);
                    if (killed)
                        _session.RegisterScoreHit();
                }
                else
                {
                    target.SetTracked(false);
                }
            }
        }

        void SpawnOne(TrackingMoversLevelDefinition definition)
        {
            if (_pool == null || definition.MoverPrefab == null)
                return;

            var point = definition.GetRandomSpawnPoint();
            point.z = definition.SpawnCenter.z;

            var speed = UnityEngine.Random.Range(definition.SpeedMin, definition.SpeedMax);
            var direction = ResolveDirection(definition.MoveDirection);

            var target = _pool.Rent();
            _alive++;
            _active.Add(target);
            target.Activate(
                point,
                direction,
                speed,
                definition.MaxHealth,
                _moveBounds,
                _aimCamera,
                OnDespawn);
        }

        void OnDespawn(TrackingMoverView target)
        {
            _active.Remove(target);
            _alive = Mathf.Max(0, _alive - 1);
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

        static Vector3 ResolveDirection(FlyDirection direction)
        {
            if (direction == FlyDirection.Random)
            {
                // Include diagonals so movement feels freer than cardinal-only.
                var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            }

            return direction switch
            {
                FlyDirection.Up => Vector3.up,
                FlyDirection.Down => Vector3.down,
                FlyDirection.Left => Vector3.left,
                FlyDirection.Right => Vector3.right,
                _ => Vector3.right
            };
        }

        public void Dispose() => Stop();
    }
}
