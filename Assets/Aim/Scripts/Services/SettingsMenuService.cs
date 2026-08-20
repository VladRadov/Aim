using System;
using Aim.Models;
using Aim.Presenters;
using Aim.Views;
using UnityEngine;

namespace Aim.Services
{
    public sealed class SettingsMenuService : IDisposable
    {
        readonly SettingsPresenter _settingsPresenter;
        readonly AudioSettingsService _audioService;

        public SettingsMenuService(
            SettingsModel settings,
            LevelWinModel levelWin,
            SettingsView settingsView,
            InputView inputView,
            AudioSource musicSource,
            AudioSource sfxSource,
            Func<bool> isOtherUiOpen = null)
        {
            _audioService = new AudioSettingsService(musicSource, sfxSource);

            if (settingsView == null)
            {
                Debug.LogWarning("SettingsMenuService: SettingsView is not assigned. Run Aim/Rebuild Settings UI.");
                return;
            }

            _settingsPresenter = new SettingsPresenter(
                settings,
                settingsView,
                _audioService,
                inputView,
                () => levelWin.IsOpen.Value || (isOtherUiOpen?.Invoke() ?? false));
            _settingsPresenter.Initialize();
        }

        public void Dispose()
        {
            _settingsPresenter?.Dispose();
        }
    }
}
