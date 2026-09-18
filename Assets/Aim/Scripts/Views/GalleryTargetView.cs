using System;
using Aim.Models;
using Aim.Services;
using UnityEngine;

namespace Aim.Views
{
    [RequireComponent(typeof(Collider))]
    public sealed class GalleryTargetView : MonoBehaviour, IHittable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] Renderer[] renderers;
        [SerializeField] Rigidbody body;
        [SerializeField] Color normalColor = new(0.75f, 0.75f, 0.78f, 1f);
        [SerializeField] Color highlightColor = new(1f, 0.82f, 0.12f, 1f);
        [SerializeField] float hitImpactForce = 1.8f;
        [SerializeField] float mass = 1.2f;
        [SerializeField] float drag = 0.08f;
        [SerializeField] float angularDrag = 1.8f;

        MaterialPropertyBlock _propertyBlock;
        Action<GalleryTargetView> _onCorrectHit;
        Action<GalleryTargetView> _onKnockedDown;
        Collider _collider;
        Vector3 _spawnPosition;
        Quaternion _spawnRotation;
        bool _isHighlighted;
        bool _isActive = true;
        bool _isFallen;

        public bool IsActive => _isActive && !_isFallen && gameObject.activeInHierarchy;
        public bool CountsAsScore => IsActive && _isHighlighted;
        public bool IsHighlighted => _isHighlighted;
        public bool IsStanding => _isActive && !_isFallen && gameObject.activeInHierarchy;

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _collider = GetComponent<Collider>();
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
            EnsureRigidbody();
            ApplyVisual();
        }

        public void Activate(
            Vector3 position,
            Quaternion rotation,
            Color normal,
            Color highlight,
            Action<GalleryTargetView> onCorrectHit,
            Action<GalleryTargetView> onKnockedDown = null)
        {
            _spawnPosition = position;
            _spawnRotation = rotation;
            normalColor = normal;
            highlightColor = highlight;
            _onCorrectHit = onCorrectHit;
            _onKnockedDown = onKnockedDown;
            _isActive = true;
            _isFallen = false;
            _isHighlighted = false;
            gameObject.SetActive(true);
            AudioSettingsService.Current?.PlaySpawn();
            ResetPoseAndPhysics();
            ApplyVisual();
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_isFallen)
                highlighted = false;
            _isHighlighted = highlighted;
            ApplyVisual();
        }

        public void Deactivate()
        {
            _isActive = false;
            _isHighlighted = false;
            _isFallen = false;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.useGravity = false;
            }

            gameObject.SetActive(false);
        }

        public void ResetStanding()
        {
            _isActive = true;
            _isFallen = false;
            _isHighlighted = false;
            gameObject.SetActive(true);
            ResetPoseAndPhysics();
            ApplyVisual();
        }

        public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!_isActive || _isFallen || !gameObject.activeInHierarchy)
                return;

            var scored = _isHighlighted;
            if (scored)
                _onCorrectHit?.Invoke(this);

            KnockDown(hitPoint, hitNormal);
        }

        void KnockDown(Vector3 hitPoint, Vector3 hitNormal)
        {
            _isFallen = true;
            _isHighlighted = false;
            ApplyVisual();
            EnsureRigidbody();

            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                var pushDir = -hitNormal;
                if (pushDir.sqrMagnitude < 0.001f)
                    pushDir = transform.forward;

                // Let off-center force create the rotation naturally instead of adding synthetic torque.
                pushDir.y = Mathf.Clamp(pushDir.y, -0.05f, 0.2f);
                pushDir.Normalize();
                body.AddForceAtPosition(pushDir * hitImpactForce, hitPoint, ForceMode.Impulse);
            }

            _onKnockedDown?.Invoke(this);
        }

        void ResetPoseAndPhysics()
        {
            EnsureRigidbody();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.useGravity = false;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            if (_collider != null)
                _collider.enabled = true;

            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        }

        void EnsureRigidbody()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();

            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();

            body.mass = mass;
            body.linearDamping = drag;
            body.angularDamping = angularDrag;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.None;
        }

        void ApplyVisual()
        {
            if (renderers == null || renderers.Length == 0)
                return;

            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();

            var color = _isHighlighted ? highlightColor : normalColor;
            var emission = _isHighlighted ? highlightColor * 1.8f : Color.black;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                _propertyBlock.SetColor(ColorId, color);
                _propertyBlock.SetColor(EmissionColorId, emission);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
