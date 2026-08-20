using System;
using UniRx;

namespace Aim.Models
{
    public sealed class SessionModel : IDisposable
    {
        readonly ReactiveProperty<int> _hits = new(0);
        readonly ReactiveProperty<int> _requiredHits = new(0);
        readonly ReactiveProperty<int> _ammoRemaining = new(0);
        readonly ReactiveProperty<int> _ammoCapacity = new(0);
        readonly ReactiveProperty<bool> _allowsShooting = new(true);
        readonly ReactiveProperty<LevelState> _state = new(LevelState.Idle);

        public IReadOnlyReactiveProperty<int> Hits => _hits;
        public IReadOnlyReactiveProperty<int> RequiredHits => _requiredHits;
        public IReadOnlyReactiveProperty<int> AmmoRemaining => _ammoRemaining;
        public IReadOnlyReactiveProperty<int> AmmoCapacity => _ammoCapacity;
        public IReadOnlyReactiveProperty<bool> AllowsShooting => _allowsShooting;
        public IReadOnlyReactiveProperty<LevelState> State => _state;

        public bool IsPlaying => _state.Value == LevelState.Playing;

        public void Start(int requiredHits, int ammo, bool allowsShooting = true)
        {
            _requiredHits.Value = System.Math.Max(1, requiredHits);
            _allowsShooting.Value = allowsShooting;
            _ammoCapacity.Value = allowsShooting ? System.Math.Max(0, ammo) : 0;
            _ammoRemaining.Value = _ammoCapacity.Value;
            _hits.Value = 0;
            _state.Value = LevelState.Playing;
        }

        public bool TryConsumeAmmo()
        {
            if (!IsPlaying || !_allowsShooting.Value || _ammoRemaining.Value <= 0)
                return false;

            _ammoRemaining.Value--;
            return true;
        }

        public void RegisterScoreHit()
        {
            if (!IsPlaying)
                return;

            _hits.Value++;
            if (_hits.Value >= _requiredHits.Value)
                _state.Value = LevelState.Won;
        }

        public void EvaluateAfterShotResolved()
        {
            if (!IsPlaying)
                return;

            if (_hits.Value >= _requiredHits.Value)
            {
                _state.Value = LevelState.Won;
                return;
            }

            if (_ammoRemaining.Value <= 0)
                _state.Value = LevelState.Lost;
        }

        public void Stop()
        {
            if (_state.Value == LevelState.Playing)
                _state.Value = LevelState.Idle;
        }

        public void Dispose()
        {
            _hits.Dispose();
            _requiredHits.Dispose();
            _ammoRemaining.Dispose();
            _ammoCapacity.Dispose();
            _allowsShooting.Dispose();
            _state.Dispose();
        }
    }
}
