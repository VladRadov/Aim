using System;
using UnityEngine;

namespace Aim.Models
{
    public readonly struct LevelRunSummary
    {
        public readonly string LevelName;
        public readonly bool IsWin;
        public readonly bool AllowsShooting;
        public readonly int ScoreHits;
        public readonly int RequiredHits;
        public readonly int ShotsFired;
        public readonly int AmmoSpent;
        public readonly int AmmoRemaining;
        public readonly float DurationSeconds;
        public readonly float Accuracy01;
        public readonly int CoinsEarned;
        public readonly int Stars;
        public readonly bool IsNewRecord;
        public readonly string RecordNote;

        public LevelRunSummary(
            string levelName,
            bool isWin,
            bool allowsShooting,
            int scoreHits,
            int requiredHits,
            int shotsFired,
            int ammoSpent,
            int ammoRemaining,
            float durationSeconds,
            float accuracy01,
            int coinsEarned,
            int stars,
            bool isNewRecord,
            string recordNote)
        {
            LevelName = levelName ?? string.Empty;
            IsWin = isWin;
            AllowsShooting = allowsShooting;
            ScoreHits = Mathf.Max(0, scoreHits);
            RequiredHits = Mathf.Max(0, requiredHits);
            ShotsFired = Mathf.Max(0, shotsFired);
            AmmoSpent = Mathf.Max(0, ammoSpent);
            AmmoRemaining = Mathf.Max(0, ammoRemaining);
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            Accuracy01 = Mathf.Clamp01(accuracy01);
            CoinsEarned = Mathf.Max(0, coinsEarned);
            Stars = Mathf.Clamp(stars, 0, 3);
            IsNewRecord = isNewRecord;
            RecordNote = recordNote ?? string.Empty;
        }

        public string AccuracyLabel =>
            !AllowsShooting
                ? "—"
                : ShotsFired <= 0
                    ? "0%"
                    : $"{Mathf.RoundToInt(Accuracy01 * 100f)}%";

        public string TimeLabel
        {
            get
            {
                var total = Mathf.FloorToInt(DurationSeconds);
                var minutes = total / 60;
                var seconds = total % 60;
                return $"{minutes}:{seconds:00}";
            }
        }
    }
}
