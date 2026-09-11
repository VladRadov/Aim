using System;

namespace Aim.Services
{
    public interface IRewardedAdService
    {
        void Show(string rewardId, Action onRewarded, Action onClosedWithoutReward = null);
    }

    public sealed class NullRewardedAdService : IRewardedAdService
    {
        public void Show(string rewardId, Action onRewarded, Action onClosedWithoutReward = null)
        {
            onClosedWithoutReward?.Invoke();
        }
    }

    public static class RewardedAdService
    {
        public static IRewardedAdService Current { get; set; } = new NullRewardedAdService();
    }
}
