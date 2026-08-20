using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class CoinsHudView : MonoBehaviour
    {
        [SerializeField] Text coinsText;
        [SerializeField] Image coinsIcon;
        [SerializeField] float tweenDuration = 0.45f;

        float _displayed;
        int _target;
        bool _initialized;
        CancellationTokenSource _tweenCts;

        public void SetCoins(int amount)
        {
            _target = Mathf.Max(0, amount);

            if (!_initialized)
            {
                _initialized = true;
                _displayed = _target;
                ApplyText(_target);
                return;
            }

            AnimateToAsync(_target).Forget();
        }

        async UniTaskVoid AnimateToAsync(int target)
        {
            _tweenCts?.Cancel();
            _tweenCts?.Dispose();
            _tweenCts = new CancellationTokenSource();
            var token = _tweenCts.Token;

            var start = _displayed;
            var end = target;
            var duration = Mathf.Max(0.05f, tweenDuration);
            var elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    var eased = 1f - Mathf.Pow(1f - t, 3f);
                    _displayed = Mathf.Lerp(start, end, eased);
                    ApplyText(Mathf.RoundToInt(_displayed));
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                _displayed = end;
                ApplyText(end);
            }
            catch (OperationCanceledException)
            {
            }
        }

        void ApplyText(int value)
        {
            if (coinsText != null)
                coinsText.text = Mathf.Max(0, value).ToString();
        }

        void OnDestroy()
        {
            _tweenCts?.Cancel();
            _tweenCts?.Dispose();
        }
    }
}
