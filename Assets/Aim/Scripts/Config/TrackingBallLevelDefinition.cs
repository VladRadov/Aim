using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "TrackingBallLevel", menuName = "Aim/Levels/Tracking Ball")]
    public sealed class TrackingBallLevelDefinition : LevelDefinition
    {
        [Header("Tracking Ball")]
        [SerializeField] TrackingTargetView trackingPrefab;
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float drainPerSecond = 40f;
        [SerializeField] float minUpwardSpeed = 5f;
        [SerializeField] float maxUpwardSpeed = 7f;
        [SerializeField] float lateralSpeed = 1.8f;
        [SerializeField] float bounciness = 1f;
        [SerializeField] float respawnDelay = 0.35f;
        [SerializeField] float aimMaxDistance = 200f;

        public override LevelType LevelType => LevelType.TrackingBall;
        public override int Ammo => 0;
        public override bool AllowsShooting => false;

        public TrackingTargetView TrackingPrefab => trackingPrefab;
        public float MaxHealth => Mathf.Max(1f, maxHealth);
        public float DrainPerSecond => Mathf.Max(0.1f, drainPerSecond);
        public float MinUpwardSpeed => Mathf.Max(0.5f, minUpwardSpeed);
        public float MaxUpwardSpeed => Mathf.Max(MinUpwardSpeed, maxUpwardSpeed);
        public float LateralSpeed => Mathf.Max(0f, lateralSpeed);
        public float Bounciness => Mathf.Clamp01(bounciness);
        public float RespawnDelay => Mathf.Max(0f, respawnDelay);
        public float AimMaxDistance => Mathf.Max(1f, aimMaxDistance);
    }
}
