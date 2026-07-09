using Aim.Config;
using Aim.Controllers;
using Aim.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aim.Editor
{
    public static class AimTrainerSceneSetup
    {
        const string ConfigAssetPath = "Assets/Aim/Config/AimTrainerConfig.asset";
        const string TargetPrefabPath = "Assets/Aim/Prefabs/TrainingTarget.prefab";
        const string BulletPrefabPath = "Assets/Aim/Prefabs/Bullet.prefab";
        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Aim/Setup Game Scene")]
        public static void SetupGameScene()
        {
            EnsureTargetLayer();
            var config = EnsureConfig();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            var bulletPrefab = EnsureBulletPrefab();
            AssignBulletPrefabToConfig(config, bulletPrefab);

            var playerRoot = new GameObject("Player");
            playerRoot.transform.position = new Vector3(0f, 1.6f, -3f);

            var yawPivot = new GameObject("YawPivot").transform;
            yawPivot.SetParent(playerRoot.transform, false);

            var pitchPivot = new GameObject("PitchPivot").transform;
            pitchPivot.SetParent(yawPivot, false);

            var cameraGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(pitchPivot, false);
            cameraGo.transform.localPosition = Vector3.zero;
            cameraGo.transform.localRotation = Quaternion.identity;

            if (!cameraGo.TryGetComponent<Camera>(out _))
                cameraGo.AddComponent<Camera>();

            if (!cameraGo.TryGetComponent<AudioListener>(out _))
                cameraGo.AddComponent<AudioListener>();

            var cameraLookView = playerRoot.AddComponent<CameraLookView>();
            SetPrivateField(cameraLookView, "yawPivot", yawPivot);
            SetPrivateField(cameraLookView, "pitchPivot", pitchPivot);

            var weaponGo = new GameObject("WeaponView");
            weaponGo.transform.SetParent(cameraGo.transform, false);
            var weaponView = weaponGo.AddComponent<WeaponView>();

            var weaponMount = new GameObject("WeaponMount").transform;
            weaponMount.SetParent(weaponGo.transform, false);
            weaponMount.localPosition = config.WeaponMountLocalPosition;
            SetPrivateField(weaponView, "weaponMount", weaponMount);

            var inputView = playerRoot.AddComponent<InputView>();
            SetPrivateField(inputView, "inputActions", inputActions);

            var uiRoot = EnsureUiRoot(out var crosshairDot);
            var crosshairView = uiRoot.AddComponent<CrosshairView>();
            SetPrivateField(crosshairView, "dotImage", crosshairDot);

            var projectilesRoot = new GameObject("Projectiles").transform;
            projectilesRoot.SetParent(playerRoot.transform, false);

            var controller = playerRoot.AddComponent<AimTrainerController>();
            SetPrivateField(controller, "config", config);
            SetPrivateField(controller, "inputView", inputView);
            SetPrivateField(controller, "cameraLookView", cameraLookView);
            SetPrivateField(controller, "weaponView", weaponView);
            SetPrivateField(controller, "crosshairView", crosshairView);
            SetPrivateField(controller, "shootCamera", cameraGo.GetComponent<Camera>());
            SetPrivateField(controller, "projectilesRoot", projectilesRoot);

            EnsureMainThreadDispatcher();
            CreateTrainingArena(config);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = playerRoot;

            Debug.Log("Aim trainer scene setup complete. Assign your weapon prefab on AimTrainerController if needed.");
        }

        static void EnsureTargetLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            var targetLayerIndex = -1;

            for (var i = 8; i < 32; i++)
            {
                var layerName = layers.GetArrayElementAtIndex(i).stringValue;
                if (layerName == GameLayers.TargetLayerName)
                {
                    targetLayerIndex = i;
                    break;
                }

                if (string.IsNullOrEmpty(layerName) && targetLayerIndex < 0)
                    targetLayerIndex = i;
            }

            if (targetLayerIndex >= 0 &&
                layers.GetArrayElementAtIndex(targetLayerIndex).stringValue != GameLayers.TargetLayerName)
            {
                layers.GetArrayElementAtIndex(targetLayerIndex).stringValue = GameLayers.TargetLayerName;
                tagManager.ApplyModifiedProperties();
            }
        }

        static AimTrainerConfig EnsureConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AimTrainerConfig>(ConfigAssetPath);
            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Aim/Config"))
                AssetDatabase.CreateFolder("Assets/Aim", "Config");

            var config = ScriptableObject.CreateInstance<AimTrainerConfig>();
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        static GameObject EnsureUiRoot(out Image crosshairDot)
        {
            var canvasGo = GameObject.Find("UI") ?? new GameObject("UI");
            if (!canvasGo.TryGetComponent<Canvas>(out var canvas))
            {
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            var crosshairGo = GameObject.Find("Crosshair") ?? new GameObject("Crosshair", typeof(RectTransform));
            crosshairGo.transform.SetParent(canvasGo.transform, false);

            var rectTransform = crosshairGo.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(8f, 8f);
            rectTransform.anchoredPosition = Vector2.zero;

            if (!crosshairGo.TryGetComponent<Image>(out crosshairDot))
                crosshairDot = crosshairGo.AddComponent<Image>();

            crosshairDot.raycastTarget = false;
            crosshairDot.color = Color.white;
            crosshairDot.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            return crosshairGo;
        }

        static void EnsureMainThreadDispatcher()
        {
            if (Object.FindAnyObjectByType<UniRx.MainThreadDispatcher>() != null)
                return;

            var dispatcherGo = new GameObject("MainThreadDispatcher");
            dispatcherGo.AddComponent<UniRx.MainThreadDispatcher>();
        }

        static void CreateTrainingArena(AimTrainerConfig config)
        {
            var arenaRoot = GameObject.Find("TrainingArena") ?? new GameObject("TrainingArena");

            var wall = GameObject.Find("TargetWall") ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "TargetWall";
            wall.transform.SetParent(arenaRoot.transform, false);
            wall.transform.position = new Vector3(0f, 2.5f, 8f);
            wall.transform.localScale = new Vector3(12f, 5f, 0.5f);

            var floor = GameObject.Find("Floor") ?? GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(arenaRoot.transform, false);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            var targetPrefab = EnsureTargetPrefab();
            var spawnPositions = new[]
            {
                new Vector3(-3f, 2f, 7.7f),
                new Vector3(-1f, 3f, 7.7f),
                new Vector3(1.5f, 1.5f, 7.7f),
                new Vector3(3f, 2.8f, 7.7f),
                new Vector3(0f, 2f, 7.7f)
            };

            var targetsRoot = GameObject.Find("Targets") ?? new GameObject("Targets");
            targetsRoot.transform.SetParent(arenaRoot.transform, false);

            for (var i = 0; i < spawnPositions.Length; i++)
            {
                var targetName = $"TrainingTarget_{i + 1}";
                if (GameObject.Find(targetName) != null)
                    continue;

                var target = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab, targetsRoot.transform);
                target.name = targetName;
                target.transform.position = spawnPositions[i];
            }

            var serializedConfig = new SerializedObject(config);
            var layerMaskProperty = serializedConfig.FindProperty("targetLayerMask");
            layerMaskProperty.intValue = GameLayers.TargetMask.value;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        }

        static TargetView EnsureTargetPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
            if (existing != null)
                return existing.GetComponent<TargetView>();

            if (!AssetDatabase.IsValidFolder("Assets/Aim/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Aim", "Prefabs");

            var targetGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetGo.name = "TrainingTarget";
            targetGo.transform.localScale = Vector3.one * 0.6f;
            targetGo.layer = LayerMask.NameToLayer(GameLayers.TargetLayerName);

            var collider = targetGo.GetComponent<SphereCollider>();
            collider.isTrigger = false;

            var targetView = targetGo.AddComponent<TargetView>();
            SetPrivateField(targetView, "targetRenderer", targetGo.GetComponent<Renderer>());

            var prefab = PrefabUtility.SaveAsPrefabAsset(targetGo, TargetPrefabPath);
            Object.DestroyImmediate(targetGo);
            return prefab.GetComponent<TargetView>();
        }

        static ProjectileView EnsureBulletPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
            if (existing != null)
                return existing.GetComponent<ProjectileView>();

            if (!AssetDatabase.IsValidFolder("Assets/Aim/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Aim", "Prefabs");

            var bulletGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletGo.name = "Bullet";
            bulletGo.transform.localScale = Vector3.one * 0.12f;
            Object.DestroyImmediate(bulletGo.GetComponent<Collider>());
            bulletGo.AddComponent<ProjectileView>();

            var renderer = bulletGo.GetComponent<Renderer>();
            renderer.sharedMaterial.color = new Color(1f, 0.85f, 0.2f, 1f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(bulletGo, BulletPrefabPath);
            Object.DestroyImmediate(bulletGo);
            return prefab.GetComponent<ProjectileView>();
        }

        static void AssignBulletPrefabToConfig(AimTrainerConfig config, ProjectileView bulletPrefab)
        {
            var serializedConfig = new SerializedObject(config);
            var bulletPrefabProperty = serializedConfig.FindProperty("bulletPrefab");
            if (bulletPrefabProperty != null && bulletPrefabProperty.objectReferenceValue == null)
            {
                bulletPrefabProperty.objectReferenceValue = bulletPrefab;
                serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SetPrivateField(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
