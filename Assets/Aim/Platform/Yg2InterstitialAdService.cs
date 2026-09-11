using System;
using System.Collections;
using Aim.Services;
using UnityEngine;
using YG;

namespace Aim.Platform
{
    public sealed class Yg2InterstitialAdService : IInterstitialAdService
    {
        public void Show(Action onClosed)
        {
#if InterstitialAdv_yg
            var runner = new GameObject("Yg2InterstitialAdWaiter").AddComponent<InterstitialAdWaiter>();
            UnityEngine.Object.DontDestroyOnLoad(runner.gameObject);
            runner.Run(onClosed);
#else
            Debug.LogWarning("Yg2InterstitialAdService: InterstitialAdv module define is missing (InterstitialAdv_yg).");
            onClosed?.Invoke();
#endif
        }

#if InterstitialAdv_yg
        sealed class InterstitialAdWaiter : MonoBehaviour
        {
            Action _onClosed;
            bool _opened;
            bool _finished;

            public void Run(Action onClosed)
            {
                _onClosed = onClosed;
                YG2.onOpenInterAdv += OnOpen;
                YG2.onCloseInterAdv += OnClose;
                YG2.onErrorInterAdv += OnError;
                YG2.InterstitialAdvShow();
                StartCoroutine(SkipIfNotOpened());
            }

            IEnumerator SkipIfNotOpened()
            {
                var loadDelay = 0.15f;
#if UNITY_EDITOR
                loadDelay = Mathf.Max(0.15f, YG2.infoYG.Simulation.loadAdv + 0.15f);
#endif
                yield return new WaitForSecondsRealtime(loadDelay);
                if (!_opened && !YG2.nowInterAdv)
                    Finish();
            }

            void OnOpen() => _opened = true;

            void OnClose() => Finish();

            void OnError() => Finish();

            void Finish()
            {
                if (_finished)
                    return;

                _finished = true;
                YG2.onOpenInterAdv -= OnOpen;
                YG2.onCloseInterAdv -= OnClose;
                YG2.onErrorInterAdv -= OnError;
                var callback = _onClosed;
                _onClosed = null;
                callback?.Invoke();
                Destroy(gameObject);
            }
        }
#endif
    }

    public static class Yg2InterstitialAdBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            InterstitialAdService.Current = new Yg2InterstitialAdService();
        }
    }
}
