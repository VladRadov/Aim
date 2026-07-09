using System;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class CrosshairView : MonoBehaviour
    {
        [SerializeField] Image dotImage;

        Color _defaultColor = Color.white;
        Color _hitColor = new(1f, 0.35f, 0.35f, 1f);
        float _hitFlashDuration = 0.12f;
        bool _isFlashing;

        public void Configure(Color defaultColor, Color hitColor, float hitFlashDuration)
        {
            _defaultColor = defaultColor;
            _hitColor = hitColor;
            _hitFlashDuration = hitFlashDuration;
            dotImage.color = _defaultColor;
        }

        public void FlashHit()
        {
            if (_isFlashing)
                return;

            FlashHitAsync().Forget();
        }

        async UniTaskVoid FlashHitAsync()
        {
            _isFlashing = true;
            dotImage.color = _hitColor;

            await UniTask.Delay(TimeSpan.FromSeconds(_hitFlashDuration));

            dotImage.color = _defaultColor;
            _isFlashing = false;
        }
    }
}
