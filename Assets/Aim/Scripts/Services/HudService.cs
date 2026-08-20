using System;
using Aim.Config;
using Aim.Models;
using Aim.Presenters;
using Aim.Views;
using UnityEngine;

namespace Aim.Services
{
    public sealed class HudService : IDisposable
    {
        readonly SessionHudPresenter _sessionHudPresenter;
        readonly CoinsHudPresenter _coinsHudPresenter;
        readonly CoinsRewardPresenter _coinsRewardPresenter;

        public HudService(
            SessionModel session,
            CoinsModel coins,
            AimTrainerConfig config,
            SessionHudView sessionHudView,
            CoinsHudView coinsHudView)
        {
            if (sessionHudView != null)
            {
                _sessionHudPresenter = new SessionHudPresenter(session, sessionHudView);
                _sessionHudPresenter.Initialize();
            }

            if (coinsHudView != null)
            {
                _coinsHudPresenter = new CoinsHudPresenter(coins, coinsHudView);
                _coinsHudPresenter.Initialize();
            }
            else
            {
                Debug.LogWarning("HudService: CoinsHudView is not assigned. Run Aim/Rebuild Coins HUD.");
            }

            _coinsRewardPresenter = new CoinsRewardPresenter(coins, session, config);
            _coinsRewardPresenter.Initialize();
        }

        public void Dispose()
        {
            _sessionHudPresenter?.Dispose();
            _coinsHudPresenter?.Dispose();
            _coinsRewardPresenter?.Dispose();
        }
    }
}
