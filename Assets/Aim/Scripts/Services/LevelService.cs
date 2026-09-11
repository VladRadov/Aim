using System;
using System.Collections.Generic;
using Aim.Config;
using Aim.Controllers;
using Aim.Models;
using Aim.Presenters;
using Aim.Views;
using UniRx;
using UnityEngine;

namespace Aim.Services
{
    public sealed class LevelService : IDisposable
    {
        readonly SessionModel _session;
        readonly CoinsModel _coins;
        readonly SettingsModel _settings;
        readonly LevelWinModel _winModel;
        readonly MainMenuModel _mainMenuModel;
        readonly LevelRunStatsTracker _statsTracker;
        readonly CampaignProgressStore _campaignProgress = new();
        readonly AimTrainerConfig _config;
        readonly LevelController _controller;
        readonly List<LevelDefinition> _campaignPlaylist = new();
        readonly List<LevelDefinition> _playlist = new();
        readonly CompositeDisposable _disposables = new();

        LevelWinPresenter _winPresenter;
        LevelTipPresenter _tipPresenter;
        int _index = -1;
        bool _isCampaign;
        int? _hitsOverride;
        int? _ammoOverride;

        public LevelController Controller => _controller;
        public LevelDefinition ActiveLevel => _controller.ActiveDefinition;
        public IReadOnlyList<LevelDefinition> Playlist => _playlist;
        public int CurrentIndex => _index;
        public bool IsCampaign => _isCampaign;
        public CampaignProgressStore CampaignProgress => _campaignProgress;

        public bool HasNext =>
            _isCampaign &&
            _playlist.Count > 0 &&
            _index >= 0 &&
            _index + 1 < _playlist.Count &&
            _playlist[_index + 1] != null;

        public LevelService(
            SessionModel session,
            CoinsModel coins,
            SettingsModel settings,
            LevelWinModel winModel,
            MainMenuModel mainMenuModel,
            LevelRunStatsTracker statsTracker,
            AimTrainerConfig config,
            Transform targetsRoot,
            Camera aimCamera)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _coins = coins ?? throw new ArgumentNullException(nameof(coins));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _winModel = winModel ?? throw new ArgumentNullException(nameof(winModel));
            _mainMenuModel = mainMenuModel ?? throw new ArgumentNullException(nameof(mainMenuModel));
            _statsTracker = statsTracker ?? throw new ArgumentNullException(nameof(statsTracker));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _controller = new LevelController(
                session,
                settings,
                targetsRoot,
                aimCamera,
                config.TargetLayerMask);

            _session.State
                .Where(state => state == LevelState.Won || state == LevelState.Lost)
                .Subscribe(state =>
                {
                    if (state == LevelState.Won && _isCampaign && _index >= 0)
                    {
                        var nextIndex = HasNext ? _index + 1 : _index;
                        _campaignProgress.SetIndex(nextIndex);
                    }

                    _tipPresenter?.Hide();
                    _controller.StopLevel(resetSession: false);
                })
                .AddTo(_disposables);
        }

        public void BindWinUi(LevelWinView view, InputView inputView)
        {
            _winPresenter?.Dispose();
            _winPresenter = null;

            if (view == null)
            {
                Debug.LogWarning("LevelService: LevelWinView is not assigned. Run Aim/Rebuild Level Win UI.");
                return;
            }

            _winPresenter = new LevelWinPresenter(
                _winModel,
                _session,
                _coins,
                _settings,
                _mainMenuModel,
                this,
                _statsTracker,
                _config,
                view,
                inputView);
            _winPresenter.Initialize();
        }

        public void BindTipUi(LevelTipView view)
        {
            _tipPresenter?.Dispose();
            _tipPresenter = null;

            if (view == null)
            {
                Debug.LogWarning("LevelService: LevelTipView is not assigned. Run Aim/Rebuild Level Tip UI.");
                return;
            }

            _tipPresenter = new LevelTipPresenter(view);
        }

        public void SetCampaignPlaylist(IEnumerable<LevelDefinition> levels)
        {
            _campaignPlaylist.Clear();
            if (levels == null)
                return;

            foreach (var level in levels)
            {
                if (level != null)
                    _campaignPlaylist.Add(level);
            }
        }

        public void SetPlaylist(IEnumerable<LevelDefinition> levels)
        {
            SetCampaignPlaylist(levels);
        }

        public bool TryStartCampaign()
        {
            if (_campaignPlaylist.Count == 0)
                return false;

            _isCampaign = true;
            ClearOverrides();
            _playlist.Clear();
            _playlist.AddRange(_campaignPlaylist);

            var index = Mathf.Clamp(_campaignProgress.GetIndex(), 0, _playlist.Count - 1);
            _index = index;
            _campaignProgress.SetIndex(_index);
            _mainMenuModel.Close();
            StartLevelInternal(_playlist[_index]);
            return true;
        }

        public bool TryStartCustom(LevelDefinition definition, int requiredHits, int ammo)
        {
            if (definition == null)
                return false;

            _isCampaign = false;
            _playlist.Clear();
            _playlist.Add(definition);
            _index = 0;
            _hitsOverride = Mathf.Max(1, requiredHits);
            _ammoOverride = definition.AllowsShooting ? Mathf.Max(0, ammo) : 0;
            _mainMenuModel.Close();
            StartLevelInternal(definition);
            return true;
        }

        public bool TryStartNext()
        {
            if (!HasNext)
                return false;

            _index++;
            _campaignProgress.SetIndex(_index);
            StartLevelInternal(_playlist[_index]);
            return true;
        }

        public bool TryRestartCurrent()
        {
            if (_index < 0 || _index >= _playlist.Count || _playlist[_index] == null)
                return false;

            StartLevelInternal(_playlist[_index]);
            return true;
        }

        public void ReturnToMenu()
        {
            _winModel.Close();
            _tipPresenter?.Hide();
            _controller.StopLevel(resetSession: true);
            ClearOverrides();
            _isCampaign = false;
            _index = -1;
            _playlist.Clear();
            _mainMenuModel.ShowRoot();
        }

        void StartLevelInternal(LevelDefinition definition)
        {
            _tipPresenter?.Hide();
            _statsTracker.Begin(definition);
            _controller.StartLevel(definition, _hitsOverride, _ammoOverride);
            _tipPresenter?.ShowForLevel(definition);
        }

        void ClearOverrides()
        {
            _hitsOverride = null;
            _ammoOverride = null;
        }

        public void Stop()
        {
            _tipPresenter?.Hide();
            _controller.StopLevel(resetSession: true);
        }

        public void Dispose()
        {
            _winPresenter?.Dispose();
            _winPresenter = null;
            _tipPresenter?.Dispose();
            _tipPresenter = null;
            _disposables.Dispose();
            _controller.Dispose();
        }
    }
}
