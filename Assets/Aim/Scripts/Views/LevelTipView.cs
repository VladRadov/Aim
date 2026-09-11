using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class LevelTipView : MonoBehaviour
    {
        [SerializeField] CanvasGroup panelGroup;
        [SerializeField] RectTransform panelRoot;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] Text modeText;
        [SerializeField] float slideDistance = 90f;
        [SerializeField] float animationDuration = 0.28f;
        [SerializeField] float visibleDuration = 4.0f;
        [SerializeField] float topInset = 20f;
        [SerializeField] Vector2 panelSize = new(460f, 132f);
        [SerializeField] float textPaddingX = 18f;

        CancellationTokenSource _animationCts;
        Vector2 _shownPosition;
        bool _ready;
        string _pendingTitle;
        string _pendingBody;
        string _pendingMode;
        bool _hasPendingTip;

        void Awake()
        {
            ApplyCompactTopLayout();
            SetHiddenImmediate();
            _ready = true;

            if (_hasPendingTip)
            {
                _hasPendingTip = false;
                ShowTip(_pendingTitle, _pendingBody, _pendingMode);
            }
        }

        public void ShowTip(string title, string body, string modeLine)
        {
            // Bootstrap may start the first level during Awake before this view is ready.
            if (!_ready)
            {
                _pendingTitle = title;
                _pendingBody = body;
                _pendingMode = modeLine;
                _hasPendingTip = true;
                return;
            }

            ApplyCompactTopLayout();

            if (titleText != null)
                titleText.text = title ?? string.Empty;
            if (bodyText != null)
                bodyText.text = body ?? string.Empty;
            if (modeText != null)
                modeText.text = modeLine ?? string.Empty;

            ShowSequenceAsync().Forget();
        }

        public void HideImmediate()
        {
            _hasPendingTip = false;
            CancelAnimation();
            SetHiddenImmediate();
        }

        void ApplyCompactTopLayout()
        {
            if (panelRoot == null)
                return;

            panelRoot.anchorMin = new Vector2(0.5f, 1f);
            panelRoot.anchorMax = new Vector2(0.5f, 1f);
            panelRoot.pivot = new Vector2(0.5f, 1f);
            panelRoot.sizeDelta = panelSize;
            _shownPosition = new Vector2(0f, -Mathf.Abs(topInset));
            panelRoot.anchoredPosition = _shownPosition;

            var contentWidth = Mathf.Max(120f, panelSize.x - textPaddingX * 2f);

            if (titleText != null)
            {
                var titleRect = titleText.rectTransform;
                if (titleRect.parent != panelRoot)
                    titleRect.SetParent(panelRoot, false);
                titleRect.SetAsLastSibling();

                titleRect.anchorMin = new Vector2(0.5f, 1f);
                titleRect.anchorMax = new Vector2(0.5f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -8f);
                titleRect.sizeDelta = new Vector2(contentWidth, 22f);
                titleText.fontSize = 17;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                titleText.verticalOverflow = VerticalWrapMode.Truncate;
                titleText.resizeTextForBestFit = false;
            }

            if (bodyText != null)
            {
                var bodyRect = bodyText.rectTransform;
                if (bodyRect.parent != panelRoot)
                    bodyRect.SetParent(panelRoot, false);
                bodyRect.SetAsLastSibling();

                bodyRect.anchorMin = new Vector2(0.5f, 1f);
                bodyRect.anchorMax = new Vector2(0.5f, 1f);
                bodyRect.pivot = new Vector2(0.5f, 1f);
                bodyRect.anchoredPosition = new Vector2(0f, -36f);
                bodyRect.sizeDelta = new Vector2(contentWidth, 54f);
                bodyText.fontSize = 13;
                bodyText.alignment = TextAnchor.UpperCenter;
                bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                bodyText.verticalOverflow = VerticalWrapMode.Truncate;
                bodyText.lineSpacing = 0.95f;
                bodyText.resizeTextForBestFit = false;
            }

            if (modeText != null)
            {
                var modeRect = modeText.rectTransform;
                if (modeRect.parent != panelRoot)
                    modeRect.SetParent(panelRoot, false);
                modeRect.SetAsLastSibling();

                modeRect.anchorMin = new Vector2(0.5f, 0f);
                modeRect.anchorMax = new Vector2(0.5f, 0f);
                modeRect.pivot = new Vector2(0.5f, 0f);
                modeRect.anchoredPosition = new Vector2(0f, 10f);
                modeRect.sizeDelta = new Vector2(contentWidth, 20f);
                modeText.fontSize = 12;
                modeText.alignment = TextAnchor.MiddleCenter;
                modeText.horizontalOverflow = HorizontalWrapMode.Wrap;
                modeText.verticalOverflow = VerticalWrapMode.Truncate;
                modeText.resizeTextForBestFit = false;
            }
        }

        async UniTaskVoid ShowSequenceAsync()
        {
            CancelAnimation();
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;

            try
            {
                await AnimateInAsync(token);
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0.5f, visibleDuration)), cancellationToken: token);
                await AnimateOutAsync(token);
                SetHiddenImmediate();
            }
            catch (OperationCanceledException)
            {
            }
        }

        async UniTask AnimateInAsync(CancellationToken token)
        {
            if (panelGroup != null)
            {
                panelGroup.gameObject.SetActive(true);
                panelGroup.blocksRaycasts = false;
                panelGroup.interactable = false;
                panelGroup.alpha = 0f;
            }

            if (panelRoot != null)
                panelRoot.anchoredPosition = _shownPosition + Vector2.up * slideDistance;

            var duration = Mathf.Max(0.01f, animationDuration);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseOutCubic(t);

                if (panelGroup != null)
                    panelGroup.alpha = eased;

                if (panelRoot != null)
                    panelRoot.anchoredPosition = Vector2.Lerp(
                        _shownPosition + Vector2.up * slideDistance,
                        _shownPosition,
                        eased);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (panelGroup != null)
                panelGroup.alpha = 1f;
            if (panelRoot != null)
                panelRoot.anchoredPosition = _shownPosition;
        }

        async UniTask AnimateOutAsync(CancellationToken token)
        {
            var duration = Mathf.Max(0.01f, animationDuration * 0.85f);
            var elapsed = 0f;
            var startPos = panelRoot != null ? panelRoot.anchoredPosition : _shownPosition;
            var startAlpha = panelGroup != null ? panelGroup.alpha : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseInCubic(t);

                if (panelGroup != null)
                    panelGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);

                if (panelRoot != null)
                    panelRoot.anchoredPosition = Vector2.Lerp(
                        startPos,
                        _shownPosition + Vector2.up * slideDistance,
                        eased);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        void SetHiddenImmediate()
        {
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.blocksRaycasts = false;
                panelGroup.interactable = false;
                panelGroup.gameObject.SetActive(false);
            }

            if (panelRoot != null)
                panelRoot.anchoredPosition = _shownPosition + Vector2.up * slideDistance;
        }

        void CancelAnimation()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;
        }

        void OnDestroy() => CancelAnimation();

        static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        static float EaseInCubic(float t) => t * t * t;
    }
}
