using System;
using UniRx;
using UnityEngine;

namespace Aim.Models
{
    public sealed class SettingsModel : IDisposable
    {
        const string MusicKey = "Aim.MusicVolume";
        const string SfxKey = "Aim.SfxVolume";

        readonly ReactiveProperty<float> _musicVolume;
        readonly ReactiveProperty<float> _sfxVolume;
        readonly ReactiveProperty<bool> _isOpen = new(false);

        public IReadOnlyReactiveProperty<float> MusicVolume => _musicVolume;
        public IReadOnlyReactiveProperty<float> SfxVolume => _sfxVolume;
        public IReadOnlyReactiveProperty<bool> IsOpen => _isOpen;

        public SettingsModel()
        {
            _musicVolume = new ReactiveProperty<float>(PlayerPrefs.GetFloat(MusicKey, 0.7f));
            _sfxVolume = new ReactiveProperty<float>(PlayerPrefs.GetFloat(SfxKey, 0.85f));
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume.Value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, _musicVolume.Value);
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume.Value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, _sfxVolume.Value);
        }

        public void Open() => _isOpen.Value = true;

        public void Close()
        {
            _isOpen.Value = false;
            PlayerPrefs.Save();
        }

        public void Toggle()
        {
            if (_isOpen.Value)
                Close();
            else
                Open();
        }

        public void Dispose()
        {
            _musicVolume.Dispose();
            _sfxVolume.Dispose();
            _isOpen.Dispose();
        }
    }
}
