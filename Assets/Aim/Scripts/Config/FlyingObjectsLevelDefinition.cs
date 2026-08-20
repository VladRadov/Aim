using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "FlyingObjectsLevel", menuName = "Aim/Levels/Flying Objects")]
    public sealed class FlyingObjectsLevelDefinition : LevelDefinition
    {
        [Header("Flying Objects")]
        [SerializeField] FlyingTargetView flyingPrefab;
        [SerializeField] float flyingSpawnInterval = 0.8f;
        [SerializeField] int maxFlyingAlive = 6;
        [SerializeField] float flyingSpeedMin = 2f;
        [SerializeField] float flyingSpeedMax = 5f;
        [SerializeField] float flyingLifetime = 5f;
        [SerializeField] FlyDirection flyingDirection = FlyDirection.Random;

        public override LevelType LevelType => Aim.Models.LevelType.FlyingObjects;
        public FlyingTargetView FlyingPrefab => flyingPrefab;
        public float FlyingSpawnInterval => flyingSpawnInterval;
        public int MaxFlyingAlive => maxFlyingAlive;
        public float FlyingSpeedMin => flyingSpeedMin;
        public float FlyingSpeedMax => flyingSpeedMax;
        public float FlyingLifetime => flyingLifetime;
        public FlyDirection FlyingDirection => flyingDirection;
    }
}
