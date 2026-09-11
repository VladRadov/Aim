using System;
using Aim.Models;
using Aim.Presenters;
using Aim.Views;
using UnityEngine;

namespace Aim.Services
{
    public sealed class ShopMenuService : IDisposable
    {
        readonly ShopPresenter _shopPresenter;

        public ShopMenuService(
            ShopModel shopModel,
            CoinsModel coinsModel,
            SettingsModel settingsModel,
            LevelWinModel levelWinModel,
            ShopView shopView,
            InputView inputView,
            Action<string> onEquipWeapon,
            Func<bool> isOtherUiOpen = null)
        {
            if (shopView == null)
            {
                Debug.LogWarning("ShopMenuService: ShopView is not assigned. Run Aim/Rebuild Shop UI.");
                return;
            }

            _shopPresenter = new ShopPresenter(
                shopModel,
                coinsModel,
                shopView,
                inputView,
                onEquipWeapon,
                () => settingsModel.IsOpen.Value ||
                      levelWinModel.IsOpen.Value ||
                      (isOtherUiOpen?.Invoke() ?? false));
            _shopPresenter.Initialize();
        }

        public void Dispose() => _shopPresenter?.Dispose();
    }
}
