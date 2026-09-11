using System;
using Aim.Config;
using Aim.Services;
using Aim.Views;

namespace Aim.Presenters
{
    public sealed class LevelTipPresenter : IDisposable
    {
        readonly LevelTipView _view;

        public LevelTipPresenter(LevelTipView view)
        {
            _view = view;
        }

        public void ShowForLevel(LevelDefinition definition)
        {
            if (_view == null || definition == null)
                return;

            var tip = LevelTipCatalog.GetTip(definition);
            _view.ShowTip(tip.Title, tip.Body, tip.ModeLine);
        }

        public void Hide()
        {
            _view?.HideImmediate();
        }

        public void Dispose()
        {
            Hide();
        }
    }
}
