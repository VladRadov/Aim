using System;
using System.Threading;
using Aim.Models;
using Aim.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Views
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class BouncingTargetView : MonoBehaviour, IHittable
    {
        const float FloorNormalThreshold = 0.5f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] Renderer targetRenderer;
        [SerializeField] Rigidbody body;
        [SerializeField] Color normalColor = new(0.35f, 0.9f, 1f, 1f);
        [SerializeField] Color hitColor = Color.white;
        [SerializeField] float mass = 0.7f;
        [SerializeField] float hitDespawnDelay = 0f;

        BouncingTargetPool _pool;
        MaterialPropertyBlock _propertyBlock;
        CancellationTokenSource _lifeCts;
        Action<BouncingTargetView> _onDespawn;
        PhysicsMaterial _bounceMaterial;
        SphereCollider _collider;
        float _bounceSpeed;
        bool _isActive;

        public bool IsActive => _isActive && gameObject.activeInHierarchy;
        public bool CountsAsScore => IsActive;
        public Collider PhysicsCollider => _collider != null ? _collider : (_collider = GetComponent<SphereCollider>());

        public void Initialize(BouncingTargetPool pool)
        {
            _pool = pool;
            EnsurePhysics();
        }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            EnsurePhysics();
            ApplyColor(normalColor);
        }

        public void Activate(
            Vector3 position,
            Vector3 velocity,
            float lifetime,
            float bounciness,
            Action<BouncingTargetView> onDespawn)
        {
            CancelLifetime();
            EnsurePhysics();

            _onDespawn = onDespawn;
            _isActive = true;
            _bounceSpeed = Mathf.Max(0.5f, Mathf.Abs(velocity.y));
            transform.position = position;
            transform.rotation = Quaternion.identity;
            ApplyBounceMaterial(bounciness);
            ApplyColor(normalColor);

            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;
            body.position = position;
            body.rotation = Quaternion.identity;
            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezePositionZ;
            body.WakeUp();

            gameObject.SetActive(true);
            AudioSettingsService.Current?.PlaySpawn();

            if (lifetime > 0f)
                LifetimeAsync(lifetime).Forget();
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
            CancelLifetime();
            NotifyDespawn();
            _pool?.Return(this);
        }

        public void ResetState()
        {
            CancelLifetime();
            _isActive = false;
            _onDespawn = null;
            _bounceSpeed = 0f;
            ApplyColor(normalColor);

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            SustainFloorBounce(collision);
        }

        void OnCollisionStay(Collision collision)
        {
            SustainFloorBounce(collision);
        }

        void SustainFloorBounce(Collision collision)
        {
            if (!_isActive || body == null || _bounceSpeed <= 0f)
                return;

            if (!HasFloorContact(collision))
                return;

            var velocity = body.linearVelocity;
            if (velocity.y >= _bounceSpeed * 0.85f)
                return;

            velocity.y = _bounceSpeed;
            body.linearVelocity = velocity;
        }

        static bool HasFloorContact(Collision collision)
        {
            var count = collision.contactCount;
            for (var i = 0; i < count; i++)
            {
                if (collision.GetContact(i).normal.y > FloorNormalThreshold)
                    return true;
            }

            return false;
        }

        async UniTaskVoid LifetimeAsync(float seconds)
        {
            _lifeCts = new CancellationTokenSource();
            var token = _lifeCts.Token;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
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
            CancelLifetime();
            ApplyColor(hitColor);
            if (hitDespawnDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(hitDespawnDelay));
            NotifyDespawn();
            _pool?.Return(this);
        }

        void NotifyDespawn()
        {
            var callback = _onDespawn;
            _onDespawn = null;
            callback?.Invoke(this);
        }

        void CancelLifetime()
        {
            _lifeCts?.Cancel();
            _lifeCts?.Dispose();
            _lifeCts = null;
        }

        void EnsurePhysics()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();

            if (_collider == null)
                _collider = GetComponent<SphereCollider>();

            if (_collider != null && _bounceMaterial == null)
            {
                _bounceMaterial = new PhysicsMaterial("BouncingBallMaterial")
                {
                    bounciness = 1f,
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounceCombine = PhysicsMaterialCombine.Maximum,
                    frictionCombine = PhysicsMaterialCombine.Minimum
                };
            }

            if (_collider != null)
                _collider.sharedMaterial = _bounceMaterial;

            body.mass = mass;
            body.linearDamping = 0f;
            body.angularDamping = 0.05f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionZ;
            body.sleepThreshold = 0f;
        }

        void ApplyBounceMaterial(float bounciness)
        {
            if (_bounceMaterial == null)
                return;

            _bounceMaterial.bounciness = Mathf.Clamp01(Mathf.Max(bounciness, 0.95f));
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
