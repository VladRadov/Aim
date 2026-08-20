using System;
using System.Threading;
using Aim.Models;
using Aim.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Views
{
    public sealed class FlyingTargetView : MonoBehaviour, IHittable
    {
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Color normalColor = new(0.2f, 0.7f, 1f, 1f);
        [SerializeField] Color hitColor = Color.white;
        [SerializeField] float hitFlashDuration = 0.1f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        FlyingTargetPool _pool;
        MaterialPropertyBlock _propertyBlock;
        CancellationTokenSource _flyCts;
        Action<FlyingTargetView> _onDespawn;
        Vector3 _baseScale;
        bool _isActive;

        public bool IsActive => _isActive && gameObject.activeInHierarchy;
        public bool CountsAsScore => IsActive;

        public void Initialize(FlyingTargetPool pool)
        {
            _pool = pool;
            _baseScale = transform.localScale;
        }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            if (_baseScale == Vector3.zero)
                _baseScale = transform.localScale;
            ApplyColor(normalColor);
        }

        public void Activate(
            Vector3 position,
            Vector3 direction,
            float speed,
            float lifetime,
            Action<FlyingTargetView> onDespawn)
        {
            Activate(position, direction, speed, lifetime, 1f, onDespawn);
        }

        public void Activate(
            Vector3 position,
            Vector3 direction,
            float speed,
            float lifetime,
            float uniformScale,
            Action<FlyingTargetView> onDespawn)
        {
            CancelFlight();
            _onDespawn = onDespawn;
            transform.position = position;
            transform.localScale = _baseScale * Mathf.Max(0.05f, uniformScale);
            _isActive = true;
            ApplyColor(normalColor);
            gameObject.SetActive(true);
            FlyAsync(direction.normalized, speed, lifetime).Forget();
        }

        public void ActivateRail(
            Vector3 start,
            Vector3 end,
            float speed,
            float lifetime,
            Action<FlyingTargetView> onDespawn)
        {
            CancelFlight();
            _onDespawn = onDespawn;
            transform.position = start;
            _isActive = true;
            ApplyColor(normalColor);
            gameObject.SetActive(true);
            RailAsync(start, end, Mathf.Max(0.1f, speed), lifetime).Forget();
        }

        public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsActive)
                return;

            FlashAndDespawnAsync().Forget();
        }

        public void Despawn()
        {
            if (!_isActive)
                return;

            _isActive = false;
            CancelFlight();
            NotifyDespawn();
            _pool?.Return(this);
        }

        public void ResetState()
        {
            CancelFlight();
            _isActive = false;
            _onDespawn = null;
            if (_baseScale != Vector3.zero)
                transform.localScale = _baseScale;
            ApplyColor(normalColor);
        }

        async UniTaskVoid FlyAsync(Vector3 direction, float speed, float lifetime)
        {
            _flyCts = new CancellationTokenSource();
            var token = _flyCts.Token;
            var elapsed = 0f;

            try
            {
                while (elapsed < lifetime && _isActive)
                {
                    transform.position += direction * (speed * Time.deltaTime);
                    elapsed += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_isActive)
                Despawn();
        }

        async UniTaskVoid RailAsync(Vector3 start, Vector3 end, float speed, float lifetime)
        {
            _flyCts = new CancellationTokenSource();
            var token = _flyCts.Token;
            var elapsed = 0f;
            var toEnd = true;
            var span = end - start;
            var distance = span.magnitude;
            if (distance < 0.01f)
            {
                Despawn();
                return;
            }

            var duration = distance / speed;

            try
            {
                while ((lifetime <= 0f || elapsed < lifetime) && _isActive)
                {
                    var from = toEnd ? start : end;
                    var to = toEnd ? end : start;
                    var segmentElapsed = 0f;

                    while (segmentElapsed < duration && _isActive)
                    {
                        segmentElapsed += Time.deltaTime;
                        elapsed += Time.deltaTime;
                        var t = Mathf.Clamp01(segmentElapsed / duration);
                        transform.position = Vector3.Lerp(from, to, t);
                        await UniTask.Yield(PlayerLoopTiming.Update, token);

                        if (lifetime > 0f && elapsed >= lifetime)
                            break;
                    }

                    toEnd = !toEnd;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_isActive)
                Despawn();
        }

        async UniTaskVoid FlashAndDespawnAsync()
        {
            _isActive = false;
            CancelFlight();
            ApplyColor(hitColor);
            await UniTask.Delay(TimeSpan.FromSeconds(hitFlashDuration));
            NotifyDespawn();
            _pool?.Return(this);
        }

        void NotifyDespawn()
        {
            var callback = _onDespawn;
            _onDespawn = null;
            callback?.Invoke(this);
        }

        void CancelFlight()
        {
            _flyCts?.Cancel();
            _flyCts?.Dispose();
            _flyCts = null;
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
