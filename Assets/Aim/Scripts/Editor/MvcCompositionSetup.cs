using Aim;
using Aim.Config;
using Aim.Installers;
using Aim.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Aim.Editor
{
    public static class MvcCompositionSetup
    {
        const string BootstrapRootName = "[1.BOOTSTRAP]";
        const string ServicesRootName = "[2. SERVICES]";

        [MenuItem("Aim/Setup MVC Composition")]
        public static void Setup()
        {
            var bootstrapRoot = FindOrCreateRoot(BootstrapRootName);
            var servicesRoot = FindOrCreateRoot(ServicesRootName);

            var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
            if (bootstrap == null)
            {
                var go = new GameObject("Bootstrap");
                go.transform.SetParent(bootstrapRoot.transform, false);
                bootstrap = go.AddComponent<Bootstrap>();
            }
            else if (bootstrap.transform.parent != bootstrapRoot.transform)
            {
                bootstrap.transform.SetParent(bootstrapRoot.transform, true);
            }

            EnsureService<GameplayService>(servicesRoot, "GameplayService");
            EnsureService<HudService>(servicesRoot, "HudService");
            EnsureService<SettingsMenuService>(servicesRoot, "SettingsMenuService");
            EnsureService<ShopMenuService>(servicesRoot, "ShopMenuService");
            EnsureService<LevelService>(servicesRoot, "LevelService");
            EnsureService<MainMenuService>(servicesRoot, "MainMenuService");

            var audioGo = EnsureChild(servicesRoot, "AudioService");
            if (audioGo.GetComponent<AudioSettingsService>() == null)
                audioGo.AddComponent<AudioSettingsService>();
            EnsureAudioSources(audioGo, out var music, out var sfx);

            var installer = bootstrapRoot.GetComponent<GameInstaller>() ??
                            bootstrapRoot.AddComponent<GameInstaller>();
            var context = bootstrapRoot.GetComponent<SceneContext>() ??
                          bootstrapRoot.AddComponent<SceneContext>();
            context.Installers = new MonoInstaller[] { installer };

            CopyLegacyBootstrapRefs(bootstrap, installer, music, sfx);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = bootstrapRoot;
            Debug.Log("MVC composition ready: SceneContext + GameInstaller on [1.BOOTSTRAP], entity services on [2. SERVICES].");
        }

        static GameObject FindOrCreateRoot(string name)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }

            var created = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, "Create " + name);
            return created;
        }

        static T EnsureService<T>(GameObject parent, string name) where T : Component
        {
            var existing = parent.GetComponentInChildren<T>(true);
            if (existing != null)
            {
                existing.transform.SetParent(parent.transform, true);
                return existing;
            }

            var go = EnsureChild(parent, name);
            return go.AddComponent<T>();
        }

        static GameObject EnsureChild(GameObject parent, string name)
        {
            var child = parent.transform.Find(name);
            if (child != null)
                return child.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static void EnsureAudioSources(GameObject audioRoot, out AudioSource music, out AudioSource sfx)
        {
            music = audioRoot.GetComponent<AudioSource>();
            if (music == null)
                music = audioRoot.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;

            var sfxTransform = audioRoot.transform.Find("SfxSource");
            if (sfxTransform == null)
            {
                var sfxGo = new GameObject("SfxSource");
                sfxGo.transform.SetParent(audioRoot.transform, false);
                sfx = sfxGo.AddComponent<AudioSource>();
            }
            else
            {
                sfx = sfxTransform.GetComponent<AudioSource>() ?? sfxTransform.gameObject.AddComponent<AudioSource>();
            }

            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
        }

        static void CopyLegacyBootstrapRefs(
            Bootstrap bootstrap,
            GameInstaller installer,
            AudioSource music,
            AudioSource sfx)
        {
            var source = new SerializedObject(bootstrap);
            var dest = new SerializedObject(installer);

            CopyRef(source, dest, "config");
            CopyRef(source, dest, "weaponShopCatalog");
            CopyRef(source, dest, "weaponPrefab", "defaultWeaponPrefab");
            CopyRef(source, dest, "shootCamera");
            CopyRef(source, dest, "projectilesRoot");
            CopyRef(source, dest, "targetsRoot");
            CopyRef(source, dest, "musicSource");
            CopyRef(source, dest, "sfxSource");

            var musicProp = dest.FindProperty("musicSource");
            if (musicProp != null && musicProp.objectReferenceValue == null)
                musicProp.objectReferenceValue = music;

            var sfxProp = dest.FindProperty("sfxSource");
            if (sfxProp != null && sfxProp.objectReferenceValue == null)
                sfxProp.objectReferenceValue = sfx;

            dest.ApplyModifiedPropertiesWithoutUndo();

            if (dest.FindProperty("config") is { objectReferenceValue: null } configProp)
            {
                configProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AimTrainerConfig>(
                    "Assets/Aim/Config/AimTrainerConfig.asset");
            }

            if (dest.FindProperty("weaponShopCatalog") is { objectReferenceValue: null } catalogProp)
            {
                catalogProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponShopCatalog>(
                    "Assets/Aim/Resources/WeaponShopCatalog.asset");
            }

            if (dest.FindProperty("shootCamera") is { objectReferenceValue: null } cameraProp)
                cameraProp.objectReferenceValue = Camera.main;

            dest.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CopyRef(SerializedObject source, SerializedObject dest, string srcName, string destName = null)
        {
            destName ??= srcName;
            var from = source.FindProperty(srcName);
            var to = dest.FindProperty(destName);
            if (from == null || to == null)
                return;
            if (to.objectReferenceValue == null)
                to.objectReferenceValue = from.objectReferenceValue;
        }
    }
}
