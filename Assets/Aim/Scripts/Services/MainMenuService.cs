using System;
using Aim.Models;
using Aim.Presenters;
using Aim.Views;
using UnityEngine;
using Zenject;

namespace Aim.Services
{
    public sealed class MainMenuService : MonoBehaviour, IEntityService, IDisposable
    {
        [Inject] MainMenuModel _model;
        [Inject] LevelService _levelService;
        [Inject] GameModeCatalog _catalog;
        [Inject] SettingsModel _settingsModel;
        [Inject] LevelWinModel _winModel;
        [Inject] MainMenuView _view;
        [Inject] InputView _inputView;

        MainMenuPresenter _presenter;

        public void Initialize()
        {
            if (_view == null)
            {
                Debug.LogError("MainMenuService: MainMenuView is missing. Run Aim → Rebuild Main Menu UI.");
                return;
            }

            _presenter = new MainMenuPresenter(
                _model,
                _levelService,
                _catalog,
                _settingsModel,
                _winModel,
                _view,
                _inputView);
            _presenter.Initialize();
        }

        void OnDestroy() => Dispose();

        public void Dispose() => _presenter?.Dispose();
    }
}
