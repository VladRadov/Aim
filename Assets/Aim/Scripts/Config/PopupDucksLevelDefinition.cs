using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "PopupDucksLevel", menuName = "Aim/Levels/Pop-up Ducks")]
    public sealed class PopupDucksLevelDefinition : LevelDefinition
    {
        [Header("Pop-up Ducks")]
        [SerializeField] PeekTargetView duckPrefab;
        [SerializeField] int columns = 5;
        [SerializeField] int rows = 2;
        [SerializeField] Vector2 spacing = new(1.6f, 1.3f);
        [SerializeField] float upHeight = 1.1f;
        [SerializeField] float downOffset = 1.1f;
        [SerializeField] float upDurationMin = 0.7f;
        [SerializeField] float upDurationMax = 1.6f;
        [SerializeField] float downDurationMin = 0.6f;
        [SerializeField] float downDurationMax = 1.8f;
        [SerializeField] float moveDuration = 0.2f;

        public override LevelType LevelType => LevelType.PopupDucks;
        public PeekTargetView DuckPrefab => duckPrefab;
        public int Columns => Mathf.Max(1, columns);
        public int Rows => Mathf.Max(1, rows);
        public Vector2 Spacing => spacing;
        public float UpHeight => upHeight;
        public float DownOffset => Mathf.Max(0.2f, downOffset);
        public float UpDurationMin => Mathf.Max(0.1f, upDurationMin);
        public float UpDurationMax => Mathf.Max(UpDurationMin, upDurationMax);
        public float DownDurationMin => Mathf.Max(0.1f, downDurationMin);
        public float DownDurationMax => Mathf.Max(DownDurationMin, downDurationMax);
        public float MoveDuration => Mathf.Max(0.05f, moveDuration);
        public override int RequiredHits => Columns * Rows;
    }
}
