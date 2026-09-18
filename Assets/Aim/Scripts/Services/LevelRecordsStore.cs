using Aim.Config;
using UnityEngine;

namespace Aim.Services
{
    public sealed class LevelRecordsStore
    {
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

            var record = GameSaveService.Current.Data.GetOrCreateRecord(definition.name);
            if (record == null)
                return false;

            var bestAccuracy = record.Accuracy;
            var bestTime = record.Time;
            var isNew = false;

            if (definition.AllowsShooting)
            {
                if (accuracy01 > bestAccuracy + 0.0001f)
                {
                    record.Accuracy = accuracy01;
                    note = "Новый рекорд точности!";
                    isNew = true;
                }
            }

            if (durationSeconds < bestTime - 0.01f)
            {
                record.Time = durationSeconds;
                note = isNew ? "Новые рекорды!" : "Новый рекорд времени!";
                isNew = true;
            }

            if (isNew)
                GameSaveService.Current.Flush();

            return isNew;
        }
    }
}
