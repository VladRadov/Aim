using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "TrackingMoversLevel", menuName = "Aim/Levels/Tracking Movers")]
    public sealed class TrackingMoversLevelDefinition : LevelDefinition
    {
        [Header("Tracking Movers")]
        [SerializeField] TrackingMoverView moverPrefab;
        [SerializeField] float spawnInterval = 0.65f;
        [SerializeField] int maxAlive = 5;
        [SerializeField] float maxHealth = 70f;
        [SerializeField] float drainPerSecond = 45f;
        [SerializeField] float speedMin = 1.6f;
        [SerializeField] float speedMax = 3.2f;
        [SerializeField] float respawnDelay;
        [SerializeField] float aimMaxDistance = 200f;
        [SerializeField] FlyDirection moveDirection = FlyDirection.Random;

        public override LevelType LevelType => LevelType.TrackingMovers;
        public override int Ammo => 0;
        public override bool AllowsShooting => false;

        public TrackingMoverView MoverPrefab => moverPrefab;
        public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
        public int MaxAlive => Mathf.Max(1, maxAlive);
        public float MaxHealth => Mathf.Max(1f, maxHealth);
        public float DrainPerSecond => Mathf.Max(0.1f, drainPerSecond);
        public float SpeedMin => Mathf.Max(0.2f, speedMin);
        public float SpeedMax => Mathf.Max(SpeedMin, speedMax);
        public float RespawnDelay => Mathf.Max(0f, respawnDelay);
        public float AimMaxDistance => Mathf.Max(1f, aimMaxDistance);
        public FlyDirection MoveDirection => moveDirection;
    }
}
