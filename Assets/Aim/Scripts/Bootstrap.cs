using Aim.Models;
using Aim.Services;
using Aim.Views;
using UnityEngine;
using Zenject;

namespace Aim
{
    /// <summary>
    /// Composition root. Zenject injects services; this class initializes them in order.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour, IInitializable
    {
        [Inject] GameplayService _gameplayService;
        [Inject] HudService _hudService;
        [Inject] SettingsMenuService _settingsMenuService;
        [Inject] ShopMenuService _shopMenuService;
        [Inject] LevelService _levelService;
        [Inject] MainMenuService _mainMenuService;
        [Inject] MainMenuModel _mainMenuModel;
        [Inject] SessionModel _sessionModel;
        [Inject] SettingsModel _settingsModel;
        [Inject] CoinsModel _coinsModel;
        [Inject] LevelWinModel _levelWinModel;
        [Inject] ShopModel _shopModel;
        [Inject] AimModel _aimModel;
        [Inject] ShootModel _shootModel;
        [Inject] RecoilModel _recoilModel;
        [Inject] InputView _inputView;

        public void Initialize()
        {
            _gameplayService.Initialize();
            _hudService.Initialize();
            _settingsMenuService.Initialize();
            _shopMenuService.Initialize();
            _levelService.Initialize();
            _mainMenuService.Initialize();

            _mainMenuModel.ShowRoot();
            if (_inputView != null)
                _inputView.SetUiCapture(true);
        }

        void OnDestroy()
        {
            _aimModel?.Dispose();
            _shootModel?.Dispose();
            _recoilModel?.Dispose();
            _shopModel?.Dispose();
            _mainMenuModel?.Dispose();
            _levelWinModel?.Dispose();
            _coinsModel?.Dispose();
            _settingsModel?.Dispose();
            _sessionModel?.Dispose();
        }
    }
}
