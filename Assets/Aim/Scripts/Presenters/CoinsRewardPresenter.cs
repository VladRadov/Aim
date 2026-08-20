using System;
using Aim.Config;
using Aim.Models;
using UniRx;
using UnityEngine;

namespace Aim.Presenters
{
    public sealed class CoinsRewardPresenter : IDisposable
    {
        readonly CoinsModel _coinsModel;
        readonly SessionModel _sessionModel;
        readonly AimTrainerConfig _config;
        readonly CompositeDisposable _disposables = new();

        int _lastAwardedHits;

        public CoinsRewardPresenter(
            CoinsModel coinsModel,
            SessionModel sessionModel,
            AimTrainerConfig config)
        {
            _coinsModel = coinsModel;
            _sessionModel = sessionModel;
            _config = config;
        }

        public void Initialize()
        {
            _sessionModel.State
                .Subscribe(OnStateChanged)
                .AddTo(_disposables);

            _sessionModel.Hits
                .Subscribe(OnHitsChanged)
                .AddTo(_disposables);
        }

        void OnStateChanged(LevelState state)
        {
            if (state == LevelState.Playing)
                _lastAwardedHits = 0;
        }

        void OnHitsChanged(int hits)
        {
            if (!_sessionModel.IsPlaying || hits <= _lastAwardedHits)
                return;

            var gained = hits - _lastAwardedHits;
            _lastAwardedHits = hits;

            var perHit = Mathf.Max(0, _config.CoinsPerHit);
            if (perHit > 0 && gained > 0)
                _coinsModel.Add(perHit * gained);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
