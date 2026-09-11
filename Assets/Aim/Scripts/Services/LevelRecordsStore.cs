using Aim.Config;
using UnityEngine;

namespace Aim.Services
{
    public sealed class LevelRecordsStore
    {
        const string Prefix = "Aim.LevelRecord.";

        public bool TryUpdateRecord(
            LevelDefinition definition,
            bool won,
            float accuracy01,
            float durationSeconds,
            out string note)
        {
            note = string.Empty;
            if (definition == null || !won)
                return false;

            var key = Prefix + definition.name;
            var bestAccuracy = PlayerPrefs.GetFloat(key + ".Accuracy", -1f);
            var bestTime = PlayerPrefs.GetFloat(key + ".Time", float.MaxValue);
            var isNew = false;

            if (definition.AllowsShooting)
            {
                if (accuracy01 > bestAccuracy + 0.0001f)
                {
                    PlayerPrefs.SetFloat(key + ".Accuracy", accuracy01);
                    note = "Новый рекорд точности!";
                    isNew = true;
                }
            }

            if (durationSeconds < bestTime - 0.01f)
            {
                PlayerPrefs.SetFloat(key + ".Time", durationSeconds);
                note = isNew ? "Новые рекорды!" : "Новый рекорд времени!";
                isNew = true;
            }

            if (isNew)
                PlayerPrefs.Save();

            return isNew;
        }
    }
}
