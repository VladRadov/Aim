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

        SessionHudPresenter _sessionHudPresenter;
        CoinsHudPresenter _coinsHudPresenter;
        CoinsRewardPresenter _coinsRewardPresenter;

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
        }

        void OnDestroy() => Dispose();

        public void Dispose()
        {
            _sessionHudPresenter?.Dispose();
            _coinsHudPresenter?.Dispose();
            _coinsRewardPresenter?.Dispose();
        }
    }
}
