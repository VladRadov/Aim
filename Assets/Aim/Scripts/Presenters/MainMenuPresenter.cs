using System;
using System.Collections.Generic;
using Aim.Models;
using Aim.Services;
using Aim.Views;
using UniRx;
using UnityEngine;

namespace Aim.Presenters
{
    public sealed class MainMenuPresenter : IDisposable
    {
        readonly MainMenuModel _model;
        readonly LevelService _levelService;
        readonly GameModeCatalog _catalog;
        readonly SettingsModel _settingsModel;
        readonly LevelWinModel _winModel;
        readonly MainMenuView _view;
        readonly InputView _inputView;
        readonly CompositeDisposable _disposables = new();

        public MainMenuPresenter(
            MainMenuModel model,
            LevelService levelService,
            GameModeCatalog catalog,
            SettingsModel settingsModel,
            LevelWinModel winModel,
            MainMenuView view,
            InputView inputView)
        {
            _model = model;
            _levelService = levelService;
            _catalog = catalog;
            _settingsModel = settingsModel;
            _winModel = winModel;
            _view = view;
            _inputView = inputView;
        }

        public void Initialize()
        {
            BuildModeCards();

            _model.IsOpen
                .Subscribe(isOpen =>
                {
                    _view.SetOpen(isOpen);
                    RefreshUiCapture();
                })
                .AddTo(_disposables);

            _model.Screen
                .Subscribe(_view.SetScreen)
                .AddTo(_disposables);

            _model.RequiredHits
                .Subscribe(_view.SetHitsLabel)
                .AddTo(_disposables);

            _model.Ammo
                .Subscribe(_view.SetAmmoLabel)
                .AddTo(_disposables);

            _view.CampaignRequested
                .Subscribe(_ => _levelService.TryStartCampaign())
                .AddTo(_disposables);

            _view.ModesRequested
                .Subscribe(_ => _model.ShowModes())
                .AddTo(_disposables);

            _view.ModesBackRequested
                .Subscribe(_ => _model.ShowRoot())
                .AddTo(_disposables);

            _view.SetupBackRequested
                .Subscribe(_ => _model.ShowModes())
                .AddTo(_disposables);

            _view.ModeSelected
                .Subscribe(OnModeSelected)
                .AddTo(_disposables);

            _view.HitsChanged
                .Subscribe(_model.SetRequiredHits)
                .AddTo(_disposables);

            _view.AmmoChanged
                .Subscribe(_model.SetAmmo)
                .AddTo(_disposables);

            _view.PlayRequested
                .Subscribe(_ => OnPlay())
                .AddTo(_disposables);

            _settingsModel.IsOpen
                .Subscribe(_ => RefreshUiCapture())
                .AddTo(_disposables);

            _winModel.IsOpen
                .Subscribe(_ => RefreshUiCapture())
                .AddTo(_disposables);

            _view.SetOpen(_model.IsOpen.Value);
            _view.SetScreen(_model.Screen.Value);
        }

        void BuildModeCards()
        {
            var cards = new List<(LevelType type, string titleRu, string titleEn, Sprite icon)>();
            for (var i = 0; i < _catalog.Modes.Count; i++)
            {
                var mode = _catalog.Modes[i];
                var fileName = mode.Type.ToString().ToLowerInvariant() + "_icon";
                var icon = Resources.Load<Sprite>("Modes/" + fileName);
                cards.Add((mode.Type, mode.DisplayName, mode.DisplayNameEn, icon));
            }

            _view.BindModeCards(cards);
        }

        void OnModeSelected(LevelType type)
        {
            if (!_catalog.TryGet(type, out var info) || info.Template == null)
                return;

            var hits = info.Template.RequiredHits;
            var ammo = info.Template.Ammo;
            _model.ShowSetup(type, hits, ammo);
            _view.BindSetup(
                info.DisplayName,
                info.DisplayNameEn,
                hits,
                ammo,
                info.AllowsShooting,
                hitsMin: 1,
                hitsMax: 50,
                ammoMin: 1,
                ammoMax: 100);
        }

        void OnPlay()
        {
            var selected = _model.SelectedMode.Value;
            if (selected == null)
                return;

            if (!_catalog.TryGet(selected.Value, out var info) || info.Template == null)
                return;

            _levelService.TryStartCustom(
                info.Template,
                _model.RequiredHits.Value,
                info.AllowsShooting ? _model.Ammo.Value : 0);
        }

        void RefreshUiCapture()
        {
            _inputView.SetUiCapture(
                _model.IsOpen.Value ||
                _winModel.IsOpen.Value ||
                _settingsModel.IsOpen.Value);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
