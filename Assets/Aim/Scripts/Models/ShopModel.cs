using System;
using System.Collections.Generic;
using System.Text;
using Aim.Config;
using Aim.Services;
using UniRx;
using UnityEngine;

namespace Aim.Models
{
    public sealed class ShopModel : IDisposable
    {
        readonly WeaponShopCatalog _catalog;
        readonly HashSet<string> _owned = new();
        readonly ReactiveProperty<bool> _isOpen = new(false);
        readonly ReactiveProperty<string> _equippedId = new(string.Empty);
        readonly Subject<Unit> _inventoryChanged = new();

        public IReadOnlyReactiveProperty<bool> IsOpen => _isOpen;
        public IReadOnlyReactiveProperty<string> EquippedWeaponId => _equippedId;
        public IObservable<Unit> InventoryChanged => _inventoryChanged;
        public WeaponShopCatalog Catalog => _catalog;

        public ShopModel(WeaponShopCatalog catalog)
        {
            _catalog = catalog;
            Load();
            GameSaveService.Current.Loaded += ApplyLoaded;
        }

        public void Open() => _isOpen.Value = true;

        public void Close() => _isOpen.Value = false;

        public void Toggle()
        {
            if (_isOpen.Value)
                Close();
            else
                Open();
        }

        public bool IsOwned(string weaponId)
        {
            return !string.IsNullOrEmpty(weaponId) && _owned.Contains(weaponId);
        }

        public bool IsEquipped(string weaponId)
        {
            return !string.IsNullOrEmpty(weaponId) && _equippedId.Value == weaponId;
        }

        public ShopWeaponEntry GetEquippedEntry()
        {
            return FindById(_equippedId.Value);
        }

        public ShopWeaponEntry FindById(string weaponId)
        {
            if (_catalog == null || _catalog.Weapons == null || string.IsNullOrEmpty(weaponId))
                return null;

            for (var i = 0; i < _catalog.Weapons.Length; i++)
            {
                var entry = _catalog.Weapons[i];
                if (entry != null && entry.Id == weaponId)
                    return entry;
            }

            return null;
        }

        public bool TryBuy(string weaponId, CoinsModel coins)
        {
            var entry = FindById(weaponId);
            if (entry == null || entry.Prefab == null || coins == null)
                return false;

            if (IsOwned(weaponId))
                return false;

            if (!coins.TrySpend(entry.Price))
                return false;

            _owned.Add(weaponId);
            Persist(flush: true);
            _inventoryChanged.OnNext(Unit.Default);

            if (string.IsNullOrEmpty(_equippedId.Value))
                Equip(weaponId);

            return true;
        }

        public bool Equip(string weaponId)
        {
            if (!IsOwned(weaponId))
                return false;

            var entry = FindById(weaponId);
            if (entry == null || entry.Prefab == null)
                return false;

            _equippedId.Value = weaponId;
            Persist(flush: true);
            _inventoryChanged.OnNext(Unit.Default);
            return true;
        }

        void ApplyLoaded()
        {
            Load();
            _inventoryChanged.OnNext(Unit.Default);
        }

        void Load()
        {
            _owned.Clear();

            if (_catalog != null && _catalog.Weapons != null)
            {
                for (var i = 0; i < _catalog.Weapons.Length; i++)
                {
                    var entry = _catalog.Weapons[i];
                    if (entry != null && entry.OwnedByDefault && !string.IsNullOrEmpty(entry.Id))
                        _owned.Add(entry.Id);
                }
            }

            var saved = GameSaveService.Current.Data.OwnedWeapons;
            if (!string.IsNullOrEmpty(saved))
            {
                var parts = saved.Split('|');
                for (var i = 0; i < parts.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(parts[i]))
                        _owned.Add(parts[i]);
                }
            }

            var equipped = GameSaveService.Current.Data.EquippedWeapon;
            if (!string.IsNullOrEmpty(equipped) && _owned.Contains(equipped) && FindById(equipped) != null)
            {
                _equippedId.Value = equipped;
                Persist(flush: false);
                return;
            }

            if (_catalog?.Weapons != null)
            {
                for (var i = 0; i < _catalog.Weapons.Length; i++)
                {
                    var entry = _catalog.Weapons[i];
                    if (entry == null || entry.Prefab == null)
                        continue;

                    if (_owned.Contains(entry.Id))
                    {
                        _equippedId.Value = entry.Id;
                        Persist(flush: false);
                        return;
                    }
                }
            }

            Persist(flush: false);
        }

        void Persist(bool flush)
        {
            var data = GameSaveService.Current.Data;
            data.OwnedWeapons = JoinOwned();
            data.EquippedWeapon = _equippedId.Value ?? string.Empty;
            if (flush)
                GameSaveService.Current.Flush();
        }

        string JoinOwned()
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var id in _owned)
            {
                if (!first)
                    builder.Append('|');
                builder.Append(id);
                first = false;
            }

            return builder.ToString();
        }

        public void Dispose()
        {
            GameSaveService.Current.Loaded -= ApplyLoaded;
            _isOpen.Dispose();
            _equippedId.Dispose();
            _inventoryChanged.Dispose();
        }
    }
}
