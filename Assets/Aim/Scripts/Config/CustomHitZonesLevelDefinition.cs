using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "CustomHitZonesLevel", menuName = "Aim/Levels/Custom Hit Zones")]
    public sealed class CustomHitZonesLevelDefinition : LevelDefinition
    {
        [Header("Custom Hit Zones")]
        [SerializeField] CustomTargetView customTargetPrefab;
        [SerializeField] Transform[] customSpawnPoints;
        [SerializeField] bool respawnCustomOnHit = true;
        [SerializeField] float customRespawnDelay = 0.4f;

        public override LevelType LevelType => Aim.Models.LevelType.CustomHitZones;
        public CustomTargetView CustomTargetPrefab => customTargetPrefab;
        public Transform[] CustomSpawnPoints => customSpawnPoints;
        public bool RespawnCustomOnHit => respawnCustomOnHit;
        public float CustomRespawnDelay => customRespawnDelay;
    }
}
