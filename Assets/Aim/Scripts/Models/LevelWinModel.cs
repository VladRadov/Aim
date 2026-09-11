using System;
using UniRx;

namespace Aim.Models
{
    public sealed class LevelWinModel : IDisposable
    {
        readonly ReactiveProperty<bool> _isOpen = new(false);
        readonly ReactiveProperty<LevelRunSummary> _summary = new(default);
        readonly ReactiveProperty<bool> _hasNextLevel = new(false);
        readonly ReactiveProperty<bool> _rewardClaimed = new(false);
        readonly ReactiveProperty<bool> _showReward = new(false);

        public IReadOnlyReactiveProperty<bool> IsOpen => _isOpen;
        public IReadOnlyReactiveProperty<LevelRunSummary> Summary => _summary;
        public IReadOnlyReactiveProperty<bool> HasNextLevel => _hasNextLevel;
        public IReadOnlyReactiveProperty<bool> RewardClaimed => _rewardClaimed;
        public IReadOnlyReactiveProperty<bool> ShowReward => _showReward;

        public void Open(LevelRunSummary summary, bool hasNextLevel, bool showReward)
        {
            _summary.Value = summary;
            _hasNextLevel.Value = hasNextLevel;
            _showReward.Value = showReward && summary.IsWin;
            _rewardClaimed.Value = false;
            _isOpen.Value = true;
        }

        public void MarkRewardClaimed() => _rewardClaimed.Value = true;

        public void Close() => _isOpen.Value = false;

        public void Dispose()
        {
            _isOpen.Dispose();
            _summary.Dispose();
            _hasNextLevel.Dispose();
            _rewardClaimed.Dispose();
            _showReward.Dispose();
        }
    }
}
