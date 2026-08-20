using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class SettingsView : MonoBehaviour
    {
        [SerializeField] Button settingsButton;
        [SerializeField] Button closeButton;
        [SerializeField] Button dimmerButton;
        [SerializeField] CanvasGroup dimmerGroup;
        [SerializeField] CanvasGroup panelGroup;
        [SerializeField] RectTransform panelRoot;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;
        [SerializeField] float animationDuration = 0.28f;

        readonly Subject<Unit> _openRequested = new();
        readonly Subject<Unit> _closeRequested = new();
        readonly Subject<float> _musicChanged = new();
        readonly Subject<float> _sfxChanged = new();

        CancellationTokenSource _animationCts;
        bool _isAnimating;
        bool _ignoreSliderEvents;

        public IObservable<Unit> OpenRequested => _openRequested;
        public IObservable<Unit> CloseRequested => _closeRequested;
        public IObservable<float> MusicChanged => _musicChanged;
        public IObservable<float> SfxChanged => _sfxChanged;

        void Awake()
        {
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => _openRequested.OnNext(Unit.Default));

            if (closeButton != null)
                closeButton.onClick.AddListener(() => _closeRequested.OnNext(Unit.Default));

            if (dimmerButton != null)
                dimmerButton.onClick.AddListener(() => _closeRequested.OnNext(Unit.Default));

            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(value =>
                {
                    if (!_ignoreSliderEvents)
                        _musicChanged.OnNext(value);
                });

            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(value =>
                {
                    if (!_ignoreSliderEvents)
                        _sfxChanged.OnNext(value);
                });

            SetClosedImmediate();
        }

        public void SetSliderValues(float music, float sfx)
        {
            _ignoreSliderEvents = true;
            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(music);
            if (sfxSlider != null)
                sfxSlider.SetValueWithoutNotify(sfx);
            _ignoreSliderEvents = false;
        }

        public void ShowAnimated()
        {
            AnimateOpenAsync().Forget();
        }

        public void HideAnimated()
        {
            AnimateCloseAsync().Forget();
        }

        async UniTaskVoid AnimateOpenAsync()
        {
            CancelAnimation();
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;
            _isAnimating = true;

            if (dimmerGroup != null)
            {
                dimmerGroup.gameObject.SetActive(true);
                dimmerGroup.blocksRaycasts = true;
                dimmerGroup.interactable = true;
            }

            if (panelGroup != null)
            {
                panelGroup.gameObject.SetActive(true);
                panelGroup.blocksRaycasts = true;
                panelGroup.interactable = true;
            }

            var duration = Mathf.Max(0.01f, animationDuration);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseOutBack(t);

                if (dimmerGroup != null)
                    dimmerGroup.alpha = Mathf.Lerp(0f, 0.72f, EaseOutCubic(t));

                if (panelGroup != null)
                    panelGroup.alpha = EaseOutCubic(t);

                if (panelRoot != null)
                {
                    var scale = Mathf.Lerp(0.82f, 1f, eased);
                    panelRoot.localScale = new Vector3(scale, scale, 1f);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (dimmerGroup != null)
                dimmerGroup.alpha = 0.72f;
            if (panelGroup != null)
                panelGroup.alpha = 1f;
            if (panelRoot != null)
                panelRoot.localScale = Vector3.one;

            _isAnimating = false;
        }

        async UniTaskVoid AnimateCloseAsync()
        {
            CancelAnimation();
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;
            _isAnimating = true;

            var duration = Mathf.Max(0.01f, animationDuration * 0.85f);
            var elapsed = 0f;
            var startDimmer = dimmerGroup != null ? dimmerGroup.alpha : 0f;
            var startPanel = panelGroup != null ? panelGroup.alpha : 0f;
            var startScale = panelRoot != null ? panelRoot.localScale.x : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseInCubic(t);

                if (dimmerGroup != null)
                    dimmerGroup.alpha = Mathf.Lerp(startDimmer, 0f, eased);

                if (panelGroup != null)
                    panelGroup.alpha = Mathf.Lerp(startPanel, 0f, eased);

                if (panelRoot != null)
                {
                    var scale = Mathf.Lerp(startScale, 0.88f, eased);
                    panelRoot.localScale = new Vector3(scale, scale, 1f);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            SetClosedImmediate();
            _isAnimating = false;
        }

        void SetClosedImmediate()
        {
            if (dimmerGroup != null)
            {
                dimmerGroup.alpha = 0f;
                dimmerGroup.blocksRaycasts = false;
                dimmerGroup.interactable = false;
                dimmerGroup.gameObject.SetActive(false);
            }

            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.blocksRaycasts = false;
                panelGroup.interactable = false;
                panelGroup.gameObject.SetActive(false);
            }

            if (panelRoot != null)
                panelRoot.localScale = new Vector3(0.82f, 0.82f, 1f);
        }

        void CancelAnimation()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;
            _isAnimating = false;
        }

        void OnDestroy()
        {
            CancelAnimation();
            _openRequested.Dispose();
            _closeRequested.Dispose();
            _musicChanged.Dispose();
            _sfxChanged.Dispose();
        }

        static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        static float EaseInCubic(float t) => t * t * t;

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
