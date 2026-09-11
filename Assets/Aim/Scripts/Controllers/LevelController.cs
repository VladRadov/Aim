using System;
using Aim.Config;
using Aim.Models;
using Aim.Services;
using Aim.Services.Levels;
using UnityEngine;

namespace Aim.Controllers
{
    public sealed class LevelController : IDisposable
    {
        readonly SessionModel _session;
        readonly SettingsModel _settings;
        readonly Transform _targetsRoot;
        readonly Camera _aimCamera;
        readonly LayerMask _targetMask;

        CharacterTargetPool _characterPool;
        FlyingTargetPool _flyingPool;
        BouncingTargetPool _bouncingPool;
        TrackingTargetPool _trackingPool;
        TrackingMoverPool _moverPool;
        PeekTargetPool _peekPool;
        PriorityTargetPool _priorityPool;
        MultiHitTargetPool _multiHitPool;
        ILevelRunner _activeRunner;

        public LevelDefinition ActiveDefinition { get; private set; }

        public LevelController(
            SessionModel session,
            SettingsModel settings,
            Transform targetsRoot,
            Camera aimCamera,
            LayerMask targetMask)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _targetsRoot = targetsRoot;
            _aimCamera = aimCamera;
            _targetMask = targetMask;
        }

        public void StartLevel(LevelDefinition definition, int? requiredHitsOverride = null, int? ammoOverride = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            StopRunner();
            EnsurePools(definition);

            ActiveDefinition = definition;
            var requiredHits = requiredHitsOverride ?? definition.RequiredHits;
            var ammo = ammoOverride ?? definition.Ammo;
            _session.Start(requiredHits, ammo, definition.AllowsShooting);
            _activeRunner = CreateRunner(definition.LevelType);
            _activeRunner.Start(definition);
        }

        public void StopLevel(bool resetSession)
        {
            StopRunner();
            ActiveDefinition = null;
            if (resetSession)
                _session.Stop();
        }

        void EnsurePools(LevelDefinition definition)
        {
            if (definition is CharacterHeadshotLevelDefinition characterLevel &&
                characterLevel.CharacterPrefab != null &&
                _characterPool == null)
            {
                _characterPool = new CharacterTargetPool(characterLevel.CharacterPrefab, _targetsRoot);
            }

            if (definition is FlyingObjectsLevelDefinition flyingLevel &&
                flyingLevel.FlyingPrefab != null &&
                _flyingPool == null)
            {
                _flyingPool = new FlyingTargetPool(flyingLevel.FlyingPrefab, _targetsRoot);
            }

            if (definition is StaticBallsLevelDefinition staticLevel &&
                staticLevel.StaticPrefab != null &&
                _flyingPool == null)
            {
                _flyingPool = new FlyingTargetPool(staticLevel.StaticPrefab, _targetsRoot);
            }

            if (definition is FlickTargetsLevelDefinition flickLevel &&
                flickLevel.FlickPrefab != null &&
                _flyingPool == null)
            {
                _flyingPool = new FlyingTargetPool(flickLevel.FlickPrefab, _targetsRoot);
            }

            if (definition is MovingRailsLevelDefinition railsLevel &&
                railsLevel.RailPrefab != null &&
                _flyingPool == null)
            {
                _flyingPool = new FlyingTargetPool(railsLevel.RailPrefab, _targetsRoot);
            }

            if (definition is PrecisionCirclesLevelDefinition precisionLevel &&
                precisionLevel.CirclePrefab != null &&
                _flyingPool == null)
            {
                _flyingPool = new FlyingTargetPool(precisionLevel.CirclePrefab, _targetsRoot);
            }

            if (definition is PeekTargetsLevelDefinition peekLevel &&
                peekLevel.PeekPrefab != null &&
                _peekPool == null)
            {
                _peekPool = new PeekTargetPool(peekLevel.PeekPrefab, _targetsRoot);
            }

            if (definition is PopupDucksLevelDefinition popupLevel &&
                popupLevel.DuckPrefab != null &&
                _peekPool == null)
            {
                _peekPool = new PeekTargetPool(popupLevel.DuckPrefab, _targetsRoot);
            }

            if (definition is PriorityTargetsLevelDefinition priorityLevel &&
                priorityLevel.PriorityPrefab != null &&
                _priorityPool == null)
            {
                _priorityPool = new PriorityTargetPool(priorityLevel.PriorityPrefab, _targetsRoot);
            }

            if (definition is DoubleTapLevelDefinition doubleTapLevel &&
                doubleTapLevel.MultiHitPrefab != null &&
                _multiHitPool == null)
            {
                _multiHitPool = new MultiHitTargetPool(doubleTapLevel.MultiHitPrefab, _targetsRoot);
            }

            if (definition is BouncingBallsLevelDefinition bouncingLevel &&
                bouncingLevel.BouncingPrefab != null &&
                _bouncingPool == null)
            {
                _bouncingPool = new BouncingTargetPool(bouncingLevel.BouncingPrefab, _targetsRoot);
            }

            if (definition is TrackingBallLevelDefinition trackingLevel &&
                trackingLevel.TrackingPrefab != null &&
                _trackingPool == null)
            {
                _trackingPool = new TrackingTargetPool(trackingLevel.TrackingPrefab, _targetsRoot);
            }

            if (definition is TrackingMoversLevelDefinition moversLevel &&
                moversLevel.MoverPrefab != null &&
                _moverPool == null)
            {
                _moverPool = new TrackingMoverPool(moversLevel.MoverPrefab, _targetsRoot);
            }
        }

