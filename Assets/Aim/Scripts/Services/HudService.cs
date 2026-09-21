using System;
using Aim.Config;
using Aim.Models;
using Aim.Presenters;
using Aim.Views;
using UnityEngine;
using Zenject;

namespace Aim.Services
{
    public sealed class HudService : MonoBehaviour, IEntityService, IDisposable
    {
        [Inject] SessionModel _session;
        [Inject] CoinsModel _coins;
        [Inject] AimTrainerConfig _config;
        [Inject] SessionHudView _sessionHudView;
        [Inject] CoinsHudView _coinsHudView;
        [Inject] InputView _inputView;

        SessionHudPresenter _sessionHudPresenter;
        CoinsHudPresenter _coinsHudPresenter;
        CoinsRewardPresenter _coinsRewardPresenter;
        MobileControlsPresenter _mobileControlsPresenter;
        MobileControlsView _mobileControlsView;

        public void Initialize()
        {
            if (_sessionHudView != null)
            {
                _sessionHudPresenter = new SessionHudPresenter(_session, _sessionHudView);
                _sessionHudPresenter.Initialize();
            }

            if (_coinsHudView != null)
            {
                _coinsHudPresenter = new CoinsHudPresenter(_coins, _coinsHudView);
                _coinsHudPresenter.Initialize();
            }
            else
            {
                Debug.LogWarning("HudService: CoinsHudView is not assigned. Run Aim/Rebuild Coins HUD.");
            }

            _coinsRewardPresenter = new CoinsRewardPresenter(_coins, _session, _config);
            _coinsRewardPresenter.Initialize();

            var canvasTransform = ResolveHudCanvas();
            if (canvasTransform != null && _inputView != null)
            {
                _mobileControlsView = canvasTransform.GetComponentInChildren<MobileControlsView>(true);
                if (_mobileControlsView == null)
                    _mobileControlsView = MobileControlsView.Create(canvasTransform);

                _mobileControlsPresenter = new MobileControlsPresenter(_session, _inputView, _mobileControlsView);
                _mobileControlsPresenter.Initialize();
            }
        }

        Transform ResolveHudCanvas()
        {
            if (_sessionHudView != null)
            {
                var canvas = _sessionHudView.GetComponentInParent<Canvas>();
                if (canvas != null)
                    return canvas.transform;
            }

            if (_coinsHudView != null)
            {
                var canvas = _coinsHudView.GetComponentInParent<Canvas>();
                if (canvas != null)
                    return canvas.transform;
            }

            return null;
        }

        void OnDestroy() => Dispose();

        public void Dispose()
        {
            _sessionHudPresenter?.Dispose();
            _coinsHudPresenter?.Dispose();
            _coinsRewardPresenter?.Dispose();
            _mobileControlsPresenter?.Dispose();
        }
    }
}
