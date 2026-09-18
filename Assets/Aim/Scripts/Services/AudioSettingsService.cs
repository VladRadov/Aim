using System.Collections.Generic;
using Aim.Models;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace Aim.Services
{
    public sealed class AudioSettingsService : MonoBehaviour, IEntityService
    {
        public static AudioSettingsService Current { get; private set; }

        [Inject(Id = "Music", Optional = true)]
        AudioSource _musicSource;

        [Inject(Id = "Sfx", Optional = true)]
        AudioSource _sfxSource;

        [Inject(Optional = true)]
        SettingsModel _settings;

        [Inject(Optional = true)]
        SessionModel _session;

        [Inject(Optional = true)]
        CoinsModel _coins;

        [SerializeField] AudioClip shootClip;
        [SerializeField] AudioClip hitClip;
        [SerializeField] AudioClip missClip;
        [SerializeField] AudioClip winClip;
        [SerializeField] AudioClip loseClip;
        [SerializeField] AudioClip clickClip;
        [SerializeField] AudioClip weaponClip;
        [SerializeField] AudioClip spawnClip;
        [SerializeField] AudioClip healthDrainClip;
        [SerializeField] AudioClip coinClip;
        [SerializeField] AudioClip musicClip;

        AudioSource _drainSource;
        float _drainUntil;

        readonly List<RaycastResult> _raycastHits = new();
        readonly CompositeDisposable _disposables = new();
        PointerEventData _pointerEvent;
        float _sfxVolume = 1f;
        bool _initialized;

        void Awake()
        {
            Current = this;
            EnsureSources();
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            EnsureSources();
            EnsureClips();

            if (_settings != null)
                Apply(_settings);

            if (_musicSource != null && musicClip != null)
            {
                _musicSource.clip = musicClip;
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
                _musicSource.spatialBlend = 0f;
                if (!_musicSource.isPlaying)
                    _musicSource.Play();
            }

            if (_session != null)
            {
                _session.State
                    .Where(state => state == LevelState.Won || state == LevelState.Lost)
                    .Subscribe(state =>
                    {
                        if (state == LevelState.Won)
                            PlayWin();
                        else
                            PlayLose();
                    })
                    .AddTo(_disposables);
            }

            if (_coins != null)
            {
                _coins.Balance
                    .Pairwise()
                    .Where(pair => pair.Current > pair.Previous)
                    .Subscribe(_ => PlayCoin())
                    .AddTo(_disposables);
            }

            _initialized = true;
        }

        public void Apply(SettingsModel settings)
        {
            ApplyMusic(settings.MusicVolume.Value);
            ApplySfx(settings.SfxVolume.Value);
        }

        public void ApplyMusic(float volume)
        {
            if (_musicSource != null)
                _musicSource.volume = Mathf.Clamp01(volume);
        }

        public void ApplySfx(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            if (_sfxSource != null)
                _sfxSource.volume = _sfxVolume;
            if (_drainSource != null)
                _drainSource.volume = _sfxVolume * 0.55f;
        }

        public void PlayShoot() => PlaySfx(shootClip, 1f);
        public void PlayHit() => PlaySfx(hitClip, RandomPitch(0.96f, 1.06f));
        public void PlayMiss() => PlaySfx(missClip, RandomPitch(0.94f, 1.04f));
        public void PlayWin() => PlaySfx(winClip, 1f);
        public void PlayLose() => PlaySfx(loseClip, 1f);
        public void PlayClick() => PlaySfx(clickClip, RandomPitch(0.97f, 1.03f));
        public void PlayWeaponChange() => PlaySfx(weaponClip, 1f);
        public void PlaySpawn() => PlaySfx(spawnClip, RandomPitch(0.92f, 1.08f));
        public void PlayCoin() => PlaySfx(coinClip, RandomPitch(0.97f, 1.04f));

        public void PlayHealthDrain()
        {
            if (_drainSource == null || healthDrainClip == null || _sfxVolume <= 0.001f)
                return;

            _drainUntil = Time.unscaledTime + 0.12f;
            _drainSource.volume = _sfxVolume * 0.55f;
            if (!_drainSource.isPlaying)
                _drainSource.Play();
        }

        public void PlaySfx(AudioClip clip, float pitch = 1f)
        {
            if (_sfxSource == null || clip == null || _sfxVolume <= 0.001f)
                return;

            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        void Update()
        {
            if (_drainSource != null && _drainSource.isPlaying && Time.unscaledTime > _drainUntil)
                _drainSource.Stop();

            if (!_initialized)
                return;

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;

            _pointerEvent ??= new PointerEventData(eventSystem);
            _pointerEvent.Reset();
            _pointerEvent.position = Mouse.current.position.ReadValue();
            _raycastHits.Clear();
            eventSystem.RaycastAll(_pointerEvent, _raycastHits);

            for (var i = 0; i < _raycastHits.Count; i++)
            {
                if (_raycastHits[i].gameObject.GetComponentInParent<Button>() == null)
                    continue;
                PlayClick();
                break;
            }
        }

        void EnsureSources()
        {
            if (_musicSource == null)
            {
                _musicSource = GetComponent<AudioSource>();
                if (_musicSource == null)
                    _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.loop = true;
                _musicSource.spatialBlend = 0f;
            }

            if (_sfxSource == null)
            {
                var sfxTransform = transform.Find("SfxSource");
                if (sfxTransform == null)
                {
                    var sfxGo = new GameObject("SfxSource");
                    sfxGo.transform.SetParent(transform, false);
                    _sfxSource = sfxGo.AddComponent<AudioSource>();
                }
                else
                {
                    _sfxSource = sfxTransform.GetComponent<AudioSource>() ??
                                 sfxTransform.gameObject.AddComponent<AudioSource>();
                }

                _sfxSource.playOnAwake = false;
                _sfxSource.loop = false;
                _sfxSource.spatialBlend = 0f;
            }

            if (_drainSource == null)
            {
                var drainTransform = transform.Find("DrainSource");
                if (drainTransform == null)
                {
                    var drainGo = new GameObject("DrainSource");
                    drainGo.transform.SetParent(transform, false);
                    _drainSource = drainGo.AddComponent<AudioSource>();
                }
                else
                {
                    _drainSource = drainTransform.GetComponent<AudioSource>() ??
                                   drainTransform.gameObject.AddComponent<AudioSource>();
                }

                _drainSource.playOnAwake = false;
                _drainSource.loop = true;
                _drainSource.spatialBlend = 0f;
            }
        }

        void EnsureClips()
        {
            if (shootClip == null) shootClip = ProceduralSfx.Shoot();
            if (hitClip == null) hitClip = ProceduralSfx.Hit();
            if (missClip == null) missClip = ProceduralSfx.Miss();
            if (winClip == null) winClip = ProceduralSfx.Win();
            if (loseClip == null) loseClip = ProceduralSfx.Lose();
            if (clickClip == null) clickClip = ProceduralSfx.Click();
            if (weaponClip == null) weaponClip = ProceduralSfx.WeaponChange();
            if (spawnClip == null) spawnClip = ProceduralSfx.Spawn();
            if (healthDrainClip == null) healthDrainClip = ProceduralSfx.HealthDrain();
            if (coinClip == null) coinClip = ProceduralSfx.Coin();
            if (musicClip == null) musicClip = ProceduralSfx.Music();

            if (_drainSource != null)
                _drainSource.clip = healthDrainClip;
        }

        static float RandomPitch(float min, float max) => Random.Range(min, max);

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
            _disposables.Dispose();
        }
    }
}
