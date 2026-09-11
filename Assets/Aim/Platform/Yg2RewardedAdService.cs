using System;
using Aim.Services;
using UnityEngine;
using YG;

namespace Aim.Platform
{
    /// <summary>
    /// Lives outside Aim.asmdef so it can reference PluginYG2 (Assembly-CSharp).
    /// </summary>
    public sealed class Yg2RewardedAdService : IRewardedAdService
    {
        const string DefaultRewardId = "double_coins";

        public void Show(string rewardId, Action onRewarded, Action onClosedWithoutReward = null)
        {
            if (string.IsNullOrEmpty(rewardId))
                rewardId = DefaultRewardId;

#if RewardedAdv_yg
            var completed = false;

            void OnClose()
            {
                YG2.onCloseRewardedAdv -= OnClose;
                YG2.onErrorRewardedAdv -= OnError;
                if (!completed)
                    onClosedWithoutReward?.Invoke();
            }

            void OnError()
            {
                YG2.onCloseRewardedAdv -= OnClose;
                YG2.onErrorRewardedAdv -= OnError;
                if (!completed)
                    onClosedWithoutReward?.Invoke();
            }

            YG2.onCloseRewardedAdv += OnClose;
            YG2.onErrorRewardedAdv += OnError;

            YG2.RewardedAdvShow(rewardId, () =>
            {
                completed = true;
                onRewarded?.Invoke();
            });
#else
            Debug.LogWarning("Yg2RewardedAdService: RewardedAdv module define is missing (RewardedAdv_yg).");
            onClosedWithoutReward?.Invoke();
#endif
        }
    }

    public static class Yg2RewardedAdBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            RewardedAdService.Current = new Yg2RewardedAdService();
        }
    }
}
