using System;
using Aim.Models;
using Aim.Views;
using UniRx;

namespace Aim.Presenters
{
    public sealed class SessionHudPresenter : IDisposable
    {
        readonly SessionModel _sessionModel;
        readonly SessionHudView _hudView;
        readonly CompositeDisposable _disposables = new();

        public SessionHudPresenter(SessionModel sessionModel, SessionHudView hudView)
        {
            _sessionModel = sessionModel;
            _hudView = hudView;
        }

        public void Initialize()
        {
            _sessionModel.Hits
                .CombineLatest(_sessionModel.RequiredHits, (hits, required) => (hits, required))
                .Subscribe(value => _hudView.SetHits(value.hits, value.required))
                .AddTo(_disposables);

            _sessionModel.AmmoRemaining
                .CombineLatest(_sessionModel.AmmoCapacity, (remaining, capacity) => (remaining, capacity))
                .Subscribe(value => _hudView.SetAmmo(value.remaining, value.capacity))
                .AddTo(_disposables);

            _sessionModel.State
                .Subscribe(_hudView.SetState)
                .AddTo(_disposables);

            // Force initial paint in case reactive streams skip first values.
            _hudView.SetHits(_sessionModel.Hits.Value, _sessionModel.RequiredHits.Value);
            _hudView.SetAmmo(_sessionModel.AmmoRemaining.Value, _sessionModel.AmmoCapacity.Value);
            _hudView.SetState(_sessionModel.State.Value);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
