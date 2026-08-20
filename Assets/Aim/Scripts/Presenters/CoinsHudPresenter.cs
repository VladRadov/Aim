using System;
using Aim.Models;
using Aim.Views;
using UniRx;

namespace Aim.Presenters
{
    public sealed class CoinsHudPresenter : IDisposable
    {
        readonly CoinsModel _coinsModel;
        readonly CoinsHudView _hudView;
        readonly CompositeDisposable _disposables = new();

        public CoinsHudPresenter(CoinsModel coinsModel, CoinsHudView hudView)
        {
            _coinsModel = coinsModel;
            _hudView = hudView;
        }

        public void Initialize()
        {
            _coinsModel.Balance
                .Subscribe(_hudView.SetCoins)
                .AddTo(_disposables);

            _hudView.SetCoins(_coinsModel.Balance.Value);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
