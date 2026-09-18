using System;
using Aim.Models;
using Aim.Presenters;
using Aim.Views;
using UnityEngine;
using Zenject;

namespace Aim.Services
{
    public sealed class ShopMenuService : MonoBehaviour, IEntityService, IDisposable
    {
        [Inject] ShopModel _shopModel;
        [Inject] CoinsModel _coinsModel;
        [Inject] SettingsModel _settingsModel;
        [Inject] LevelWinModel _levelWinModel;
        [Inject] MainMenuModel _mainMenuModel;
        [Inject] ShopView _shopView;
        [Inject] InputView _inputView;
        [Inject] GameplayService _gameplayService;

        ShopPresenter _shopPresenter;

        public void Initialize()
        {
            if (_shopView == null)
            {
                Debug.LogWarning("ShopMenuService: ShopView is not assigned. Run Aim/Rebuild Shop UI.");
                return;
            }

            _shopPresenter = new ShopPresenter(
                _shopModel,
                _coinsModel,
                _shopView,
                _inputView,
                _gameplayService.EquipWeaponById,
                () => _settingsModel.IsOpen.Value ||
                      _levelWinModel.IsOpen.Value ||
                      _mainMenuModel.IsOpen.Value);
            _shopPresenter.Initialize();
        }

        void OnDestroy() => Dispose();

        public void Dispose() => _shopPresenter?.Dispose();
    }
}
