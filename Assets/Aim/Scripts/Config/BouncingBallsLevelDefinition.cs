using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "BouncingBallsLevel", menuName = "Aim/Levels/Bouncing Balls")]
    public sealed class BouncingBallsLevelDefinition : LevelDefinition
    {
        [Header("Bouncing Balls")]
        [SerializeField] BouncingTargetView bouncingPrefab;
        [SerializeField] float spawnInterval = 0.55f;
        [SerializeField] int maxAlive = 6;
        [SerializeField] float minUpwardSpeed = 5f;
        [SerializeField] float maxUpwardSpeed = 7f;
        [SerializeField] float lateralSpeed = 1.8f;
        [Tooltip("0 = stay alive until hit. >0 = auto-despawn after seconds.")]
        [SerializeField] float lifeTime;
        [SerializeField] float bounciness = 1f;

        public override LevelType LevelType => LevelType.BouncingBalls;
        public BouncingTargetView BouncingPrefab => bouncingPrefab;
        public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
        public int MaxAlive => Mathf.Max(1, maxAlive);
        public float MinUpwardSpeed => Mathf.Max(0.5f, minUpwardSpeed);
        public float MaxUpwardSpeed => Mathf.Max(MinUpwardSpeed, maxUpwardSpeed);
        public float LateralSpeed => Mathf.Max(0f, lateralSpeed);
        public float LifeTime => Mathf.Max(0f, lifeTime);
        public float Bounciness => Mathf.Clamp01(bounciness);
    }
}
