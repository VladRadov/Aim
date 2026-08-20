using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "FlickTargetsLevel", menuName = "Aim/Levels/Flick Targets")]
    public sealed class FlickTargetsLevelDefinition : LevelDefinition
    {
        [Header("Flick Targets")]
        [SerializeField] FlyingTargetView flickPrefab;
        [SerializeField] float spawnInterval = 0.45f;
        [SerializeField] int maxAlive = 1;
        [SerializeField] float lifeTimeMin = 0.35f;
        [SerializeField] float lifeTimeMax = 0.8f;

        public override LevelType LevelType => LevelType.FlickTargets;
        public FlyingTargetView FlickPrefab => flickPrefab;
        public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
        public int MaxAlive => Mathf.Max(1, maxAlive);
        public float LifeTimeMin => Mathf.Max(0.1f, lifeTimeMin);
        public float LifeTimeMax => Mathf.Max(LifeTimeMin, lifeTimeMax);

        public float RollLifeTime() => UnityEngine.Random.Range(LifeTimeMin, LifeTimeMax);
    }
}
