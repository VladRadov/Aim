using System;
using Aim.Services;
using UniRx;
using UnityEngine;

namespace Aim.Models
{
    public sealed class CoinsModel : IDisposable
    {
        readonly ReactiveProperty<int> _balance;

        public IReadOnlyReactiveProperty<int> Balance => _balance;

        public CoinsModel()
        {
            _balance = new ReactiveProperty<int>(Mathf.Max(0, GameSaveService.Current.Data.Coins));
            GameSaveService.Current.Loaded += ApplyLoaded;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            _balance.Value += amount;
            Persist();
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                return true;

            if (_balance.Value < amount)
                return false;

            _balance.Value -= amount;
            Persist();
            return true;
        }

        public void SetBalance(int amount)
        {
            _balance.Value = Mathf.Max(0, amount);
            Persist();
        }

        void ApplyLoaded()
        {
            _balance.Value = Mathf.Max(0, GameSaveService.Current.Data.Coins);
        }

        void Persist()
        {
            GameSaveService.Current.Data.Coins = _balance.Value;
            GameSaveService.Current.Flush();
        }

        public void Dispose()
        {
            GameSaveService.Current.Loaded -= ApplyLoaded;
            _balance.Dispose();
        }
    }
}
