using System;
using Aim.Models;
using Aim.Presenters;
using Aim.Views;
using UnityEngine;
using Zenject;

namespace Aim.Services
{
    public sealed class SettingsMenuService : MonoBehaviour, IEntityService, IDisposable
    {
        [Inject] SettingsModel _settings;
        [Inject] LevelWinModel _levelWin;
        [Inject] ShopModel _shop;
        [Inject] MainMenuModel _mainMenu;
        [Inject] SettingsView _settingsView;
        [Inject] InputView _inputView;
        [Inject] AudioSettingsService _audioService;

        SettingsPresenter _settingsPresenter;

        public void Initialize()
        {
            if (_settingsView == null)
            {
                Debug.LogWarning("SettingsMenuService: SettingsView is not assigned. Run Aim/Rebuild Settings UI.");
                return;
            }

            _settingsPresenter = new SettingsPresenter(
                _settings,
                _settingsView,
                _audioService,
                _inputView,
                () => _levelWin.IsOpen.Value || _shop.IsOpen.Value || _mainMenu.IsOpen.Value);
            _settingsPresenter.Initialize();
        }

        void OnDestroy() => Dispose();

        public void Dispose()
        {
            _settingsPresenter?.Dispose();
        }
    }
}
