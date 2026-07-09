using System;
using System.Threading;
using Aim.Models;
using Aim.Services;
using Cysharp.Threading.Tasks;
using UniRx.Toolkit;
using UnityEngine;

namespace Aim.Views
{
    public sealed class ProjectileView : MonoBehaviour
    {
        ProjectilePool _pool;
        CancellationTokenSource _flyCts;

        public void Initialize(ProjectilePool pool)
        {
            _pool = pool;
        }

        public void Launch(ProjectileLaunchParams launchParams, Action<ShootResult> onComplete)
        {
            CancelFlight();

            transform.position = launchParams.Origin;
            transform.rotation = Quaternion.LookRotation(launchParams.Direction);
            FlyAsync(launchParams, onComplete).Forget();
        }

        async UniTaskVoid FlyAsync(ProjectileLaunchParams launchParams, Action<ShootResult> onComplete)
        {
            _flyCts = new CancellationTokenSource();
            var token = _flyCts.Token;

            var direction = launchParams.Direction.normalized;
            var traveled = 0f;
            var position = launchParams.Origin;

            try
            {
                while (traveled < launchParams.MaxDistance)
                {
                    var step = launchParams.Speed * Time.deltaTime;
                    if (Physics.Raycast(position, direction, out var hit, step, launchParams.HitMask, QueryTriggerInteraction.Ignore))
                    {
                        transform.position = hit.point;
                        onComplete?.Invoke(HitResolver.Resolve(hit));
                        ReturnToPool();
                        return;
                    }

                    position += direction * step;
                    transform.position = position;
                    traveled += step;

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                onComplete?.Invoke(ShootResult.Miss);
                ReturnToPool();
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void ResetState()
        {
            CancelFlight();
        }

        void CancelFlight()
        {
            _flyCts?.Cancel();
            _flyCts?.Dispose();
            _flyCts = null;
        }

        void ReturnToPool()
        {
            CancelFlight();
            _pool?.Return(this);
        }
    }
}
