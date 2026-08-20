using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "PrecisionCirclesLevel", menuName = "Aim/Levels/Precision Circles")]
    public sealed class PrecisionCirclesLevelDefinition : LevelDefinition
    {
        [Header("Precision Circles")]
        [SerializeField] FlyingTargetView circlePrefab;
        [SerializeField] float spawnInterval = 0.9f;
        [SerializeField] int maxAlive = 5;
        [SerializeField] float lifeTime = 5f;
        [SerializeField] float scaleMin = 0.35f;
        [SerializeField] float scaleMax = 0.7f;
        [SerializeField] float nearDistance = 6f;
        [SerializeField] float farDistance = 14f;

        public override LevelType LevelType => LevelType.PrecisionCircles;
        public FlyingTargetView CirclePrefab => circlePrefab;
        public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
        public int MaxAlive => Mathf.Max(1, maxAlive);
        public float LifeTime => Mathf.Max(0.5f, lifeTime);
        public float ScaleMin => Mathf.Max(0.1f, scaleMin);
        public float ScaleMax => Mathf.Max(ScaleMin, scaleMax);
        public float NearDistance => Mathf.Max(1f, nearDistance);
        public float FarDistance => Mathf.Max(NearDistance + 0.5f, farDistance);
    }
}
