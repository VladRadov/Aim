using System;
using System.Threading;
using Aim.Models;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class LevelWinView : MonoBehaviour
    {
        [SerializeField] Button nextLevelButton;
        [SerializeField] Button retryButton;
        [SerializeField] Button rewardAdButton;
        [SerializeField] Button menuButton;
        [SerializeField] CanvasGroup dimmerGroup;
        [SerializeField] CanvasGroup panelGroup;
        [SerializeField] RectTransform panelRoot;
        [SerializeField] Text titleText;
        [SerializeField] Text recordText;
        [SerializeField] Image[] starImages = Array.Empty<Image>();
        [SerializeField] Color starLitColor = new(1f, 0.84f, 0.28f, 1f);
        [SerializeField] Color starDimColor = new(0.28f, 0.32f, 0.40f, 0.85f);
        [SerializeField] Text accuracyValueText;
        [SerializeField] Text timeValueText;
        [SerializeField] Text coinsValueText;
        [SerializeField] Text shotsValueText;
        [SerializeField] Text accuracyLabelText;
        [SerializeField] Text timeLabelText;
        [SerializeField] Text coinsLabelText;
        [SerializeField] Text shotsLabelText;
        [SerializeField] float animationDuration = 0.28f;

        readonly Subject<Unit> _nextLevelRequested = new();
        readonly Subject<Unit> _retryRequested = new();
        readonly Subject<Unit> _rewardAdRequested = new();
        readonly Subject<Unit> _menuRequested = new();

        CancellationTokenSource _animationCts;
        float _fitScale = 1f;
        int _boundStars;

        public IObservable<Unit> NextLevelRequested => _nextLevelRequested;
        public IObservable<Unit> RetryRequested => _retryRequested;
        public IObservable<Unit> RewardAdRequested => _rewardAdRequested;
        public IObservable<Unit> MenuRequested => _menuRequested;

        void Awake()
        {
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(() => _nextLevelRequested.OnNext(Unit.Default));

            if (retryButton != null)
                retryButton.onClick.AddListener(() => _retryRequested.OnNext(Unit.Default));

            if (rewardAdButton != null)
                rewardAdButton.onClick.AddListener(() => _rewardAdRequested.OnNext(Unit.Default));

            if (menuButton != null)
                menuButton.onClick.AddListener(() => _menuRequested.OnNext(Unit.Default));

            RecalculateFitScale();
            SetClosedImmediate();
        }

        public void BindSummary(LevelRunSummary summary)
        {
            _boundStars = summary.Stars;

            if (titleText != null)
                titleText.text = summary.IsWin ? "Уровень пройден" : "Уровень провален";

            if (recordText != null)
            {
                if (summary.IsNewRecord && !string.IsNullOrEmpty(summary.RecordNote))
                {
                    recordText.gameObject.SetActive(true);
                    recordText.text = summary.RecordNote;
                }
                else
                {
                    recordText.gameObject.SetActive(false);
                    recordText.text = string.Empty;
                }
            }

            SetStars(summary.Stars);

            if (accuracyLabelText != null)
                accuracyLabelText.text = "Точность";
            if (timeLabelText != null)
                timeLabelText.text = "Время";
            if (coinsLabelText != null)
                coinsLabelText.text = "Монеты";
            if (shotsLabelText != null)
                shotsLabelText.text = summary.AllowsShooting ? "Выстрелы" : "Попадания";

            if (accuracyValueText != null)
                accuracyValueText.text = summary.AccuracyLabel;
            if (timeValueText != null)
                timeValueText.text = summary.TimeLabel;
            if (coinsValueText != null)
                coinsValueText.text = $"+{summary.CoinsEarned}";
            if (shotsValueText != null)
            {
                shotsValueText.text = summary.AllowsShooting
                    ? $"{summary.ScoreHits}/{summary.ShotsFired}"
                    : $"{summary.ScoreHits}/{summary.RequiredHits}";
            }
        }

        public void SetNextLevelAvailable(bool available)
        {
            if (nextLevelButton == null)
                return;

            nextLevelButton.interactable = available;
            nextLevelButton.gameObject.SetActive(available);
            var group = nextLevelButton.GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = available ? 1f : 0.45f;

            LayoutButtons(rewardAdButton != null && rewardAdButton.gameObject.activeSelf);
        }

        public void SetRewardVisible(bool visible)
        {
            if (rewardAdButton == null)
                return;

            rewardAdButton.gameObject.SetActive(visible);
            LayoutButtons(visible);
        }

        void LayoutButtons(bool rewardVisible)
        {
            if (retryButton == null)
                return;

            var retryRect = retryButton.transform as RectTransform;
            var nextRect = nextLevelButton != null ? nextLevelButton.transform as RectTransform : null;
            var rewardRect = rewardAdButton != null ? rewardAdButton.transform as RectTransform : null;
            var y = retryRect != null ? retryRect.anchoredPosition.y : -100f;
            var nextVisible = nextLevelButton != null && nextLevelButton.gameObject.activeSelf;

            if (rewardVisible && nextVisible)
            {
                if (retryRect != null)
                    retryRect.anchoredPosition = new Vector2(-118f, y);
                if (nextRect != null)
                    nextRect.anchoredPosition = new Vector2(0f, y);
                if (rewardRect != null)
                    rewardRect.anchoredPosition = new Vector2(118f, y);
            }
            else if (rewardVisible)
            {
                if (retryRect != null)
                    retryRect.anchoredPosition = new Vector2(-66f, y);
                if (rewardRect != null)
                    rewardRect.anchoredPosition = new Vector2(66f, y);
            }
            else if (nextVisible)
            {
                if (retryRect != null)
                    retryRect.anchoredPosition = new Vector2(-66f, y);
                if (nextRect != null)
                    nextRect.anchoredPosition = new Vector2(66f, y);
            }
            else
            {
                if (retryRect != null)
                    retryRect.anchoredPosition = new Vector2(0f, y);
            }
        }

        void RecalculateFitScale()
        {
            _fitScale = 1f;
            if (panelRoot == null)
                return;

            var rootRect = transform as RectTransform;
            if (rootRect == null)
                return;

            var canvasHeight = rootRect.rect.height;
            var canvasWidth = rootRect.rect.width;
            if (canvasHeight < 1f || canvasWidth < 1f)
                return;

            var panelSize = panelRoot.sizeDelta;
            if (panelSize.x < 1f || panelSize.y < 1f)
                return;

            _fitScale = Mathf.Clamp(
                Mathf.Min((canvasWidth * 0.92f) / panelSize.x, (canvasHeight * 0.88f) / panelSize.y),
                0.55f,
                1f);
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

        public void ShowAnimated()
        {
            RecalculateFitScale();
            AnimateOpenAsync().Forget();
        }

        public void HideAnimated() => AnimateCloseAsync().Forget();

        void SetStars(int stars)
        {
            if (starImages == null)
                return;

            for (var i = 0; i < starImages.Length; i++)
            {
                var image = starImages[i];
                if (image == null)
                    continue;

                var lit = i < stars;
                image.color = lit ? starLitColor : starDimColor;
                image.transform.localScale = lit ? Vector3.one : new Vector3(0.92f, 0.92f, 1f);
            }
        }

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

            if (starImages != null)
            {
                for (var i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] != null)
                        starImages[i].transform.localScale = Vector3.zero;
                }
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
                    var scale = Mathf.Lerp(_fitScale * 0.86f, _fitScale, eased);
                    panelRoot.localScale = new Vector3(scale, scale, 1f);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (dimmerGroup != null)
                dimmerGroup.alpha = 0.72f;
            if (panelGroup != null)
                panelGroup.alpha = 1f;
            if (panelRoot != null)
                panelRoot.localScale = new Vector3(_fitScale, _fitScale, 1f);

            await AnimateStarsAsync(token);
        }

        async UniTask AnimateStarsAsync(CancellationToken token)
        {
            if (starImages == null)
                return;

            for (var i = 0; i < starImages.Length; i++)
            {
                var image = starImages[i];
                if (image == null)
                    continue;

                var target = i < _boundStars
                    ? Vector3.one
                    : new Vector3(0.92f, 0.92f, 1f);

                // Scale-in each star in sequence.
                var elapsed = 0f;
                const float starDuration = 0.12f;
                while (elapsed < starDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = EaseOutBack(Mathf.Clamp01(elapsed / starDuration));
                    image.transform.localScale = Vector3.Lerp(Vector3.zero, target, t);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                image.transform.localScale = target;
            }
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
                    var scale = Mathf.Lerp(startScale, _fitScale * 0.88f, eased);
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
            {
                var closed = _fitScale * 0.86f;
                panelRoot.localScale = new Vector3(closed, closed, 1f);
            }
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
            _retryRequested.Dispose();
            _rewardAdRequested.Dispose();
            _menuRequested.Dispose();
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
