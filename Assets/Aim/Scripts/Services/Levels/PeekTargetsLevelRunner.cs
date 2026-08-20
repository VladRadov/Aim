using System.Collections.Generic;
using Aim.Config;
using Aim.Views;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class PeekTargetsLevelRunner : ILevelRunner
    {
        readonly PeekTargetPool _pool;
        readonly Transform _root;
        readonly List<PeekTargetView> _targets = new();
        readonly List<GameObject> _covers = new();

        public PeekTargetsLevelRunner(PeekTargetPool pool, Transform root)
        {
            _pool = pool;
            _root = root;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not PeekTargetsLevelDefinition peekLevel)
            {
                Debug.LogError("PeekTargetsLevelRunner requires PeekTargetsLevelDefinition.");
                return;
            }

            if (_pool == null || peekLevel.PeekPrefab == null)
            {
                Debug.LogError("PeekTargetsLevelRunner: peek prefab or pool is missing.");
                return;
            }

            Stop();
            SpawnCoversAndTargets(peekLevel);
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

            for (var i = 0; i < _covers.Count; i++)
            {
                if (_covers[i] != null)
                    UnityEngine.Object.Destroy(_covers[i]);
            }

            _covers.Clear();
        }

        void SpawnCoversAndTargets(PeekTargetsLevelDefinition definition)
        {
            var count = definition.CoverCount;
            var spacing = definition.CoverSpacing;
            var center = definition.SpawnCenter;
            var originX = center.x - (count - 1) * spacing * 0.5f;
            var coverSize = definition.CoverSize;

            for (var i = 0; i < count; i++)
            {
                var coverX = originX + i * spacing;
                var coverPos = new Vector3(coverX, center.y + coverSize.y * 0.5f - 1f, center.z);

                var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cover.name = $"PeekCover_{i}";
                if (_root != null)
                    cover.transform.SetParent(_root, false);
                cover.transform.position = coverPos;
                cover.transform.localScale = coverSize;
                // Same mask as targets so shots stop on cover; no IHittable => no score.
                cover.layer = definition.PeekPrefab.gameObject.layer;
                var coverRenderer = cover.GetComponent<Renderer>();
                if (coverRenderer != null)
                    coverRenderer.material.color = new Color(0.28f, 0.3f, 0.34f, 1f);
                _covers.Add(cover);

                var coverHalf = coverSize * 0.5f;
                var target = _pool.Rent();
                _targets.Add(target);
                target.ActivateFromCover(
                    coverPos,
                    coverHalf,
                    definition.TargetHeight,
                    definition.HideDepth,
                    definition.PeekSideOffset,
                    definition.PeekDurationMin,
                    definition.PeekDurationMax,
                    definition.HideDurationMin,
                    definition.HideDurationMax,
                    definition.MoveDuration,
                    initialHideDelay: UnityEngine.Random.Range(0f, definition.HideDurationMax),
                    onDespawn: despawned => _targets.Remove(despawned));
            }
        }

        public void Dispose() => Stop();
    }
}
