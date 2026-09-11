using System;

namespace Aim.Services
{
    public interface IInterstitialAdService
    {
        /// <summary>
        /// Attempts to show a fullscreen interstitial. Always invokes onClosed
        /// when the ad finishes, fails, or is skipped (timer/cooldown).
        /// </summary>
        void Show(Action onClosed);
    }

    public sealed class NullInterstitialAdService : IInterstitialAdService
    {
        public void Show(Action onClosed) => onClosed?.Invoke();
    }

    public static class InterstitialAdService
    {
        public static IInterstitialAdService Current { get; set; } = new NullInterstitialAdService();
    }
}
