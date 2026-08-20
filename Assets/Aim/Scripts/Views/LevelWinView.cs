using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class LevelWinView : MonoBehaviour
    {
        [SerializeField] Button nextLevelButton;
        [SerializeField] Button rewardAdButton;
        [SerializeField] CanvasGroup dimmerGroup;
        [SerializeField] CanvasGroup panelGroup;
        [SerializeField] RectTransform panelRoot;
        [SerializeField] Text titleText;
        [SerializeField] Text winBonusText;
        [SerializeField] float animationDuration = 0.28f;

        readonly Subject<Unit> _nextLevelRequested = new();
        readonly Subject<Unit> _rewardAdRequested = new();

        CancellationTokenSource _animationCts;

        public IObservable<Unit> NextLevelRequested => _nextLevelRequested;
        public IObservable<Unit> RewardAdRequested => _rewardAdRequested;

        void Awake()
        {
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(() => _nextLevelRequested.OnNext(Unit.Default));

            if (rewardAdButton != null)
                rewardAdButton.onClick.AddListener(() => _rewardAdRequested.OnNext(Unit.Default));

            if (titleText != null)
                titleText.text = "Уровень пройден";

            SetClosedImmediate();
        }

        public void SetWinBonus(int amount)
        {
            if (winBonusText != null)
                winBonusText.text = amount > 0 ? $"+{amount}" : string.Empty;
        }

        public void SetNextLevelAvailable(bool available)
        {
            if (nextLevelButton == null)
                return;

            nextLevelButton.interactable = available;
            nextLevelButton.gameObject.SetActive(true);
            var group = nextLevelButton.GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = available ? 1f : 0.45f;
        }

        public void SetRewardAvailable(bool available)
        {
            if (rewardAdButton == null)
                return;

            rewardAdButton.interactable = available;
            var group = rewardAdButton.GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = available ? 1f : 0.45f;
        }

        public void ShowAnimated() => AnimateOpenAsync().Forget();

        public void HideAnimated() => AnimateCloseAsync().Forget();

        async UniTaskVoid AnimateOpenAsync()
        {
            CancelAnimation();
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;

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
        }

        async UniTaskVoid AnimateCloseAsync()
        {
            CancelAnimation();
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;

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
        }

        void OnDestroy()
        {
            CancelAnimation();
            _nextLevelRequested.Dispose();
            _rewardAdRequested.Dispose();
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
