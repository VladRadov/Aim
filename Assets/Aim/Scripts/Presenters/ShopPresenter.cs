using System;
using Aim.Models;
using Aim.Services;
using Aim.Views;
using UniRx;
using UnityEngine.InputSystem;

namespace Aim.Presenters
{
    public sealed class ShopPresenter : IDisposable
    {
        readonly ShopModel _shopModel;
        readonly CoinsModel _coinsModel;
        readonly ShopView _shopView;
        readonly InputView _inputView;
        readonly Action<string> _onEquipWeapon;
        readonly Func<bool> _isOtherUiOpen;
        readonly CompositeDisposable _disposables = new();

        public ShopPresenter(
            ShopModel shopModel,
            CoinsModel coinsModel,
            ShopView shopView,
            InputView inputView,
            Action<string> onEquipWeapon,
            Func<bool> isOtherUiOpen = null)
        {
            _shopModel = shopModel;
            _coinsModel = coinsModel;
            _shopView = shopView;
            _inputView = inputView;
            _onEquipWeapon = onEquipWeapon;
            _isOtherUiOpen = isOtherUiOpen;
        }

        public void Initialize()
        {
            RefreshList();

            _shopView.OpenRequested
                .Subscribe(_ =>
                {
                    if (_isOtherUiOpen?.Invoke() == true)
                        return;
                    _shopModel.Open();
                })
                .AddTo(_disposables);

            _shopView.CloseRequested
                .Subscribe(_ => _shopModel.Close())
                .AddTo(_disposables);

            _shopView.BuyRequested
                .Subscribe(id =>
                {
                    var wasEmpty = string.IsNullOrEmpty(_shopModel.EquippedWeaponId.Value);
                    if (!_shopModel.TryBuy(id, _coinsModel))
                        return;

                    RefreshList();
                    if (wasEmpty || _shopModel.IsEquipped(id))
                        _onEquipWeapon?.Invoke(id);
                })
                .AddTo(_disposables);

            _shopView.EquipRequested
                .Subscribe(id =>
                {
                    if (!_shopModel.Equip(id))
                        return;

                    _onEquipWeapon?.Invoke(id);
                    RefreshList();
                })
                .AddTo(_disposables);

            _shopModel.IsOpen
                .Skip(1)
                .Subscribe(isOpen =>
                {
                    _inputView.SetUiCapture(isOpen || (_isOtherUiOpen?.Invoke() ?? false));
                    if (isOpen)
                    {
                        RefreshList();
                        _shopView.ShowAnimated();
                    }
                    else
                    {
                        _shopView.HideAnimated();
                    }
                })
                .AddTo(_disposables);

            _coinsModel.Balance
                .Skip(1)
                .Subscribe(_ =>
                {
                    if (_shopModel.IsOpen.Value)
                        RefreshList();
                })
                .AddTo(_disposables);

            _shopModel.InventoryChanged
                .Subscribe(_ => RefreshList())
                .AddTo(_disposables);

            Observable.EveryUpdate()
                .Where(_ => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                .Subscribe(_ =>
                {
                    if (_shopModel.IsOpen.Value)
                        _shopModel.Close();
                })
                .AddTo(_disposables);
        }

        void RefreshList()
        {
            var catalog = _shopModel.Catalog;
            _shopView.RebuildWeapons(
                catalog != null ? catalog.Weapons : null,
                _shopModel.IsOwned,
                _shopModel.IsEquipped,
                _coinsModel.Balance.Value);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
