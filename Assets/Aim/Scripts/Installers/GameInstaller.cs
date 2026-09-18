using Aim.Config;
using Aim.Models;
using Aim.Services;
using Aim.Views;
using UnityEngine;
using Zenject;

namespace Aim.Installers
{
    public sealed class GameInstaller : MonoInstaller
    {
        [Header("Config")]
        [SerializeField] AimTrainerConfig config;
        [SerializeField] WeaponShopCatalog weaponShopCatalog;
        [SerializeField] GameObject defaultWeaponPrefab;

        [Header("World")]
        [SerializeField] Camera shootCamera;
        [SerializeField] Transform projectilesRoot;
        [SerializeField] Transform targetsRoot;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;

        public override void InstallBindings()
        {
            BindConfig();
            BindModels();
            BindSceneViews();
            BindWorld();
            BindServices();
            BindBootstrap();
        }

        void BindConfig()
        {
            Container.BindInstance(config).AsSingle();
            Container.Bind<WeaponShopCatalog>().FromMethod(_ => weaponShopCatalog).AsSingle();
            Container.Bind<GameObject>().WithId("DefaultWeapon").FromMethod(_ => defaultWeaponPrefab);
            Container.Bind<GameModeCatalog>()
                .FromMethod(ctx => new GameModeCatalog(ctx.Container.Resolve<AimTrainerConfig>().Levels))
                .AsSingle();
        }

        void BindModels()
        {
            Container.Bind<SessionModel>().AsSingle();
            Container.Bind<SettingsModel>().AsSingle();
            Container.Bind<CoinsModel>().AsSingle();
            Container.Bind<LevelWinModel>().AsSingle();
            Container.Bind<MainMenuModel>().AsSingle();
            Container.Bind<ShopModel>().AsSingle();
            Container.Bind<ShootModel>().AsSingle();
            Container.Bind<RecoilModel>().AsSingle();
            Container.Bind<AimModel>()
                .FromMethod(ctx =>
                {
                    var cfg = ctx.Container.Resolve<AimTrainerConfig>();
                    return new AimModel(cfg.MinPitch, cfg.MaxPitch);
                })
                .AsSingle();
            Container.Bind<LevelRunStatsTracker>().AsSingle();
        }

        void BindSceneViews()
        {
            Container.Bind<InputView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<CameraLookView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<WeaponView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<CrosshairView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<SessionHudView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<CoinsHudView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<SettingsView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ShopView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LevelWinView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LevelTipView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<MainMenuView>().FromComponentInHierarchy().AsSingle();
        }

        void BindWorld()
        {
            var camera = shootCamera != null ? shootCamera : Camera.main;
            Container.Bind<Camera>().FromInstance(camera).AsSingle();

            var projectiles = projectilesRoot != null
                ? projectilesRoot
                : new GameObject("Projectiles").transform;
            var targets = targetsRoot != null
                ? targetsRoot
                : new GameObject("LevelTargets").transform;

            Container.Bind<Transform>().WithId("ProjectilesRoot").FromInstance(projectiles);
            Container.Bind<Transform>().WithId("TargetsRoot").FromInstance(targets);
            Container.Bind<AudioSource>().WithId("Music").FromMethod(_ => musicSource);
            Container.Bind<AudioSource>().WithId("Sfx").FromMethod(_ => sfxSource);
        }

        void BindServices()
        {
            Container.Bind<AudioSettingsService>().FromComponentInHierarchy().AsSingle();
            Container.Bind<GameplayService>().FromComponentInHierarchy().AsSingle();
            Container.Bind<HudService>().FromComponentInHierarchy().AsSingle();
            Container.Bind<SettingsMenuService>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ShopMenuService>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LevelService>().FromComponentInHierarchy().AsSingle();
            Container.Bind<MainMenuService>().FromComponentInHierarchy().AsSingle();
        }

        void BindBootstrap()
        {
            Container.BindInterfacesAndSelfTo<Bootstrap>().FromComponentInHierarchy().AsSingle();
        }
    }
}
