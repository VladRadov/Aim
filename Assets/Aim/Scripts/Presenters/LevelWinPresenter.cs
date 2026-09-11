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
        readonly MainMenuModel _mainMenuModel;
        readonly LevelService _levelService;
        readonly LevelRunStatsTracker _statsTracker;
        readonly AimTrainerConfig _config;
        readonly LevelWinView _view;
        readonly InputView _inputView;
        readonly CompositeDisposable _disposables = new();

        bool _winBonusGrantedForPanel;
        bool _rewardAdInProgress;
        const string DoubleCoinsRewardId = "double_coins";

        public LevelWinPresenter(
            LevelWinModel winModel,
            SessionModel sessionModel,
            CoinsModel coinsModel,
            SettingsModel settingsModel,
            MainMenuModel mainMenuModel,
            LevelService levelService,
            LevelRunStatsTracker statsTracker,
            AimTrainerConfig config,
            LevelWinView view,
            InputView inputView)
        {
            _winModel = winModel;
            _sessionModel = sessionModel;
            _coinsModel = coinsModel;
            _settingsModel = settingsModel;
            _mainMenuModel = mainMenuModel;
            _levelService = levelService;
            _statsTracker = statsTracker;
            _config = config;
            _view = view;
            _inputView = inputView;
        }

        public void Initialize()
        {
            _sessionModel.State
                .Where(state => state == LevelState.Won || state == LevelState.Lost)
                .Subscribe(state => OpenResultPanel(state == LevelState.Won))
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

            _winModel.Summary
                .Subscribe(_view.BindSummary)
                .AddTo(_disposables);

            _winModel.HasNextLevel
                .Subscribe(_view.SetNextLevelAvailable)
                .AddTo(_disposables);

            _winModel.ShowReward
                .Subscribe(_view.SetRewardVisible)
                .AddTo(_disposables);

            _winModel.RewardClaimed
                .Subscribe(claimed => _view.SetRewardAvailable(!claimed && !_rewardAdInProgress))
                .AddTo(_disposables);

            _view.NextLevelRequested
                .Subscribe(_ => OnNextLevel())
                .AddTo(_disposables);

            _view.RetryRequested
                .Subscribe(_ => OnRetry())
                .AddTo(_disposables);

            _view.RewardAdRequested
                .Subscribe(_ => OnRewardAd())
                .AddTo(_disposables);

            _view.MenuRequested
                .Subscribe(_ => OnMenu())
                .AddTo(_disposables);

            _settingsModel.IsOpen
                .Subscribe(_ => RefreshUiCapture())
                .AddTo(_disposables);

            _mainMenuModel.IsOpen
                .Subscribe(_ => RefreshUiCapture())
                .AddTo(_disposables);
        }

        void OpenResultPanel(bool isWin)
        {
            _rewardAdInProgress = false;
            var winBonus = isWin ? Mathf.Max(0, _config.CoinsWinBonus) : 0;
            _winBonusGrantedForPanel = false;

            if (!_winBonusGrantedForPanel && winBonus > 0)
            {
                _coinsModel.Add(winBonus);
                _winBonusGrantedForPanel = true;
            }

            var summary = _statsTracker != null
                ? _statsTracker.BuildSummary(isWin)
                : new LevelRunSummary(
                    "Уровень",
                    isWin,
                    true,
                    0, 0, 0, 0, 0,
                    0f, 0f, winBonus, isWin ? 1 : 0, false, string.Empty);

            _winModel.Open(
                summary,
                hasNextLevel: isWin && _levelService.IsCampaign && _levelService.HasNext,
                showReward: isWin);
        }

        void OnNextLevel()
        {
            if (!_winModel.IsOpen.Value || _rewardAdInProgress)
                return;

            if (!_winModel.Summary.Value.IsWin || !_winModel.HasNextLevel.Value)
                return;

            if (!_levelService.HasNext)
                return;

            ShowInterstitialThen(() =>
            {
                _winModel.Close();
                _levelService.TryStartNext();
            });
        }

        void OnRetry()
        {
            if (!_winModel.IsOpen.Value || _rewardAdInProgress)
                return;

            ShowInterstitialThen(() =>
            {
                _winModel.Close();
                _levelService.TryRestartCurrent();
            });
        }

        void ShowInterstitialThen(Action continueAction)
        {
            if (continueAction == null)
                return;

            _rewardAdInProgress = true;
            InterstitialAdService.Current.Show(() =>
            {
                _rewardAdInProgress = false;
                continueAction.Invoke();
            });
        }

        void OnMenu()
        {
            if (!_winModel.IsOpen.Value || _rewardAdInProgress)
                return;

            _levelService.ReturnToMenu();
        }

        void OnRewardAd()
        {
            if (!_winModel.IsOpen.Value || !_winModel.ShowReward.Value || _winModel.RewardClaimed.Value)
                return;

            if (_rewardAdInProgress)
                return;

            var coinsToDouble = _winModel.Summary.Value.CoinsEarned;
            if (coinsToDouble <= 0)
                return;

            _rewardAdInProgress = true;
            _view.SetRewardAvailable(false);

            RewardedAdService.Current.Show(
                DoubleCoinsRewardId,
                onRewarded: () =>
                {
                    _rewardAdInProgress = false;
                    if (!_winModel.IsOpen.Value || _winModel.RewardClaimed.Value)
                        return;

                    _coinsModel.Add(coinsToDouble);
                    _winModel.MarkRewardClaimed();
                },
                onClosedWithoutReward: () =>
                {
                    _rewardAdInProgress = false;
                    if (_winModel.IsOpen.Value && !_winModel.RewardClaimed.Value)
                        _view.SetRewardAvailable(true);
                });
        }

        void RefreshUiCapture()
        {
            _inputView.SetUiCapture(
                _winModel.IsOpen.Value ||
                _settingsModel.IsOpen.Value ||
                _mainMenuModel.IsOpen.Value);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
