using System;
using UniRx;

namespace Aim.Models
{
    public sealed class ShootModel : IDisposable
    {
        readonly ReactiveProperty<int> _hits = new(0);
        readonly ReactiveProperty<int> _shots = new(0);
        readonly ReactiveProperty<ShootResult> _lastResult = new(ShootResult.Miss);

        public IReadOnlyReactiveProperty<int> Hits => _hits;
        public IReadOnlyReactiveProperty<int> Shots => _shots;
        public IReadOnlyReactiveProperty<ShootResult> LastResult => _lastResult;

        public void RegisterShot(ShootResult result)
        {
            _shots.Value++;
            if (result.DidHit)
                _hits.Value++;

            _lastResult.Value = result;
        }

        public void ResetStats()
        {
            _hits.Value = 0;
            _shots.Value = 0;
            _lastResult.Value = ShootResult.Miss;
        }

        public void Dispose()
        {
            _hits.Dispose();
            _shots.Dispose();
            _lastResult.Dispose();
        }
    }
}
