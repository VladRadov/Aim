using System;
using System.Collections.Generic;
using System.Threading;
using Aim.Config;
using Aim.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Services.Levels
{
    public sealed class ShootingGalleryLevelRunner : ILevelRunner
    {
        readonly Transform _root;
        readonly List<GalleryTargetView> _targets = new();
        ShootingGalleryLevelDefinition _definition;
        CancellationTokenSource _cts;
        GalleryTargetView _activeTarget;
        GameObject _fallFloor;
        bool _waitingRetarget;

        public ShootingGalleryLevelRunner(Transform root)
        {
            _root = root;
        }

        public void Start(LevelDefinition definition)
        {
            if (definition is not ShootingGalleryLevelDefinition galleryLevel)
            {
                Debug.LogError("ShootingGalleryLevelRunner requires ShootingGalleryLevelDefinition.");
                return;
            }

            Stop();
            _definition = galleryLevel;
            _cts = new CancellationTokenSource();
            EnsureFallFloor(galleryLevel);
            SpawnGrid(galleryLevel);
            PickNextTarget(exclude: null);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _waitingRetarget = false;
            _activeTarget = null;
            _definition = null;

            for (var i = 0; i < _targets.Count; i++)
            {
                if (_targets[i] != null)
                    UnityEngine.Object.Destroy(_targets[i].gameObject);
            }

            _targets.Clear();
            ClearExistingTargets();
            DestroyFallFloor();
        }

        void EnsureFallFloor(ShootingGalleryLevelDefinition definition)
        {
            DestroyFallFloor();

            var center = definition.SpawnCenter;
            var columns = definition.Columns;
            var rows = definition.Rows;
            var spacing = definition.Spacing;
            var width = Mathf.Max(4f, columns * spacing.x + 4f);
            var depth = 6f;
            var floorY = Mathf.Max(0.02f, center.y - (rows - 1) * spacing.y * 0.5f - 0.55f);

            _fallFloor = new GameObject("GalleryFallFloor");
            if (_root != null)
                _fallFloor.transform.SetParent(_root, false);

            _fallFloor.transform.position = new Vector3(center.x, floorY, center.z);
            var box = _fallFloor.AddComponent<BoxCollider>();
            box.size = new Vector3(width, 0.1f, depth);
            box.center = new Vector3(0f, -0.05f, 0f);
        }

        void DestroyFallFloor()
        {
            if (_fallFloor != null)
            {
                UnityEngine.Object.Destroy(_fallFloor);
                _fallFloor = null;
            }
        }

        void SpawnGrid(ShootingGalleryLevelDefinition definition)
        {
            ClearExistingTargets();

            var prefabs = definition.TargetPrefabs;
            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogWarning("ShootingGalleryLevel: assign targetPrefabs on the level asset.");
                return;
            }

            var columns = definition.Columns;
            var rows = definition.Rows;
            var spacing = definition.Spacing;
            var center = definition.SpawnCenter;
            var rotation = Quaternion.Euler(0f, definition.FacingYaw, 0f);

            var originX = center.x - (columns - 1) * spacing.x * 0.5f;
            var originY = center.y - (rows - 1) * spacing.y * 0.5f;

            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var prefab = PickPrefab(prefabs);
                    if (prefab == null)
                        continue;

                    var position = new Vector3(
                        originX + col * spacing.x,
                        originY + row * spacing.y,
                        center.z);

                    var target = UnityEngine.Object.Instantiate(prefab, position, rotation, _root);
                    target.Activate(
                        position,
                        rotation,
                        definition.NormalColor,
                        definition.HighlightColor,
                        OnCorrectHit,
                        OnKnockedDown);
                    _targets.Add(target);
                }
            }
        }

        static GalleryTargetView PickPrefab(GalleryTargetView[] prefabs)
        {
            var valid = 0;
            for (var i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] != null)
                    valid++;
            }

            if (valid == 0)
                return null;

            var pick = UnityEngine.Random.Range(0, valid);
            for (var i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] == null)
                    continue;
                if (pick == 0)
                    return prefabs[i];
                pick--;
            }

            return null;
        }

        void ClearExistingTargets()
        {
            if (_root == null)
                return;

            for (var i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (child != null && child.GetComponentInChildren<GalleryTargetView>(true) != null)
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        void OnCorrectHit(GalleryTargetView target)
        {
            if (_cts == null || _cts.IsCancellationRequested || _waitingRetarget)
                return;
            if (target == null || target != _activeTarget)
                return;

            ClearHighlights();
            var previous = _activeTarget;
            _activeTarget = null;
            _waitingRetarget = true;
            RetargetAfterDelayAsync(previous, _cts.Token).Forget();
        }

        void OnKnockedDown(GalleryTargetView target)
        {
            if (target == null)
                return;

            if (_activeTarget == target)
            {
                ClearHighlights();
                _activeTarget = null;
            }
        }

        async UniTaskVoid RetargetAfterDelayAsync(GalleryTargetView previous, CancellationToken token)
        {
            try
            {
                var delay = _definition != null ? _definition.RetargetDelay : 0.35f;
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _waitingRetarget = false;
            if (_cts == null || _cts.IsCancellationRequested)
                return;

            PickNextTarget(previous);
        }

        void PickNextTarget(GalleryTargetView exclude)
        {
            if (_targets.Count == 0)
                return;

            ClearHighlights();

            var standingCount = 0;
            for (var i = 0; i < _targets.Count; i++)
            {
                if (_targets[i] != null && _targets[i].IsStanding && _targets[i] != exclude)
                    standingCount++;
            }

            if (standingCount == 0)
                return;

            GalleryTargetView next = null;
            var attempts = 0;
            do
            {
                next = _targets[UnityEngine.Random.Range(0, _targets.Count)];
                attempts++;
            } while ((next == null || !next.IsStanding || next == exclude) && attempts < 32);

            if (next == null || !next.IsStanding)
                return;

            _activeTarget = next;
            next.SetHighlighted(true);
        }

        int CountStanding()
        {
            var count = 0;
            for (var i = 0; i < _targets.Count; i++)
            {
                if (_targets[i] != null && _targets[i].IsStanding)
                    count++;
            }

            return count;
        }

        void ClearHighlights()
        {
            for (var i = 0; i < _targets.Count; i++)
                _targets[i]?.SetHighlighted(false);
        }

        public void Dispose() => Stop();
    }
}
