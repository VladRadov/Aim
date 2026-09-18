using System;
using System.Collections.Generic;

namespace YG
{
    [Serializable]
    public sealed class AimLevelRecordSave
    {
        public string id;
        public float accuracy = -1f;
        public float time = float.MaxValue;
    }

    public partial class SavesYG
    {
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.85f;
        public int coins;
        public int campaignIndex;
        public string ownedWeapons = "";
        public string equippedWeapon = "";
        public List<AimLevelRecordSave> levelRecords = new();
    }
}
