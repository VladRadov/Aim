using Aim.Models;
using UnityEngine;

namespace Aim.Config
{
    public abstract class LevelDefinition : ScriptableObject
    {
        [Header("Common")]
        [SerializeField] string displayName = "Level";
        [SerializeField] int requiredHits = 10;
        [SerializeField] int ammo = 15;
        [SerializeField] bool allowsShooting = true;
        [SerializeField] bool autoStart = true;

        [Header("Spawn Area (world)")]
        [SerializeField] Vector3 spawnCenter = new(0f, 2f, 8f);
        [SerializeField] Vector3 spawnSize = new(8f, 3f, 0.5f);
        [SerializeField] float minSpawnDistance = 1.6f;

        public string DisplayName => displayName;
        public abstract LevelType LevelType { get; }
        public virtual int RequiredHits => requiredHits;
        public virtual int Ammo => ammo;
        public virtual bool AllowsShooting => allowsShooting;
        public bool AutoStart => autoStart;
        public Vector3 SpawnCenter => spawnCenter;
        public Vector3 SpawnSize => spawnSize;
        public float MinSpawnDistance => minSpawnDistance > 0.01f ? minSpawnDistance : 1.6f;

        public Vector3 GetRandomSpawnPoint()
        {
            var half = spawnSize * 0.5f;
            return spawnCenter + new Vector3(
                UnityEngine.Random.Range(-half.x, half.x),
                UnityEngine.Random.Range(-half.y, half.y),
                UnityEngine.Random.Range(-half.z, half.z));
        }

        public Vector3 GetRandomGroundSpawnPoint()
        {
            var half = spawnSize * 0.5f;
            return new Vector3(
                spawnCenter.x + UnityEngine.Random.Range(-half.x, half.x),
                spawnCenter.y,
                spawnCenter.z + UnityEngine.Random.Range(-half.z, half.z));
        }
    }
}
