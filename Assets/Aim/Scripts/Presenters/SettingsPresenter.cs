using System;
using Aim.Models;
using Aim.Services;
using Aim.Views;
using UniRx;
using UnityEngine.InputSystem;

namespace Aim.Presenters
{
    public sealed class SettingsPresenter : IDisposable
    {
        readonly SettingsModel _settingsModel;
        readonly SettingsView _settingsView;
        readonly AudioSettingsService _audioService;
        readonly InputView _inputView;
        readonly System.Func<bool> _isOtherUiOpen;
        readonly CompositeDisposable _disposables = new();

        public SettingsPresenter(
            SettingsModel settingsModel,
            SettingsView settingsView,
            AudioSettingsService audioService,
            InputView inputView,
            System.Func<bool> isOtherUiOpen = null)
        {
            _settingsModel = settingsModel;
            _settingsView = settingsView;
            _audioService = audioService;
            _inputView = inputView;
            _isOtherUiOpen = isOtherUiOpen;
        }

        public void Initialize()
        {
            _settingsView.SetSliderValues(_settingsModel.MusicVolume.Value, _settingsModel.SfxVolume.Value);
            _audioService.Apply(_settingsModel);

            _settingsView.OpenRequested
                .Subscribe(_ =>
                {
                    if (_isOtherUiOpen?.Invoke() == true)
                        return;
                    _settingsModel.Open();
                })
                .AddTo(_disposables);

            _settingsView.CloseRequested
                .Subscribe(_ => _settingsModel.Close())
                .AddTo(_disposables);

            _settingsView.MusicChanged
                .Subscribe(value =>
                {
                    _settingsModel.SetMusicVolume(value);
                    _audioService.ApplyMusic(value);
                })
                .AddTo(_disposables);

            _settingsView.SfxChanged
                .Subscribe(value =>
                {
                    _settingsModel.SetSfxVolume(value);
                    _audioService.ApplySfx(value);
                })
                .AddTo(_disposables);

            _settingsModel.IsOpen
                .Skip(1)
                .Subscribe(isOpen =>
                {
                    _inputView.SetUiCapture(isOpen || (_isOtherUiOpen?.Invoke() ?? false));
                    if (isOpen)
                        _settingsView.ShowAnimated();
                    else
                        _settingsView.HideAnimated();
                })
                .AddTo(_disposables);

            Observable.EveryUpdate()
                .Where(_ => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                .Subscribe(_ =>
                {
                    if (_isOtherUiOpen?.Invoke() == true)
                        return;
                    _settingsModel.Toggle();
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
