using System;
using Aim.Services;
using UniRx;
using UnityEngine;

namespace Aim.Models
{
    public sealed class SettingsModel : IDisposable
    {
        readonly ReactiveProperty<float> _musicVolume;
        readonly ReactiveProperty<float> _sfxVolume;
        readonly ReactiveProperty<bool> _isOpen = new(false);

        public IReadOnlyReactiveProperty<float> MusicVolume => _musicVolume;
        public IReadOnlyReactiveProperty<float> SfxVolume => _sfxVolume;
        public IReadOnlyReactiveProperty<bool> IsOpen => _isOpen;

        public SettingsModel()
        {
            var data = GameSaveService.Current.Data;
            _musicVolume = new ReactiveProperty<float>(Mathf.Clamp01(data.MusicVolume));
            _sfxVolume = new ReactiveProperty<float>(Mathf.Clamp01(data.SfxVolume));
            GameSaveService.Current.Loaded += ApplyLoaded;
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume.Value = Mathf.Clamp01(value);
            GameSaveService.Current.Data.MusicVolume = _musicVolume.Value;
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume.Value = Mathf.Clamp01(value);
            GameSaveService.Current.Data.SfxVolume = _sfxVolume.Value;
        }

        public void Open() => _isOpen.Value = true;

        public void Close()
        {
            _isOpen.Value = false;
            GameSaveService.Current.Flush();
        }

        public void Toggle()
        {
            if (_isOpen.Value)
                Close();
            else
                Open();
        }

        void ApplyLoaded()
        {
            var data = GameSaveService.Current.Data;
            _musicVolume.Value = Mathf.Clamp01(data.MusicVolume);
            _sfxVolume.Value = Mathf.Clamp01(data.SfxVolume);
        }

        public void Dispose()
        {
            GameSaveService.Current.Loaded -= ApplyLoaded;
            _musicVolume.Dispose();
            _sfxVolume.Dispose();
            _isOpen.Dispose();
        }
    }
}
