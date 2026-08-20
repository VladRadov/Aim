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
    public sealed class CharacterLevelRunner : ILevelRunner
    {
        readonly CharacterTargetPool _pool;
        readonly Transform _root;
        readonly List<CharacterTargetView> _active = new();
        SpawnOccupancyTracker _occupancy;
        CancellationTokenSource _cts;
        int _alive;

        public CharacterLevelRunner(CharacterTargetPool pool, Transform root)
        {
            _pool = pool;
            _root = root;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not CharacterHeadshotLevelDefinition characterLevel)
            {
                Debug.LogError("CharacterLevelRunner requires CharacterHeadshotLevelDefinition.");
                return;
            }

            Stop();
            _occupancy = new SpawnOccupancyTracker(characterLevel.MinSpawnDistance);
            _cts = new CancellationTokenSource();
            SpawnLoopAsync(characterLevel, _cts.Token).Forget();
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
                var character = snapshot[i];
                if (character != null)
                    character.Despawn();
            }
        }

        async UniTaskVoid SpawnLoopAsync(CharacterHeadshotLevelDefinition definition, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_alive < definition.MaxCharactersAlive)
                        SpawnOne(definition);

                    await UniTask.Delay(TimeSpan.FromSeconds(definition.CharacterSpawnInterval), cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        void SpawnOne(CharacterHeadshotLevelDefinition definition)
        {
            if (definition.CharacterPrefab == null || _pool == null || _occupancy == null)
                return;

            if (!_occupancy.TryGetFreePoint(definition, out var spawnPoint, groundOnly: true))
                return;

            var character = _pool.Rent();
            _alive++;
            _active.Add(character);
            _occupancy.Register(character.transform);

            character.Activate(
                spawnPoint,
                definition.CharacterMoveMode,
                definition.CharacterRunSpeed,
                definition.CharacterJumpHeight,
                definition.CharacterJumpInterval,
                definition.CharacterLifetime,
                definition.SpawnSize.x * 0.5f,
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