        ILevelRunner CreateRunner(LevelType type)
        {
            return type switch
            {
                LevelType.CharacterHeadshot => new CharacterLevelRunner(_characterPool, _targetsRoot),
                LevelType.FlyingObjects => new FlyingObjectsLevelRunner(_flyingPool),
                LevelType.CustomHitZones => new CustomHitZonesLevelRunner(_targetsRoot),
                LevelType.ShootingGallery => new ShootingGalleryLevelRunner(_targetsRoot),
                LevelType.BouncingBalls => new BouncingBallsLevelRunner(_bouncingPool, _targetsRoot),
                LevelType.TrackingBall => new TrackingBallLevelRunner(
                    _trackingPool,
                    _targetsRoot,
                    _session,
                    _settings,
                    _aimCamera,
                    _targetMask),
                LevelType.TrackingMovers => new TrackingMoversLevelRunner(
                    _moverPool,
                    _session,
                    _settings,
                    _aimCamera,
                    _targetMask),
                LevelType.StaticBalls => new StaticBallsLevelRunner(_flyingPool),
                LevelType.FlickTargets => new FlickTargetsLevelRunner(_flyingPool),
                LevelType.PeekTargets => new PeekTargetsLevelRunner(_peekPool, _targetsRoot),
                LevelType.MovingRails => new MovingRailsLevelRunner(_flyingPool),
                LevelType.PopupDucks => new PopupDucksLevelRunner(_peekPool, _targetsRoot),
                LevelType.PriorityTargets => new PriorityTargetsLevelRunner(_priorityPool),
                LevelType.PrecisionCircles => new PrecisionCirclesLevelRunner(_flyingPool),
                LevelType.DoubleTap => new DoubleTapLevelRunner(_multiHitPool, _aimCamera),
                _ => new FlyingObjectsLevelRunner(_flyingPool)
            };
        }

        void StopRunner()
        {
            _activeRunner?.Stop();
            _activeRunner?.Dispose();
            _activeRunner = null;
        }

        public void Dispose()
        {
            StopRunner();
            ActiveDefinition = null;
            _characterPool?.Dispose();
            _flyingPool?.Dispose();
            _bouncingPool?.Dispose();
            _trackingPool?.Dispose();
            _moverPool?.Dispose();
            _peekPool?.Dispose();
            _priorityPool?.Dispose();
            _multiHitPool?.Dispose();
            _characterPool = null;
            _flyingPool = null;
            _bouncingPool = null;
            _trackingPool = null;
            _moverPool = null;
            _peekPool = null;
            _priorityPool = null;
            _multiHitPool = null;
        }
    }
}
