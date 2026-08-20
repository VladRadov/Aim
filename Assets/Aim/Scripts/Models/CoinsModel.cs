using System;
using UniRx;
using UnityEngine;

namespace Aim.Models
{
    public sealed class CoinsModel : IDisposable
    {
        const string CoinsKey = "Aim.Coins";

        readonly ReactiveProperty<int> _balance;

        public IReadOnlyReactiveProperty<int> Balance => _balance;

        public CoinsModel()
        {
            _balance = new ReactiveProperty<int>(Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0)));
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            _balance.Value += amount;
            Save();
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                return true;

            if (_balance.Value < amount)
                return false;

            _balance.Value -= amount;
            Save();
            return true;
        }

        public void SetBalance(int amount)
        {
            _balance.Value = Mathf.Max(0, amount);
            Save();
        }

        void Save()
        {
            PlayerPrefs.SetInt(CoinsKey, _balance.Value);
            PlayerPrefs.Save();
        }

        public void Dispose() => _balance.Dispose();
    }
}
