using Aim.Models;
using Aim.Views;
using UnityEngine;

namespace Aim.Config
{
    [CreateAssetMenu(fileName = "CharacterHeadshotLevel", menuName = "Aim/Levels/Character Headshot")]
    public sealed class CharacterHeadshotLevelDefinition : LevelDefinition
    {
        [Header("Character Headshot")]
        [SerializeField] CharacterTargetView characterPrefab;
        [SerializeField] CharacterMoveMode characterMoveMode = CharacterMoveMode.Random;
        [SerializeField] float characterSpawnInterval = 1.2f;
        [SerializeField] int maxCharactersAlive = 3;
        [SerializeField] float characterRunSpeed = 2.5f;
        [SerializeField] float characterJumpHeight = 1.2f;
        [SerializeField] float characterJumpInterval = 1.1f;
        [SerializeField] float characterLifetime = 8f;

        public override LevelType LevelType => Aim.Models.LevelType.CharacterHeadshot;
        public CharacterTargetView CharacterPrefab => characterPrefab;
        public CharacterMoveMode CharacterMoveMode => characterMoveMode;
        public float CharacterSpawnInterval => characterSpawnInterval;
        public int MaxCharactersAlive => maxCharactersAlive;
        public float CharacterRunSpeed => characterRunSpeed;
        public float CharacterJumpHeight => characterJumpHeight;
        public float CharacterJumpInterval => characterJumpInterval;
        public float CharacterLifetime => characterLifetime;
    }
}
