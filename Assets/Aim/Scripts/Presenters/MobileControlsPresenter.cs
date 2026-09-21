using System;
using Aim.Models;
using Aim.Services;
using Aim.Views;
using UniRx;

namespace Aim.Presenters
{
    public sealed class MobileControlsPresenter : IDisposable
    {
        readonly SessionModel _session;
        readonly InputView _inputView;
        readonly MobileControlsView _view;
        readonly CompositeDisposable _disposables = new();
        bool _touchLayout;
        bool _lastVisible;
        bool _lastFire;

        public MobileControlsPresenter(
            SessionModel session,
            InputView inputView,
            MobileControlsView view)
        {
            _session = session;
            _inputView = inputView;
            _view = view;
        }

        public void Initialize()
        {
            if (_view == null || _inputView == null || _session == null)
                return;

            _touchLayout = MobileDevice.IsHandheld();

            _view.LookStream
                .Subscribe(delta => _inputView.PushLook(delta))
                .AddTo(_disposables);

            _view.FireStream
                .Subscribe(_ => _inputView.PushAttack())
                .AddTo(_disposables);

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    if (!_touchLayout && MobileDevice.HasTouchThisFrame())
                        _touchLayout = true;

                    Refresh();
                })
                .AddTo(_disposables);

            Refresh();
        }

        void Refresh()
        {
            var show = _session.IsPlaying &&
                       !_inputView.IsUiCapture &&
                       (_touchLayout || MobileDevice.IsHandheld());
            var showFire = show && _session.AllowsShooting.Value;

            if (show == _lastVisible && showFire == _lastFire)
                return;

            _lastVisible = show;
            _lastFire = showFire;
            _view.SetVisible(show, showFire);
            _inputView.SetOnScreenAim(show);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
