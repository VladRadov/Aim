using Aim.Config;
using Aim.Models;
using Aim.Presenters;
using Aim.Services;
using Aim.Views;
using UnityEngine;

namespace Aim
{
    public sealed class Bootstrap : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] AimTrainerConfig config;
        [SerializeField] GameObject weaponPrefab;
        [SerializeField] WeaponShopCatalog weaponShopCatalog;

        [Header("Player Views")]
        [SerializeField] InputView inputView;
        [SerializeField] CameraLookView cameraLookView;
        [SerializeField] WeaponView weaponView;
        [SerializeField] Camera shootCamera;

        [Header("UI Views")]
        [SerializeField] CrosshairView crosshairView;
        [SerializeField] SessionHudView sessionHudView;
        [SerializeField] CoinsHudView coinsHudView;
        [SerializeField] SettingsView settingsView;
        [SerializeField] ShopView shopView;
        [SerializeField] LevelWinView levelWinView;
        [SerializeField] LevelTipView levelTipView;
        [SerializeField] MainMenuView mainMenuView;

        [Header("World")]
        [SerializeField] Transform projectilesRoot;
        [SerializeField] Transform targetsRoot;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;

        SessionModel _sessionModel;
        SettingsModel _settingsModel;
        CoinsModel _coinsModel;
        LevelWinModel _levelWinModel;
        MainMenuModel _mainMenuModel;
        ShopModel _shopModel;

        GameplayService _gameplayService;
        HudService _hudService;
        SettingsMenuService _settingsMenuService;
        ShopMenuService _shopMenuService;
        LevelService _levelService;
        MainMenuPresenter _mainMenuPresenter;
        GameModeCatalog _modeCatalog;

        void Awake()
        {
            ResolveSceneDependencies();
            InitializeServices();
        }

        void OnDestroy()
        {
            _mainMenuPresenter?.Dispose();
            _levelService?.Dispose();
            _shopMenuService?.Dispose();
            _settingsMenuService?.Dispose();
            _hudService?.Dispose();
            _gameplayService?.Dispose();

            _shopModel?.Dispose();
            _mainMenuModel?.Dispose();
            _levelWinModel?.Dispose();
            _coinsModel?.Dispose();
            _settingsModel?.Dispose();
            _sessionModel?.Dispose();
        }

        void InitializeServices()
        {
            _sessionModel = new SessionModel();
            _settingsModel = new SettingsModel();
            _coinsModel = new CoinsModel();
            _levelWinModel = new LevelWinModel();
            _mainMenuModel = new MainMenuModel();
            _shopModel = new ShopModel(weaponShopCatalog);

            var startingWeapon = ResolveStartingWeaponPrefab();

            _gameplayService = new GameplayService(
                config,
                _sessionModel,
                _settingsModel,
                inputView,
                cameraLookView,
                weaponView,
                crosshairView,
                shootCamera,
                projectilesRoot,
                startingWeapon);

            _hudService = new HudService(
                _sessionModel,
                _coinsModel,
                config,
                sessionHudView,
                coinsHudView);

            _settingsMenuService = new SettingsMenuService(
                _settingsModel,
                _levelWinModel,
                settingsView,
                inputView,
                musicSource,
                sfxSource,
                () => _shopModel.IsOpen.Value || _mainMenuModel.IsOpen.Value);

            _shopMenuService = new ShopMenuService(
                _shopModel,
                _coinsModel,
                _settingsModel,
                _levelWinModel,
                shopView,
                inputView,
                EquipWeaponById,
                () => _mainMenuModel.IsOpen.Value);

            var statsTracker = new LevelRunStatsTracker(
                _gameplayService.ShootModel,
                _sessionModel,
                _coinsModel);

            _levelService = new LevelService(
                _sessionModel,
                _coinsModel,
                _settingsModel,
                _levelWinModel,
                _mainMenuModel,
                statsTracker,
                config,
                targetsRoot,
                shootCamera);
            _levelService.BindWinUi(levelWinView, inputView);
            _levelService.BindTipUi(levelTipView);
            _levelService.SetCampaignPlaylist(config.Levels);

            _modeCatalog = new GameModeCatalog(config.Levels);

            if (mainMenuView != null)
            {
                _mainMenuPresenter = new MainMenuPresenter(
                    _mainMenuModel,
                    _levelService,
                    _modeCatalog,
                    _settingsModel,
                    _levelWinModel,
                    mainMenuView,
                    inputView);
                _mainMenuPresenter.Initialize();
            }
            else
            {
                Debug.LogError("Bootstrap: MainMenuView is missing. Run Aim → Rebuild Main Menu UI.");
            }
        }

        void Start()
        {
            _mainMenuModel?.ShowRoot();
            if (inputView != null)
                inputView.SetUiCapture(true);
        }

        GameObject ResolveStartingWeaponPrefab()
        {
            var equipped = _shopModel?.GetEquippedEntry();
            if (equipped?.Prefab != null)
                return equipped.Prefab;
            return weaponPrefab;
        }

        void EquipWeaponById(string weaponId)
        {
            var entry = _shopModel?.FindById(weaponId);
            if (entry?.Prefab == null || _gameplayService == null)
                return;

            _gameplayService.EquipWeaponPrefab(entry.Prefab);
            weaponPrefab = entry.Prefab;
        }

        void ResolveSceneDependencies()
        {
            if (shootCamera == null)
                shootCamera = Camera.main;

            if (weaponShopCatalog == null)
                weaponShopCatalog = Resources.Load<WeaponShopCatalog>("WeaponShopCatalog");

            if (inputView == null)
                inputView = FindAnyObjectByType<InputView>();

            if (cameraLookView == null)
                cameraLookView = FindAnyObjectByType<CameraLookView>();

            if (weaponView == null)
                weaponView = FindAnyObjectByType<WeaponView>();

            if (crosshairView == null)
                crosshairView = FindAnyObjectByType<CrosshairView>();

            if (sessionHudView == null)
                sessionHudView = FindAnyObjectByType<SessionHudView>();

            if (coinsHudView == null)
                coinsHudView = FindAnyObjectByType<CoinsHudView>();

            if (settingsView == null)
                settingsView = FindAnyObjectByType<SettingsView>();

            if (shopView == null)
                shopView = FindAnyObjectByType<ShopView>();

            if (levelWinView == null)
                levelWinView = FindAnyObjectByType<LevelWinView>();

            if (levelTipView == null)
                levelTipView = FindAnyObjectByType<LevelTipView>();

            if (mainMenuView == null)
                mainMenuView = FindAnyObjectByType<MainMenuView>();

            if (projectilesRoot == null)
                projectilesRoot = new GameObject("Projectiles").transform;

            if (targetsRoot == null)
                targetsRoot = new GameObject("LevelTargets").transform;

            if (weaponShopCatalog == null)
                Debug.LogError("Bootstrap: WeaponShopCatalog is missing. Place it at Resources/WeaponShopCatalog or assign on Bootstrap.");

            if (shopView == null)
                Debug.LogError("Bootstrap: ShopView is missing. Run Aim → Rebuild Shop UI.");

            EnsureAudioSources();
        }

        void EnsureAudioSources()
        {
            if (musicSource != null && sfxSource != null)
                return;

            var audioRoot = GameObject.Find("GameAudio") ?? new GameObject("GameAudio");
            if (musicSource == null)
            {
                musicSource = audioRoot.GetComponent<AudioSource>();
                if (musicSource == null)
                    musicSource = audioRoot.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f;
            }

            if (sfxSource == null)
            {
                var sfxGo = audioRoot.transform.Find("SfxSource");
                if (sfxGo == null)
                {
                    var created = new GameObject("SfxSource");
                    created.transform.SetParent(audioRoot.transform, false);
                    sfxSource = created.AddComponent<AudioSource>();
                }
                else
                {
                    sfxSource = sfxGo.GetComponent<AudioSource>() ?? sfxGo.gameObject.AddComponent<AudioSource>();
                }

                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
            }
        }
    }
}
