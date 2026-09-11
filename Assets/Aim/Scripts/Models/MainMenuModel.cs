using System;
using UniRx;
using UnityEngine;

namespace Aim.Models
{
    public enum MainMenuScreen
    {
        Root = 0,
        Modes = 1,
        Setup = 2
    }

    public sealed class MainMenuModel : IDisposable
    {
        readonly ReactiveProperty<bool> _isOpen = new(true);
        readonly ReactiveProperty<MainMenuScreen> _screen = new(MainMenuScreen.Root);
        readonly ReactiveProperty<LevelType?> _selectedMode = new(null);
        readonly ReactiveProperty<int> _requiredHits = new(10);
        readonly ReactiveProperty<int> _ammo = new(15);

        public IReadOnlyReactiveProperty<bool> IsOpen => _isOpen;
        public IReadOnlyReactiveProperty<MainMenuScreen> Screen => _screen;
        public IReadOnlyReactiveProperty<LevelType?> SelectedMode => _selectedMode;
        public IReadOnlyReactiveProperty<int> RequiredHits => _requiredHits;
        public IReadOnlyReactiveProperty<int> Ammo => _ammo;

        public void ShowRoot()
        {
            _selectedMode.Value = null;
            _screen.Value = MainMenuScreen.Root;
            _isOpen.Value = true;
        }

        public void ShowModes()
        {
            _selectedMode.Value = null;
            _screen.Value = MainMenuScreen.Modes;
            _isOpen.Value = true;
        }

        public void ShowSetup(LevelType mode, int defaultHits, int defaultAmmo)
        {
            _selectedMode.Value = mode;
            _requiredHits.Value = Mathf.Max(1, defaultHits);
            _ammo.Value = Mathf.Max(0, defaultAmmo);
            _screen.Value = MainMenuScreen.Setup;
            _isOpen.Value = true;
        }

        public void SetRequiredHits(int value) => _requiredHits.Value = Mathf.Max(1, value);

        public void SetAmmo(int value) => _ammo.Value = Mathf.Max(0, value);

        public void Close() => _isOpen.Value = false;

        public void Dispose()
        {
            _isOpen.Dispose();
            _screen.Dispose();
            _selectedMode.Dispose();
            _requiredHits.Dispose();
            _ammo.Dispose();
        }
    }
}
