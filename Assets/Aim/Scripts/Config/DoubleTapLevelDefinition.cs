using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "DoubleTapLevel", menuName = "Aim/Levels/Double Tap")]
    public sealed class DoubleTapLevelDefinition : LevelDefinition
    {
        [Header("Double Tap / Multi-HP")]
        [SerializeField] MultiHitTargetView multiHitPrefab;
        [SerializeField] float spawnInterval = 0.85f;
        [SerializeField] int maxAlive = 4;
        [SerializeField] int hitPoints = 3;
        [SerializeField] float lifeTime = 6f;

        public override LevelType LevelType => LevelType.DoubleTap;
        public MultiHitTargetView MultiHitPrefab => multiHitPrefab;
        public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
        public int MaxAlive => Mathf.Max(1, maxAlive);
        public int HitPoints => Mathf.Max(2, hitPoints);
        public float LifeTime => Mathf.Max(0.5f, lifeTime);
    }
}
