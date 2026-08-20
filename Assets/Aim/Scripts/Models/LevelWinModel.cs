using System;
using UniRx;

namespace Aim.Models
{
    public sealed class LevelWinModel : IDisposable
    {
        readonly ReactiveProperty<bool> _isOpen = new(false);
        readonly ReactiveProperty<int> _awardedWinBonus = new(0);
        readonly ReactiveProperty<bool> _hasNextLevel = new(false);
        readonly ReactiveProperty<bool> _rewardClaimed = new(false);

        public IReadOnlyReactiveProperty<bool> IsOpen => _isOpen;
        public IReadOnlyReactiveProperty<int> AwardedWinBonus => _awardedWinBonus;
        public IReadOnlyReactiveProperty<bool> HasNextLevel => _hasNextLevel;
        public IReadOnlyReactiveProperty<bool> RewardClaimed => _rewardClaimed;

        public void Open(int winBonus, bool hasNextLevel)
        {
            _awardedWinBonus.Value = System.Math.Max(0, winBonus);
            _hasNextLevel.Value = hasNextLevel;
            _rewardClaimed.Value = false;
            _isOpen.Value = true;
        }

        public void MarkRewardClaimed() => _rewardClaimed.Value = true;

        public void Close() => _isOpen.Value = false;

        public void Dispose()
        {
            _isOpen.Dispose();
            _awardedWinBonus.Dispose();
            _hasNextLevel.Dispose();
            _rewardClaimed.Dispose();
        }
    }
}
