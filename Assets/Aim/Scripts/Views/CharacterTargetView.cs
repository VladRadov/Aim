using System;
using System.Threading;
using Aim.Models;
using Aim.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aim.Views
{
    public sealed class CharacterTargetView : MonoBehaviour
    {
        const string DefaultRunStateName = "Drunk_Run_Forward";

        static readonly int IdleHash = Animator.StringToHash("Idle");

        [SerializeField] HitZoneView headZone;
        [SerializeField] HitZoneView bodyZone;
        [SerializeField] Transform visualRoot;
        [SerializeField] Animator animator;
        [SerializeField] string runStateName = DefaultRunStateName;
        [SerializeField] string[] idleActionStateNames;

        CharacterTargetPool _pool;
        CancellationTokenSource _moveCts;
        Action<CharacterTargetView> _onDespawn;
        Vector3 _basePosition;
        Quaternion _baseRotation;
        bool _isAlive;
        bool _isDying;

        public bool IsAlive => _isAlive && !_isDying && gameObject.activeInHierarchy;

        public void Initialize(CharacterTargetPool pool)
        {
            _pool = pool;
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (headZone != null)
            {
                headZone.Configure(HitZoneKind.Head, true);
                headZone.SetHitCallback(OnZoneHit);
            }

            if (bodyZone != null)
            {
                bodyZone.Configure(HitZoneKind.Body, false);
                bodyZone.SetHitCallback(OnZoneHit);
            }

            if (animator != null)
                animator.applyRootMotion = false;
        }

        public void Activate(
            Vector3 position,
            CharacterMoveMode moveMode,
            float runSpeed,
            float jumpHeight,
            float jumpInterval,
            float lifetime,
            float runHalfWidth,
            Action<CharacterTargetView> onDespawn)
        {
            CancelMove();
            _onDespawn = onDespawn;
            _basePosition = position;
            _baseRotation = transform.rotation;
            transform.position = position;
            _isAlive = true;
            _isDying = false;

            headZone?.ResetState();
            bodyZone?.ResetState();

            if (visualRoot != null)
                visualRoot.localPosition = Vector3.zero;

            var resolvedMode = moveMode == CharacterMoveMode.Random
                ? (UnityEngine.Random.value < 0.5f ? CharacterMoveMode.Run : CharacterMoveMode.Jump)
                : moveMode;

            if (resolvedMode == CharacterMoveMode.Run)
                PlayRunAnimation();
            else
                PlayRandomIdleActionAnimation();

            MoveAsync(resolvedMode, runSpeed, jumpHeight, jumpInterval, lifetime, runHalfWidth).Forget();
        }

        void OnZoneHit(HitZoneView zone, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!_isAlive || _isDying)
                return;

            if (zone.ZoneKind == HitZoneKind.Head)
                Despawn();
        }

        public void Despawn()
        {
            if (_isDying)
                return;

            if (!_isAlive)
                return;

            _isDying = true;
            _isAlive = false;
            CancelMove();
            FinishDespawn();
        }

        void FinishDespawn()
        {
            var callback = _onDespawn;
            _onDespawn = null;
            callback?.Invoke(this);
            _pool?.Return(this);
        }

        public void ResetState()
        {
            CancelMove();
            _isAlive = false;
            _isDying = false;
            _onDespawn = null;
            headZone?.ResetState();
            bodyZone?.ResetState();
            if (visualRoot != null)
                visualRoot.localPosition = Vector3.zero;

            transform.rotation = _baseRotation;
            PlayAnimation(IdleHash);
        }

        void PlayRunAnimation()
        {
            var stateName = string.IsNullOrEmpty(runStateName) ? DefaultRunStateName : runStateName;
            PlayAnimation(Animator.StringToHash(stateName));
        }

        void PlayRandomIdleActionAnimation()
        {
            if (idleActionStateNames == null || idleActionStateNames.Length == 0)
            {
                PlayAnimation(IdleHash);
                return;
            }

            var index = UnityEngine.Random.Range(0, idleActionStateNames.Length);
            var stateName = idleActionStateNames[index];
            if (string.IsNullOrEmpty(stateName))
            {
                PlayAnimation(IdleHash);
                return;
            }

            PlayAnimation(Animator.StringToHash(stateName));
        }

        void PlayAnimation(int stateHash)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            if (!animator.enabled)
                animator.enabled = true;

            animator.Rebind();
            animator.Update(0f);
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
        }

        async UniTaskVoid MoveAsync(
            CharacterMoveMode mode,
            float runSpeed,
            float jumpHeight,
            float jumpInterval,
            float lifetime,
            float runHalfWidth)
        {
            _moveCts = new CancellationTokenSource();
            var token = _moveCts.Token;
            var elapsed = 0f;

            var useProceduralJump = mode == CharacterMoveMode.Jump &&
                                    (animator == null || animator.runtimeAnimatorController == null);

            try
            {
                if (mode == CharacterMoveMode.Run)
                {
                    var direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                    FaceMoveDirection(direction);

                    while (elapsed < lifetime && _isAlive && !_isDying)
                    {
                        var x = transform.position.x + direction * runSpeed * Time.deltaTime;
                        if (x > _basePosition.x + runHalfWidth || x < _basePosition.x - runHalfWidth)
                        {
                            direction *= -1f;
                            FaceMoveDirection(direction);
                        }

                        transform.position = new Vector3(x, _basePosition.y, _basePosition.z);
                        elapsed += Time.deltaTime;
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                }
                else
                {
                    transform.rotation = _baseRotation;
                    var jumpTimer = 0f;
                    while (elapsed < lifetime && _isAlive && !_isDying)
                    {
                        if (useProceduralJump)
                        {
                            jumpTimer += Time.deltaTime;
                            var jumpPhase = Mathf.PingPong(jumpTimer / Mathf.Max(0.01f, jumpInterval), 1f);
                            var height = Mathf.Sin(jumpPhase * Mathf.PI) * jumpHeight;
                            if (visualRoot != null)
                                visualRoot.localPosition = new Vector3(0f, height, 0f);
                            else
                                transform.position = _basePosition + Vector3.up * height;
                        }

                        elapsed += Time.deltaTime;
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_isAlive && !_isDying)
                Despawn();
        }

        void FaceMoveDirection(float direction)
        {
            // Drunk Run Forward faces local +Z; strafe along world X.
            var yaw = direction >= 0f ? 90f : -90f;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void CancelMove()
        {
            _moveCts?.Cancel();
            _moveCts?.Dispose();
            _moveCts = null;
        }
    }
}
