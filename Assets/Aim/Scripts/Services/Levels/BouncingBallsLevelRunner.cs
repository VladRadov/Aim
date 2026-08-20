using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class BouncingBallsLevelRunner : ILevelRunner
    {
        static readonly PhysicsMaterial ArenaBounceMaterial = new("BouncingArenaMaterial")
        {
            bounciness = 1f,
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };

        readonly BouncingTargetPool _pool;
        readonly Transform _root;
        readonly List<BouncingTargetView> _active = new();
        readonly List<GameObject> _arena = new();

        CancellationTokenSource _cts;
        int _alive;

        public BouncingBallsLevelRunner(BouncingTargetPool pool, Transform root)
        {
            _pool = pool;
            _root = root;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not BouncingBallsLevelDefinition bouncingLevel)
            {
                Debug.LogError("BouncingBallsLevelRunner requires BouncingBallsLevelDefinition.");
                return;
            }

            Stop();
            BuildArena(bouncingLevel);
            _cts = new CancellationTokenSource();
            SpawnLoopAsync(bouncingLevel, _cts.Token).Forget();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            DespawnAll();
            _alive = 0;
            DestroyArena();
        }

        async UniTaskVoid SpawnLoopAsync(BouncingBallsLevelDefinition definition, CancellationToken token)
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

        void SpawnOne(BouncingBallsLevelDefinition definition)
        {
            if (_pool == null || definition.BouncingPrefab == null)
                return;

            var point = definition.GetRandomGroundSpawnPoint();
            point.y = definition.SpawnCenter.y + 0.35f;
            point.z = definition.SpawnCenter.z;

            var lateralX = UnityEngine.Random.Range(-definition.LateralSpeed, definition.LateralSpeed);
            var velocity = new Vector3(
                lateralX,
                UnityEngine.Random.Range(definition.MinUpwardSpeed, definition.MaxUpwardSpeed),
                0f);

            var target = _pool.Rent();
            IgnoreCollisionsWithActive(target);
            _alive++;
            _active.Add(target);
            target.Activate(
                point,
                velocity,
                definition.LifeTime,
                definition.Bounciness,
                OnDespawn);
        }

        void IgnoreCollisionsWithActive(BouncingTargetView target)
        {
            var collider = target != null ? target.PhysicsCollider : null;
            if (collider == null)
                return;

            for (var i = 0; i < _active.Count; i++)
            {
                var other = _active[i];
                if (other == null || other == target)
                    continue;

                var otherCollider = other.PhysicsCollider;
                if (otherCollider == null)
                    continue;

                Physics.IgnoreCollision(collider, otherCollider, true);
            }
        }

        void OnDespawn(BouncingTargetView target)
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

        void BuildArena(BouncingBallsLevelDefinition definition)
        {
            DestroyArena();

            var center = definition.SpawnCenter;
            var size = definition.SpawnSize;
            var half = size * 0.5f;
            var floorY = center.y - 0.1f;
            var wallHeight = Mathf.Max(3f, size.y + 2.5f);
            var wallThickness = 0.25f;

            CreateArenaBox(
                "BounceFloor",
                new Vector3(center.x, floorY, center.z),
                new Vector3(Mathf.Max(4f, size.x + 2f), 0.2f, 3f));

            CreateArenaBox(
                "BounceWallLeft",
                new Vector3(center.x - half.x - wallThickness * 0.5f, floorY + wallHeight * 0.5f, center.z),
                new Vector3(wallThickness, wallHeight, 3f));

            CreateArenaBox(
                "BounceWallRight",
                new Vector3(center.x + half.x + wallThickness * 0.5f, floorY + wallHeight * 0.5f, center.z),
                new Vector3(wallThickness, wallHeight, 3f));
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
