using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "PeekTargetsLevel", menuName = "Aim/Levels/Peek Targets")]
    public sealed class PeekTargetsLevelDefinition : LevelDefinition
    {
        [Header("Peek / Cover")]
        [SerializeField] PeekTargetView peekPrefab;
        [SerializeField] int coverCount = 4;
        [SerializeField] float coverSpacing = 2.2f;
        [SerializeField] Vector3 coverSize = new(0.9f, 2f, 0.35f);
        [SerializeField] float peekSideOffset = 1.15f;
        [SerializeField] float hideDepth = 0.55f;
        [SerializeField] float targetHeight = 1.15f;
        [SerializeField] float peekDurationMin = 0.5f;
        [SerializeField] float peekDurationMax = 1.5f;
        [SerializeField] float hideDurationMin = 0.8f;
        [SerializeField] float hideDurationMax = 2f;
        [SerializeField] float moveDuration = 0.18f;

        public override LevelType LevelType => LevelType.PeekTargets;
        public PeekTargetView PeekPrefab => peekPrefab;
        public int CoverCount => Mathf.Max(1, coverCount);
        public float CoverSpacing => Mathf.Max(0.5f, coverSpacing);
        public Vector3 CoverSize => coverSize;
        public float PeekSideOffset => Mathf.Max(0.3f, peekSideOffset);
        public float HideDepth => Mathf.Max(0.1f, hideDepth);
        public float TargetHeight => targetHeight;
        public float PeekDurationMin => Mathf.Max(0.1f, peekDurationMin);
        public float PeekDurationMax => Mathf.Max(PeekDurationMin, peekDurationMax);
        public float HideDurationMin => Mathf.Max(0.1f, hideDurationMin);
        public float HideDurationMax => Mathf.Max(HideDurationMin, hideDurationMax);
        public float MoveDuration => Mathf.Max(0.05f, moveDuration);
    }
}
