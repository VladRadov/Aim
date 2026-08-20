using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "ShootingGalleryLevel", menuName = "Aim/Levels/Shooting Gallery")]
    public sealed class ShootingGalleryLevelDefinition : LevelDefinition
    {
        [Header("Shooting Gallery")]
        [Tooltip("Assign one or more target prefabs. Each grid slot picks randomly from this list.")]
        [SerializeField] GalleryTargetView[] targetPrefabs;
        [SerializeField] int columns = 5;
        [SerializeField] int rows = 2;
        [SerializeField] Vector2 spacing = new(1.4f, 1.2f);
        [SerializeField] float facingYaw = 180f;
        [SerializeField] float retargetDelay = 0.35f;
        [SerializeField] Color normalColor = new(0.75f, 0.75f, 0.78f, 1f);
        [SerializeField] Color highlightColor = new(1f, 0.82f, 0.12f, 1f);

        public override LevelType LevelType => Aim.Models.LevelType.ShootingGallery;
        public override int RequiredHits => Columns * Rows;
        public GalleryTargetView[] TargetPrefabs => targetPrefabs;
        public int Columns => Mathf.Max(1, columns);
        public int Rows => Mathf.Max(1, rows);
        public Vector2 Spacing => spacing;
        public float FacingYaw => facingYaw;
        public float RetargetDelay => Mathf.Max(0.05f, retargetDelay);
        public Color NormalColor => normalColor;
        public Color HighlightColor => highlightColor;
    }
}
