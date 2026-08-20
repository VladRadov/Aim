using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "MovingRailsLevel", menuName = "Aim/Levels/Moving Rails")]
    public sealed class MovingRailsLevelDefinition : LevelDefinition
    {
        [Header("Moving Rails")]
        [SerializeField] FlyingTargetView railPrefab;
        [SerializeField] float spawnInterval = 1.1f;
        [SerializeField] int maxAlive = 4;
        [SerializeField] int railCount = 3;
        [SerializeField] float railWidth = 7f;
        [SerializeField] float railSpacingY = 1.1f;
        [SerializeField] float speedMin = 2.2f;
        [SerializeField] float speedMax = 4.5f;
        [SerializeField] float lifetime = 8f;

        public override LevelType LevelType => LevelType.MovingRails;
        public FlyingTargetView RailPrefab => railPrefab;
        public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
        public int MaxAlive => Mathf.Max(1, maxAlive);
        public int RailCount => Mathf.Max(1, railCount);
        public float RailWidth => Mathf.Max(1f, railWidth);
        public float RailSpacingY => Mathf.Max(0.2f, railSpacingY);
        public float SpeedMin => Mathf.Max(0.1f, speedMin);
        public float SpeedMax => Mathf.Max(SpeedMin, speedMax);
        public float Lifetime => Mathf.Max(0.5f, lifetime);
    }
}
