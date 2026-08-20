using System.Collections.Generic;
using Aim.Config;
using Aim.Views;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class PopupDucksLevelRunner : ILevelRunner
    {
        readonly PeekTargetPool _pool;
        readonly List<PeekTargetView> _targets = new();

        public PopupDucksLevelRunner(PeekTargetPool pool, Transform root)
        {
            _pool = pool;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not PopupDucksLevelDefinition popupLevel)
            {
                Debug.LogError("PopupDucksLevelRunner requires PopupDucksLevelDefinition.");
                return;
            }

            if (_pool == null || popupLevel.DuckPrefab == null)
            {
                Debug.LogError("PopupDucksLevelRunner: duck prefab or pool is missing.");
                return;
            }

            Stop();
            SpawnGrid(popupLevel);
        }

        public void Stop()
        {
            if (_targets.Count > 0)
            {
                var snapshot = _targets.ToArray();
                _targets.Clear();
                for (var i = 0; i < snapshot.Length; i++)
                    snapshot[i]?.Despawn();
            }
        }

        void SpawnGrid(PopupDucksLevelDefinition definition)
        {
            var columns = definition.Columns;
            var rows = definition.Rows;
            var spacing = definition.Spacing;
            var center = definition.SpawnCenter;
            var originX = center.x - (columns - 1) * spacing.x * 0.5f;
            var originZ = center.z - (rows - 1) * spacing.y * 0.5f;

            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var x = originX + col * spacing.x;
                    var z = originZ + row * spacing.y;
                    var up = new Vector3(x, definition.UpHeight, z);
                    var down = new Vector3(x, definition.UpHeight - definition.DownOffset, z);

                    var target = _pool.Rent();
                    _targets.Add(target);
                    target.Activate(
                        hidePosition: down,
                        peekPosition: up,
                        peekDurationMin: definition.UpDurationMin,
                        peekDurationMax: definition.UpDurationMax,
                        hideDurationMin: definition.DownDurationMin,
                        hideDurationMax: definition.DownDurationMax,
                        moveDuration: definition.MoveDuration,
                        initialHideDelay: UnityEngine.Random.Range(0f, definition.DownDurationMax),
                        onDespawn: despawned => _targets.Remove(despawned));
                }
            }
        }

        public void Dispose() => Stop();
    }
}
