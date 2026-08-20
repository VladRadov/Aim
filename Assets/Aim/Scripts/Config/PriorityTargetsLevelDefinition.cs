using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "PriorityTargetsLevel", menuName = "Aim/Levels/Priority Targets")]
    public sealed class PriorityTargetsLevelDefinition : LevelDefinition
    {
        [Header("Priority Targets")]
        [SerializeField] PriorityTargetView priorityPrefab;
        [SerializeField] int maxAlive = 5;
        [SerializeField] float respawnDelay = 0.35f;

        public override LevelType LevelType => LevelType.PriorityTargets;
        public PriorityTargetView PriorityPrefab => priorityPrefab;
        public int MaxAlive => Mathf.Max(2, maxAlive);
        public float RespawnDelay => Mathf.Max(0.05f, respawnDelay);
    }
}
