using Aim;
using Aim.Config;
using Aim.Installers;
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

            var sessionHud = EnsureSessionHud(uiRoot.transform.parent as RectTransform ?? uiRoot.GetComponentInParent<Canvas>().transform);
            var coinsHud = EnsureCoinsHud(uiRoot.transform.parent as RectTransform ?? uiRoot.GetComponentInParent<Canvas>().transform);

            var projectilesRoot = new GameObject("Projectiles").transform;
            var targetsRoot = new GameObject("LevelTargets").transform;

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<Bootstrap>();
            SetPrivateField(bootstrap, "config", config);
            SetPrivateField(bootstrap, "inputView", inputView);
            SetPrivateField(bootstrap, "cameraLookView", cameraLookView);
            SetPrivateField(bootstrap, "weaponView", weaponView);
            SetPrivateField(bootstrap, "crosshairView", crosshairView);
            SetPrivateField(bootstrap, "sessionHudView", sessionHud);
            SetPrivateField(bootstrap, "coinsHudView", coinsHud);
            SetPrivateField(bootstrap, "shootCamera", cameraGo.GetComponent<Camera>());
            SetPrivateField(bootstrap, "projectilesRoot", projectilesRoot);
            SetPrivateField(bootstrap, "targetsRoot", targetsRoot);

            EnsureMainThreadDispatcher();
            CreateTrainingArena(config);
            MvcCompositionSetup.Setup();

            var installer = Object.FindAnyObjectByType<GameInstaller>();
            if (installer != null)
            {
                SetPrivateField(installer, "config", config);
                SetPrivateField(installer, "shootCamera", cameraGo.GetComponent<Camera>());
                SetPrivateField(installer, "projectilesRoot", projectilesRoot);
                SetPrivateField(installer, "targetsRoot", targetsRoot);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = bootstrapGo;

            Debug.Log("Aim trainer scene setup complete. Assign your weapon prefab on Bootstrap if needed.");
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

        static SessionHudView EnsureSessionHud(Transform canvasTransform)
        {
            var existing = Object.FindAnyObjectByType<SessionHudView>();
            if (existing != null)
            {
                RebuildSessionCounters(existing);
                return existing;
            }

            var hudGo = new GameObject("SessionHud", typeof(RectTransform));
            hudGo.transform.SetParent(canvasTransform, false);

            var rootRect = hudGo.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var state = CreateHudLabel(hudGo.transform, "StateText", new Vector2(0.5f, 0.5f), Vector2.zero, 36, TextAnchor.MiddleCenter);
            state.color = new Color(1f, 0.85f, 0.2f, 1f);

            var hitsText = CreateStatCounter(
                hudGo.transform,
                "HitsCounter",
                new Vector2(-130f, -18f),
                "Assets/Aim/Sprites/hits_icon.png",
                new Color(0.94f, 0.94f, 0.96f, 0.95f),
                out var hitsIcon,
                out var hitsGroup);

            var ammoText = CreateStatCounter(
                hudGo.transform,
                "AmmoCounter",
                new Vector2(130f, -18f),
                "Assets/Aim/Sprites/ammo_icon.png",
                new Color(0.94f, 0.94f, 0.96f, 0.95f),
                out var ammoIcon,
                out var ammoGroup);

            var hud = hudGo.AddComponent<SessionHudView>();
            SetPrivateField(hud, "hitsText", hitsText);
            SetPrivateField(hud, "ammoText", ammoText);
            SetPrivateField(hud, "stateText", state);
            SetPrivateField(hud, "hitsIcon", hitsIcon);
            SetPrivateField(hud, "ammoIcon", ammoIcon);
            SetPrivateField(hud, "hitsGroup", hitsGroup);
            SetPrivateField(hud, "ammoGroup", ammoGroup);
            return hud;
        }

        [MenuItem("Aim/Rebuild Session HUD")]
        public static void RebuildSessionHudMenu()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found. Run Aim/Setup Game Scene first.");
                return;
            }

            var hud = Object.FindAnyObjectByType<SessionHudView>();
            if (hud == null)
                hud = EnsureSessionHud(canvas.transform);
            else
                RebuildSessionCounters(hud);

            var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
            if (bootstrap != null)
                SetPrivateField(bootstrap, "sessionHudView", hud);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = hud.gameObject;
            Debug.Log("Session HUD rebuilt (hits + ammo with circle icons) and linked to Bootstrap.");
        }

        [MenuItem("Aim/Rebuild Ammo HUD")]
        public static void RebuildAmmoHudMenu() => RebuildSessionHudMenu();

        [MenuItem("Aim/Rebuild Coins HUD")]
        public static void RebuildCoinsHudMenu()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found. Run Aim/Setup Game Scene first.");
                return;
            }

            var coinsHud = EnsureCoinsHud(canvas.transform);
            var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
            if (bootstrap != null)
                SetPrivateField(bootstrap, "coinsHudView", coinsHud);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = coinsHud.gameObject;
            Debug.Log("Coins HUD rebuilt (top-left) and linked to Bootstrap.");
        }

        static CoinsHudView EnsureCoinsHud(Transform canvasTransform)
        {
            var existing = Object.FindAnyObjectByType<CoinsHudView>();
            if (existing != null)
            {
                RebuildCoinsCounter(existing);
                return existing;
            }

            var coinsGo = new GameObject("CoinsHud", typeof(RectTransform));
            coinsGo.transform.SetParent(canvasTransform, false);

            var rootRect = coinsGo.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var coinsText = CreateCornerCoinCounter(
                coinsGo.transform,
                "CoinsCounter",
                out var coinsIcon);

            var coinsHud = coinsGo.AddComponent<CoinsHudView>();
            SetPrivateField(coinsHud, "coinsText", coinsText);
            SetPrivateField(coinsHud, "coinsIcon", coinsIcon);
            return coinsHud;
        }

        static void RebuildCoinsCounter(CoinsHudView coinsHud)
        {
            DestroyChildIfExists(coinsHud.transform, "CoinsCounter");

            var coinsText = CreateCornerCoinCounter(
                coinsHud.transform,
                "CoinsCounter",
                out var coinsIcon);

            SetPrivateField(coinsHud, "coinsText", coinsText);
            SetPrivateField(coinsHud, "coinsIcon", coinsIcon);
        }

        static Text CreateCornerCoinCounter(Transform parent, string name, out Image iconImage)
        {
            var counterGo = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            counterGo.transform.SetParent(parent, false);
            var group = counterGo.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var counterRect = counterGo.GetComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0f, 1f);
            counterRect.anchorMax = new Vector2(0f, 1f);
            counterRect.pivot = new Vector2(0f, 1f);
            counterRect.anchoredPosition = new Vector2(18f, -18f);
            counterRect.sizeDelta = new Vector2(180f, 56f);

            var layout = counterGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 14, 4, 4);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var panel = counterGo.AddComponent<Image>();
            panel.color = new Color(0f, 0f, 0f, 0.4f);
            panel.raycastTarget = false;
            panel.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            panel.type = Image.Type.Sliced;

            var circleGo = new GameObject("IconCircle", typeof(RectTransform));
            circleGo.transform.SetParent(counterGo.transform, false);
            var circleRect = circleGo.GetComponent<RectTransform>();
            circleRect.sizeDelta = new Vector2(44f, 44f);

            var circleImage = circleGo.AddComponent<Image>();
            circleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            circleImage.color = new Color(0.94f, 0.94f, 0.96f, 0.95f);
            circleImage.raycastTarget = false;
            circleImage.preserveAspect = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(circleGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            iconImage = iconGo.AddComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Aim/Sprites/coin_icon.png");
            iconImage.color = Color.white;

            var textGo = new GameObject("ValueText", typeof(RectTransform));
            textGo.transform.SetParent(counterGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(100f, 40f);
            var valueText = textGo.AddComponent<Text>();
            valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valueText.fontSize = 26;
            valueText.fontStyle = FontStyle.Bold;
            valueText.alignment = TextAnchor.MiddleLeft;
            valueText.color = Color.white;
            valueText.raycastTarget = false;
            valueText.text = "0";
            LanguageYgTextUtility.AttachToTextComponent(valueText, valueText.text);

            return valueText;
        }

        static void RebuildSessionCounters(SessionHudView hud)
        {
            DestroyChildIfExists(hud.transform, "HitsText");
            DestroyChildIfExists(hud.transform, "HitsCounter");
            DestroyChildIfExists(hud.transform, "AmmoCounter");

            var hitsText = CreateStatCounter(
                hud.transform,
                "HitsCounter",
                new Vector2(-130f, -18f),
                "Assets/Aim/Sprites/hits_icon.png",
                new Color(0.94f, 0.94f, 0.96f, 0.95f),
                out var hitsIcon,
                out var hitsGroup);

            var ammoText = CreateStatCounter(
                hud.transform,
                "AmmoCounter",
                new Vector2(130f, -18f),
                "Assets/Aim/Sprites/ammo_icon.png",
                new Color(0.94f, 0.94f, 0.96f, 0.95f),
                out var ammoIcon,
                out var ammoGroup);

            SetPrivateField(hud, "hitsText", hitsText);
            SetPrivateField(hud, "ammoText", ammoText);
            SetPrivateField(hud, "hitsIcon", hitsIcon);
            SetPrivateField(hud, "ammoIcon", ammoIcon);
            SetPrivateField(hud, "hitsGroup", hitsGroup);
            SetPrivateField(hud, "ammoGroup", ammoGroup);
        }

        static void DestroyChildIfExists(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        static Text CreateStatCounter(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            string iconAssetPath,
            Color circleColor,
            out Image iconImage,
            out CanvasGroup group)
        {
            var counterGo = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            counterGo.transform.SetParent(parent, false);
            group = counterGo.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var counterRect = counterGo.GetComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0.5f, 1f);
            counterRect.anchorMax = new Vector2(0.5f, 1f);
            counterRect.pivot = new Vector2(0.5f, 1f);
            counterRect.anchoredPosition = anchoredPosition;
            counterRect.sizeDelta = new Vector2(200f, 56f);

            var layout = counterGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 14, 4, 4);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var panel = counterGo.AddComponent<Image>();
            panel.color = new Color(0f, 0f, 0f, 0.4f);
            panel.raycastTarget = false;
            panel.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            panel.type = Image.Type.Sliced;

            var circleGo = new GameObject("IconCircle", typeof(RectTransform));
            circleGo.transform.SetParent(counterGo.transform, false);
            var circleRect = circleGo.GetComponent<RectTransform>();
            circleRect.sizeDelta = new Vector2(44f, 44f);

            var circleImage = circleGo.AddComponent<Image>();
            circleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            circleImage.color = circleColor;
            circleImage.raycastTarget = false;
            circleImage.preserveAspect = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(circleGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            iconImage = iconGo.AddComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPath);
            iconImage.color = Color.white;

            var textGo = new GameObject("ValueText", typeof(RectTransform));
            textGo.transform.SetParent(counterGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(120f, 40f);
            var valueText = textGo.AddComponent<Text>();
            valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valueText.fontSize = 26;
            valueText.fontStyle = FontStyle.Bold;
            valueText.alignment = TextAnchor.MiddleLeft;
            valueText.color = Color.white;
            valueText.raycastTarget = false;
            valueText.text = "0 / 0";
            LanguageYgTextUtility.AttachToTextComponent(valueText, valueText.text);

            return valueText;
        }

        static Text CreateHudLabel(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, int fontSize, TextAnchor alignment)
        {
            var labelGo = new GameObject(name, typeof(RectTransform));
            labelGo.transform.SetParent(parent, false);
            var rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(360f, 40f);
            var text = labelGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            LanguageYgTextUtility.AttachToTextComponent(text, text.text);
            return text;
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
