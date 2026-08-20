using System;
using System.Threading;
using Aim.Models;
using Aim.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Views
{
    public sealed class PeekTargetView : MonoBehaviour, IHittable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] Renderer targetRenderer;
        [SerializeField] Collider targetCollider;
        [SerializeField] Color hiddenColor = new(0.35f, 0.35f, 0.4f, 1f);
        [SerializeField] Color peekColor = new(1f, 0.55f, 0.15f, 1f);
        [SerializeField] Color hitColor = Color.white;
        [SerializeField] float hitFlashDuration = 0.1f;

        PeekTargetPool _pool;
        MaterialPropertyBlock _propertyBlock;
        CancellationTokenSource _loopCts;
        Action<PeekTargetView> _onDespawn;
        Vector3 _hidePosition;
        Vector3 _peekPosition;
        Vector3 _coverCenter;
        Vector3 _coverHalfExtents;
        float _targetHeight;
        float _hideDepthBehind;
        float _peekOffset;
        float _peekDurationMin;
        float _peekDurationMax;
        float _hideDurationMin;
        float _hideDurationMax;
        float _moveDuration;
        bool _isActive;
        bool _isPeeking;
        bool _useCoverAnchor;

        public bool IsActive => _isActive && gameObject.activeInHierarchy;
        public bool CountsAsScore => IsActive && _isPeeking;

        public void Initialize(PeekTargetPool pool)
        {
            _pool = pool;
            if (targetCollider == null)
                targetCollider = GetComponent<Collider>();
        }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            if (targetCollider == null)
                targetCollider = GetComponent<Collider>();
            ApplyColor(hiddenColor);
            SetColliderEnabled(false);
        }

        public void Activate(
            Vector3 hidePosition,
            Vector3 peekPosition,
            float peekDurationMin,
            float peekDurationMax,
            float hideDurationMin,
            float hideDurationMax,
            float moveDuration,
            float initialHideDelay,
            Action<PeekTargetView> onDespawn)
        {
            _useCoverAnchor = false;
            BeginActivate(
                hidePosition,
                peekPosition,
                peekDurationMin,
                peekDurationMax,
                hideDurationMin,
                hideDurationMax,
                moveDuration,
                initialHideDelay,
                onDespawn);
        }

        public void ActivateFromCover(
            Vector3 coverCenter,
            Vector3 coverHalfExtents,
            float targetHeight,
            float hideDepthBehind,
            float peekOffset,
            float peekDurationMin,
            float peekDurationMax,
            float hideDurationMin,
            float hideDurationMax,
            float moveDuration,
            float initialHideDelay,
            Action<PeekTargetView> onDespawn)
        {
            _useCoverAnchor = true;
            _coverCenter = coverCenter;
            _coverHalfExtents = coverHalfExtents;
            _targetHeight = targetHeight;
            _hideDepthBehind = Mathf.Max(0.1f, hideDepthBehind);
            _peekOffset = Mathf.Max(0.3f, peekOffset);
            RollPeekPositions();

            BeginActivate(
                _hidePosition,
                _peekPosition,
                peekDurationMin,
                peekDurationMax,
                hideDurationMin,
                hideDurationMax,
                moveDuration,
                initialHideDelay,
                onDespawn);
        }

        void BeginActivate(
            Vector3 hidePosition,
            Vector3 peekPosition,
            float peekDurationMin,
            float peekDurationMax,
            float hideDurationMin,
            float hideDurationMax,
            float moveDuration,
            float initialHideDelay,
            Action<PeekTargetView> onDespawn)
        {
            CancelLoop();
            _onDespawn = onDespawn;
            _hidePosition = hidePosition;
            _peekPosition = peekPosition;
            _peekDurationMin = Mathf.Max(0.1f, peekDurationMin);
            _peekDurationMax = Mathf.Max(_peekDurationMin, peekDurationMax);
            _hideDurationMin = Mathf.Max(0.1f, hideDurationMin);
            _hideDurationMax = Mathf.Max(_hideDurationMin, hideDurationMax);
            _moveDuration = Mathf.Max(0.05f, moveDuration);

            _isActive = true;
            _isPeeking = false;
            transform.position = hidePosition;
            ApplyColor(hiddenColor);
            SetColliderEnabled(false);
            gameObject.SetActive(true);

            PeekLoopAsync(Mathf.Max(0f, initialHideDelay)).Forget();
        }

        void RollPeekPositions()
        {
            _hidePosition = new Vector3(
                _coverCenter.x,
                _targetHeight,
                _coverCenter.z + _coverHalfExtents.z + _hideDepthBehind);

            var direction = UnityEngine.Random.Range(0, 3);
            _peekPosition = direction switch
            {
                0 => new Vector3(
                    _coverCenter.x - _peekOffset,
                    _targetHeight,
                    _coverCenter.z),
                1 => new Vector3(
                    _coverCenter.x + _peekOffset,
                    _targetHeight,
                    _coverCenter.z),
                _ => new Vector3(
                    _coverCenter.x,
                    _targetHeight,
                    _coverCenter.z - _coverHalfExtents.z - _peekOffset * 0.45f)
            };
        }

        public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!CountsAsScore)
                return;

            FlashAndRestartAsync().Forget();
        }

        public void Despawn()
        {
            RetireToPool(notifyRunner: true);
        }

        public void ResetState()
        {
            CancelLoop();
            _isActive = false;
            _isPeeking = false;
            _useCoverAnchor = false;
            _onDespawn = null;
            ApplyColor(hiddenColor);
            SetColliderEnabled(false);
            gameObject.SetActive(false);
        }

        void RetireToPool(bool notifyRunner)
        {
            if (!_isActive && !gameObject.activeSelf)
                return;

            CancelLoop();
            _isActive = false;
            _isPeeking = false;
            SetColliderEnabled(false);
            gameObject.SetActive(false);

            if (notifyRunner)
                NotifyDespawn();

            _pool?.Return(this);
        }

        async UniTaskVoid PeekLoopAsync(float initialDelay)
        {
            _loopCts = new CancellationTokenSource();
            var token = _loopCts.Token;

            try
            {
                if (initialDelay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(initialDelay), cancellationToken: token);

                while (_isActive && !token.IsCancellationRequested)
                {
                    var hideTime = UnityEngine.Random.Range(_hideDurationMin, _hideDurationMax);
                    await UniTask.Delay(TimeSpan.FromSeconds(hideTime), cancellationToken: token);
                    if (!_isActive)
                        return;

                    if (_useCoverAnchor)
                    {
                        RollPeekPositions();
                        transform.position = _hidePosition;
                    }

                    await MoveToAsync(_peekPosition, token);
                    if (!_isActive)
                        return;

                    _isPeeking = true;
                    ApplyColor(peekColor);
                    SetColliderEnabled(true);

                    var peekTime = UnityEngine.Random.Range(_peekDurationMin, _peekDurationMax);
                    await UniTask.Delay(TimeSpan.FromSeconds(peekTime), cancellationToken: token);
                    if (!_isActive)
                        return;

                    _isPeeking = false;
                    SetColliderEnabled(false);
                    ApplyColor(hiddenColor);
                    await MoveToAsync(_hidePosition, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        async UniTaskVoid FlashAndRestartAsync()
        {
            _isPeeking = false;
            SetColliderEnabled(false);
            CancelLoop();
            _loopCts = new CancellationTokenSource();
            var token = _loopCts.Token;

            ApplyColor(hitColor);
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(hitFlashDuration), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!_isActive)
                return;

            if (_useCoverAnchor)
                RollPeekPositions();

            transform.position = _hidePosition;
            ApplyColor(hiddenColor);
            PeekLoopAsync(UnityEngine.Random.Range(_hideDurationMin, _hideDurationMax)).Forget();
        }

        async UniTask MoveToAsync(Vector3 target, CancellationToken token)
        {
            var start = transform.position;
            var elapsed = 0f;
            while (elapsed < _moveDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _moveDuration);
                transform.position = Vector3.Lerp(start, target, t);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            transform.position = target;
        }

        void NotifyDespawn()
        {
            var callback = _onDespawn;
            _onDespawn = null;
            callback?.Invoke(this);
        }

        void CancelLoop()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;
        }

        void SetColliderEnabled(bool enabled)
        {
            if (targetCollider != null)
                targetCollider.enabled = enabled;
        }

        void ApplyColor(Color color)
        {
            if (targetRenderer == null)
                return;

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
