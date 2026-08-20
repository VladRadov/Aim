using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "StaticBallsLevel", menuName = "Aim/Levels/Static Balls")]
    public sealed class StaticBallsLevelDefinition : LevelDefinition
    {
        [Header("Static Balls")]
        [SerializeField] FlyingTargetView staticPrefab;
        [SerializeField] float spawnInterval = 0.7f;
        [SerializeField] int maxAlive = 6;
        [SerializeField] float lifeTime = 4.5f;

        public override LevelType LevelType => LevelType.StaticBalls;
        public FlyingTargetView StaticPrefab => staticPrefab;
        public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
        public int MaxAlive => Mathf.Max(1, maxAlive);
        public float LifeTime => Mathf.Max(0.5f, lifeTime);
    }
}
