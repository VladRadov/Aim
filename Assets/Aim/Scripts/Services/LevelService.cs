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
        readonly AimTrainerConfig _config;
        readonly LevelController _controller;
        readonly List<LevelDefinition> _playlist = new();
        readonly CompositeDisposable _disposables = new();

        LevelWinPresenter _winPresenter;
        int _index = -1;

        public LevelController Controller => _controller;
        public LevelDefinition ActiveLevel => _controller.ActiveDefinition;
        public IReadOnlyList<LevelDefinition> Playlist => _playlist;
        public int CurrentIndex => _index;

        public bool HasNext =>
            _playlist.Count > 0 &&
            _index >= 0 &&
            _index + 1 < _playlist.Count &&
            _playlist[_index + 1] != null;

        public LevelService(
            SessionModel session,
            CoinsModel coins,
            SettingsModel settings,
            LevelWinModel winModel,
            AimTrainerConfig config,
            Transform targetsRoot,
            Camera aimCamera)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _coins = coins ?? throw new ArgumentNullException(nameof(coins));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _winModel = winModel ?? throw new ArgumentNullException(nameof(winModel));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _controller = new LevelController(
                session,
                settings,
                targetsRoot,
                aimCamera,
                config.TargetLayerMask);

            _session.State
                .Where(state => state == LevelState.Won || state == LevelState.Lost)
                .Subscribe(_ => _controller.StopLevel(resetSession: false))
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
                this,
                _config,
                view,
                inputView);
            _winPresenter.Initialize();
        }

        public void SetPlaylist(IEnumerable<LevelDefinition> levels)
        {
            _playlist.Clear();
            _index = -1;
            if (levels == null)
                return;

            foreach (var level in levels)
            {
                if (level != null)
                    _playlist.Add(level);
            }
        }

        public void Enqueue(LevelDefinition level)
        {
            if (level != null)
                _playlist.Add(level);
        }

        public void EnqueueRange(IEnumerable<LevelDefinition> levels)
        {
            if (levels == null)
                return;

            foreach (var level in levels)
                Enqueue(level);
        }

        public void InsertNext(LevelDefinition level)
        {
            if (level == null)
                return;

            var insertAt = Mathf.Clamp(_index + 1, 0, _playlist.Count);
            _playlist.Insert(insertAt, level);
        }

        public void ClearPlaylist()
        {
            _playlist.Clear();
            _index = -1;
        }

        public bool TryStartFirst()
        {
            if (_playlist.Count == 0)
                return false;

            _index = 0;
            _controller.StartLevel(_playlist[0]);
            return true;
        }

        public bool TryStartNext()
        {
            if (!HasNext)
                return false;

            _index++;
            _controller.StartLevel(_playlist[_index]);
            return true;
        }

        public void StartLevel(LevelDefinition definition)
        {
            if (definition == null)
                return;

            var playlistIndex = _playlist.IndexOf(definition);
            _index = playlistIndex;
            if (playlistIndex < 0)
            {
                _playlist.Add(definition);
                _index = _playlist.Count - 1;
            }

            _controller.StartLevel(definition);
        }

        public void Stop()
        {
            _controller.StopLevel(resetSession: true);
        }

        public void Dispose()
        {
            _winPresenter?.Dispose();
            _winPresenter = null;
            _disposables.Dispose();
            _controller.Dispose();
        }
    }
}
