using System;
using Aim.Models;
using Aim.Views;
using UniRx;

namespace Aim.Presenters
{
    public sealed class CrosshairPresenter : IDisposable
    {
        readonly ShootModel _shootModel;
        readonly CrosshairView _crosshairView;
        readonly CompositeDisposable _disposables = new();

        public CrosshairPresenter(ShootModel shootModel, CrosshairView crosshairView)
        {
            _shootModel = shootModel;
            _crosshairView = crosshairView;
        }

        public void Initialize()
        {
            _shootModel.LastResult
                .Where(result => result.DidHit)
                .Subscribe(_ => _crosshairView.FlashHit())
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
