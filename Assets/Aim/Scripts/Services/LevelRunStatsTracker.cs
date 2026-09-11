using Aim.Config;
using Aim.Models;
using UnityEngine;

namespace Aim.Services
{
    public sealed class LevelRunStatsTracker
    {
        readonly LevelRecordsStore _records = new();
        readonly ShootModel _shootModel;
        readonly SessionModel _sessionModel;
        readonly CoinsModel _coinsModel;

        LevelDefinition _definition;
        float _startedAt;
        int _coinsAtStart;
        bool _running;

        public LevelRunStatsTracker(
            ShootModel shootModel,
            SessionModel sessionModel,
            CoinsModel coinsModel)
        {
            _shootModel = shootModel;
            _sessionModel = sessionModel;
            _coinsModel = coinsModel;
        }

        public void Begin(LevelDefinition definition)
        {
            _definition = definition;
            _startedAt = Time.unscaledTime;
            _coinsAtStart = _coinsModel != null ? _coinsModel.Balance.Value : 0;
            _shootModel?.ResetStats();
            _running = definition != null;
        }

        public LevelRunSummary BuildSummary(bool isWin, int extraCoins = 0)
        {
            if (!_running || _definition == null || _sessionModel == null)
            {
                return new LevelRunSummary(
                    "Уровень",
                    isWin,
                    true,
                    0, 0, 0, 0, 0,
                    0f, 0f, 0, 0, false, string.Empty);
            }

            var allowsShooting = _definition.AllowsShooting;
            var scoreHits = _sessionModel.Hits.Value;
            var required = _sessionModel.RequiredHits.Value;
            var shots = _shootModel != null ? _shootModel.Shots.Value : 0;
            var ammoCapacity = _sessionModel.AmmoCapacity.Value;
            var ammoRemaining = _sessionModel.AmmoRemaining.Value;
            var ammoSpent = allowsShooting ? Mathf.Max(0, ammoCapacity - ammoRemaining) : 0;
            var duration = Mathf.Max(0f, Time.unscaledTime - _startedAt);
            var accuracy = !allowsShooting
                ? 1f
                : shots > 0
                    ? Mathf.Clamp01((float)scoreHits / shots)
                    : 0f;

            var coinsNow = _coinsModel != null ? _coinsModel.Balance.Value : _coinsAtStart;
            var coinsEarned = Mathf.Max(0, coinsNow - _coinsAtStart) + Mathf.Max(0, extraCoins);

            var stars = CalculateStars(
                isWin,
                _definition,
                allowsShooting,
                accuracy,
                ammoCapacity,
                ammoRemaining,
                duration,
                required);
            var isNewRecord = _records.TryUpdateRecord(
                _definition,
                isWin,
                accuracy,
                duration,
                out var recordNote);

            return new LevelRunSummary(
                _definition.DisplayName,
                isWin,
                allowsShooting,
                scoreHits,
                required,
                shots,
                ammoSpent,
                ammoRemaining,
                duration,
                accuracy,
                coinsEarned,
                stars,
                isNewRecord,
                recordNote);
        }

        static int CalculateStars(
            bool isWin,
            LevelDefinition definition,
            bool allowsShooting,
            float accuracy,
            int ammoCapacity,
            int ammoRemaining,
            float duration,
            int requiredHits)
        {
            if (!isWin)
                return 0;

            var stars = 1;

            if (allowsShooting)
            {
                if (accuracy >= 0.65f)
                    stars = 2;
                if (accuracy >= 0.85f)
                {
                    var ammoRatio = ammoCapacity > 0 ? (float)ammoRemaining / ammoCapacity : 1f;
                    if (ammoRatio >= 0.2f || accuracy >= 0.95f)
                        stars = 3;
                }

                return stars;
            }

            // Tracking / aim-only: thresholds based on real kill time, not a flat formula.
            var perfect = EstimateTrackingPerfectSeconds(definition, requiredHits);
            var twoStarLimit = perfect * 2.1f;
            var threeStarLimit = perfect * 1.45f;

            if (duration <= twoStarLimit)
                stars = 2;
            if (duration <= threeStarLimit)
                stars = 3;

            return stars;
        }

        static float EstimateTrackingPerfectSeconds(LevelDefinition definition, int requiredHits)
        {
            var hits = Mathf.Max(1, requiredHits);

            if (definition is TrackingBallLevelDefinition ball)
            {
                var secondsPerKill = ball.MaxHealth / ball.DrainPerSecond + ball.RespawnDelay;
                return hits * secondsPerKill;
            }

            if (definition is TrackingMoversLevelDefinition movers)
            {
                var secondsPerKill = movers.MaxHealth / movers.DrainPerSecond + movers.RespawnDelay;
                // Switching targets / spawn pacing adds overhead beyond pure drain time.
                return hits * secondsPerKill * 1.25f;
            }

            // Fallback for other non-shooting modes.
            return Mathf.Max(20f, hits * 6f);
        }
    }
}
