using System;
using Aim.Config;
using Aim.Models;
using Aim.Services;
using Aim.Views;
using UniRx;
using UnityEngine;

namespace Aim.Presenters
{
    public sealed class LevelWinPresenter : IDisposable
    {
        readonly LevelWinModel _winModel;
        readonly SessionModel _sessionModel;
        readonly CoinsModel _coinsModel;
        readonly SettingsModel _settingsModel;
        readonly LevelService _levelService;
        readonly AimTrainerConfig _config;
        readonly LevelWinView _view;
        readonly InputView _inputView;
        readonly CompositeDisposable _disposables = new();

        bool _winBonusGrantedForPanel;

        public LevelWinPresenter(
            LevelWinModel winModel,
            SessionModel sessionModel,
            CoinsModel coinsModel,
            SettingsModel settingsModel,
            LevelService levelService,
            AimTrainerConfig config,
            LevelWinView view,
            InputView inputView)
        {
            _winModel = winModel;
            _sessionModel = sessionModel;
            _coinsModel = coinsModel;
            _settingsModel = settingsModel;
            _levelService = levelService;
            _config = config;
            _view = view;
            _inputView = inputView;
        }

        public void Initialize()
        {
            _sessionModel.State
                .Where(state => state == LevelState.Won)
                .Subscribe(_ => OpenWinPanel())
                .AddTo(_disposables);

            _winModel.IsOpen
                .Skip(1)
                .Subscribe(isOpen =>
                {
                    RefreshUiCapture();
                    if (isOpen)
                        _view.ShowAnimated();
                    else
                        _view.HideAnimated();
                })
                .AddTo(_disposables);

            _winModel.HasNextLevel
                .Subscribe(_view.SetNextLevelAvailable)
                .AddTo(_disposables);

            _winModel.RewardClaimed
                .Subscribe(claimed => _view.SetRewardAvailable(!claimed))
                .AddTo(_disposables);

            _winModel.AwardedWinBonus
                .Subscribe(_view.SetWinBonus)
                .AddTo(_disposables);

            _view.NextLevelRequested
                .Subscribe(_ => OnNextLevel())
                .AddTo(_disposables);

            _view.RewardAdRequested
                .Subscribe(_ => OnRewardAd())
                .AddTo(_disposables);

            _settingsModel.IsOpen
                .Subscribe(_ => RefreshUiCapture())
                .AddTo(_disposables);
        }

        void OpenWinPanel()
        {
            var hasNext = _levelService.HasNext;
            var winBonus = Mathf.Max(0, _config.CoinsWinBonus);
            _winBonusGrantedForPanel = false;
            _winModel.Open(winBonus, hasNext);

            if (!_winBonusGrantedForPanel && winBonus > 0)
            {
                _coinsModel.Add(winBonus);
                _winBonusGrantedForPanel = true;
            }
        }

        void OnNextLevel()
        {
            if (!_winModel.IsOpen.Value)
                return;

            if (!_levelService.HasNext)
                return;

            _winModel.Close();
            _levelService.TryStartNext();
        }

        void OnRewardAd()
        {
            if (!_winModel.IsOpen.Value || _winModel.RewardClaimed.Value)
                return;

            Debug.Log("LevelWin: rewarded ad stub — double coins will be wired later.");
        }

        void RefreshUiCapture()
        {
            _inputView.SetUiCapture(_winModel.IsOpen.Value || _settingsModel.IsOpen.Value);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
