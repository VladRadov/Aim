using Aim.Config;
using Aim.Models;
using Aim.Services;
using Aim.Views;
using UnityEditor;
using UnityEngine;

namespace Aim.Editor
{
    public static class LevelContentSetup
    {
        const string PrefabsFolder = "Assets/Aim/Prefabs";
        const string LevelsFolder = "Assets/Aim/Config/Levels";
        const string CharacterPrefabPath = PrefabsFolder + "/CharacterTarget.prefab";
        const string FlyingPrefabPath = PrefabsFolder + "/FlyingTarget.prefab";
        const string BouncingPrefabPath = PrefabsFolder + "/BouncingTarget.prefab";
        const string TrackingPrefabPath = PrefabsFolder + "/TrackingTarget.prefab";
        const string TrackingMoverPrefabPath = PrefabsFolder + "/TrackingMover.prefab";
        const string CustomPrefabPath = PrefabsFolder + "/CustomTarget.prefab";
        const string GalleryPrefabPath = PrefabsFolder + "/GalleryTarget.prefab";
        const string PeekPrefabPath = PrefabsFolder + "/PeekTarget.prefab";
        const string PriorityPrefabPath = PrefabsFolder + "/PriorityTarget.prefab";
        const string MultiHitPrefabPath = PrefabsFolder + "/MultiHitTarget.prefab";
        const string FlyingLevelPath = LevelsFolder + "/Level_FlyingObjects.asset";
        const string BouncingLevelPath = LevelsFolder + "/Level_BouncingBalls.asset";
        const string TrackingLevelPath = LevelsFolder + "/Level_TrackingBall.asset";
        const string TrackingMoversLevelPath = LevelsFolder + "/Level_TrackingMovers.asset";
        const string StaticLevelPath = LevelsFolder + "/Level_StaticBalls.asset";
        const string FlickLevelPath = LevelsFolder + "/Level_FlickTargets.asset";
        const string PeekLevelPath = LevelsFolder + "/Level_PeekTargets.asset";
        const string RailsLevelPath = LevelsFolder + "/Level_MovingRails.asset";
        const string PopupLevelPath = LevelsFolder + "/Level_PopupDucks.asset";
        const string PriorityLevelPath = LevelsFolder + "/Level_PriorityTargets.asset";
        const string PrecisionLevelPath = LevelsFolder + "/Level_PrecisionCircles.asset";
        const string DoubleTapLevelPath = LevelsFolder + "/Level_DoubleTap.asset";
        const string CharacterLevelPath = LevelsFolder + "/Level_CharacterHeadshot.asset";
        const string CustomLevelPath = LevelsFolder + "/Level_CustomHitZones.asset";
        const string GalleryLevelPath = LevelsFolder + "/Level_ShootingGallery.asset";

        [MenuItem("Aim/Create Level Prefabs And Sample Levels")]
        public static void CreateAll()
        {
            EnsureFolders();
            EnsureTargetLayer();

            var character = EnsureCharacterPrefab();
            var flying = EnsureFlyingPrefab();
            var bouncing = EnsureBouncingPrefab();
            var tracking = EnsureTrackingPrefab();
            var movers = EnsureTrackingMoverPrefab();
            var custom = EnsureCustomPrefab();
            var gallery = EnsureGalleryPrefab();
            var peek = EnsurePeekPrefab();
            var priority = EnsurePriorityPrefab();
            var multiHit = EnsureMultiHitPrefab();

            EnsureFlyingLevelAsset(FlyingLevelPath, "Flying Balls", flying, 10, 15);
            EnsureBouncingLevelAsset(BouncingLevelPath, "Bouncing Balls", bouncing, 12, 18);
            EnsureTrackingLevelAsset(TrackingLevelPath, "Tracking Ball", tracking, 8);
            EnsureTrackingMoversLevelAsset(TrackingMoversLevelPath, "Tracking Movers", movers, 12);
            EnsureStaticLevelAsset(StaticLevelPath, "Static Balls", flying, 12, 18);
            EnsureFlickLevelAsset(FlickLevelPath, "Flick Targets", flying, 15, 25);
            EnsurePeekLevelAsset(PeekLevelPath, "Peek Cover", peek, 12, 30);
            EnsureRailsLevelAsset(RailsLevelPath, "Moving Rails", flying, 12, 22);
            EnsurePopupLevelAsset(PopupLevelPath, "Pop-up Ducks", peek, 10, 24);
            EnsurePriorityLevelAsset(PriorityLevelPath, "Priority Targets", priority, 12, 28);
            EnsurePrecisionLevelAsset(PrecisionLevelPath, "Precision Circles", flying, 10, 16);
            EnsureDoubleTapLevelAsset(DoubleTapLevelPath, "Double Tap", multiHit, 8, 30);
            EnsureCharacterLevelAsset(CharacterLevelPath, "Headshots", character, 8, 12);
            EnsureCustomLevelAsset(CustomLevelPath, "Custom Zones", custom, 10, 14);
            EnsureGalleryLevelAsset(GalleryLevelPath, "Shooting Gallery", gallery, 12, 18);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Level prefabs and typed LevelDefinitions created in Assets/Aim/Prefabs and Assets/Aim/Config/Levels.");
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Aim/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Aim", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Aim/Config"))
                AssetDatabase.CreateFolder("Assets/Aim", "Config");
            if (!AssetDatabase.IsValidFolder(LevelsFolder))
                AssetDatabase.CreateFolder("Assets/Aim/Config", "Levels");
        }

        static void EnsureTargetLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (var i = 8; i < 32; i++)
            {
                var prop = layers.GetArrayElementAtIndex(i);
                if (prop.stringValue == GameLayers.TargetLayerName)
                    return;
                if (string.IsNullOrEmpty(prop.stringValue))
                {
                    prop.stringValue = GameLayers.TargetLayerName;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }
        }

        static CharacterTargetView EnsureCharacterPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            if (existing != null)
                return existing.GetComponent<CharacterTargetView>();

            var root = new GameObject("CharacterTarget");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(visual.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            SetLayerRecursive(body, GameLayers.Target);
            Object.DestroyImmediate(body.GetComponent<Rigidbody>());

            var bodyZone = body.AddComponent<HitZoneView>();
            SetPrivateField(bodyZone, "zoneKind", (int)HitZoneKind.Body);
            SetPrivateField(bodyZone, "countsAsScore", false);
            SetPrivateField(bodyZone, "zoneRenderer", body.GetComponent<Renderer>());
            SetPrivateField(bodyZone, "normalColor", new Color(0.25f, 0.45f, 0.9f, 1f));

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0f, 2f, 0f);
            head.transform.localScale = Vector3.one * 0.45f;
            SetLayerRecursive(head, GameLayers.Target);
            Object.DestroyImmediate(head.GetComponent<Rigidbody>());

            var headZone = head.AddComponent<HitZoneView>();
            SetPrivateField(headZone, "zoneKind", (int)HitZoneKind.Head);
            SetPrivateField(headZone, "countsAsScore", true);
            SetPrivateField(headZone, "zoneRenderer", head.GetComponent<Renderer>());
            SetPrivateField(headZone, "normalColor", new Color(0.95f, 0.35f, 0.35f, 1f));

            var character = root.AddComponent<CharacterTargetView>();
            SetPrivateField(character, "headZone", headZone);
            SetPrivateField(character, "bodyZone", bodyZone);
            SetPrivateField(character, "visualRoot", visual.transform);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CharacterPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<CharacterTargetView>();
        }

        static FlyingTargetView EnsureFlyingPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(FlyingPrefabPath);
            if (existing != null)
                return existing.GetComponent<FlyingTargetView>();

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "FlyingTarget";
            ball.transform.localScale = Vector3.one * 0.55f;
            SetLayerRecursive(ball, GameLayers.Target);
            Object.DestroyImmediate(ball.GetComponent<Rigidbody>());

            var view = ball.AddComponent<FlyingTargetView>();
            SetPrivateField(view, "targetRenderer", ball.GetComponent<Renderer>());
            SetPrivateField(view, "normalColor", new Color(0.2f, 0.75f, 1f, 1f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(ball, FlyingPrefabPath);
            Object.DestroyImmediate(ball);
            return prefab.GetComponent<FlyingTargetView>();
        }

        static BouncingTargetView EnsureBouncingPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BouncingPrefabPath);
            if (existing != null)
                return existing.GetComponent<BouncingTargetView>();

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "BouncingTarget";
            ball.transform.localScale = Vector3.one * 0.45f;
            SetLayerRecursive(ball, GameLayers.Target);
            Object.DestroyImmediate(ball.GetComponent<Rigidbody>());

            var view = ball.AddComponent<BouncingTargetView>();
            SetPrivateField(view, "targetRenderer", ball.GetComponent<Renderer>());
            SetPrivateField(view, "normalColor", new Color(0.35f, 0.9f, 1f, 1f));
            SetPrivateField(view, "hitColor", new Color(1f, 1f, 1f, 1f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(ball, BouncingPrefabPath);
            Object.DestroyImmediate(ball);
            return prefab.GetComponent<BouncingTargetView>();
        }

        static TrackingTargetView EnsureTrackingPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(TrackingPrefabPath);
            if (existing != null)
                return existing.GetComponent<TrackingTargetView>();

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "TrackingTarget";
            ball.transform.localScale = Vector3.one * 1.5f;
            SetLayerRecursive(ball, GameLayers.Target);
            Object.DestroyImmediate(ball.GetComponent<Rigidbody>());

            var view = ball.AddComponent<TrackingTargetView>();
            SetPrivateField(view, "targetRenderer", ball.GetComponent<Renderer>());
            SetPrivateField(view, "normalColor", new Color(0.2f, 0.95f, 0.75f, 1f));
            SetPrivateField(view, "trackedColor", new Color(1f, 0.45f, 0.55f, 1f));
            SetPrivateField(view, "healthBarWorldWidth", 2.85f);
            SetPrivateField(view, "healthBarWorldHeight", 0.27f);
            SetPrivateField(view, "healthBarHeightOffset", 1.65f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(ball, TrackingPrefabPath);
            Object.DestroyImmediate(ball);
            return prefab.GetComponent<TrackingTargetView>();
        }

        static TrackingMoverView EnsureTrackingMoverPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(TrackingMoverPrefabPath);
            if (existing != null)
                return existing.GetComponent<TrackingMoverView>();

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "TrackingMover";
            ball.transform.localScale = Vector3.one * 0.56f;
            SetLayerRecursive(ball, GameLayers.Target);
            Object.DestroyImmediate(ball.GetComponent<Rigidbody>());

            var view = ball.AddComponent<TrackingMoverView>();
            SetPrivateField(view, "targetRenderer", ball.GetComponent<Renderer>());
            SetPrivateField(view, "normalColor", new Color(0.55f, 0.75f, 1f, 1f));
            SetPrivateField(view, "trackedColor", new Color(1f, 0.5f, 0.35f, 1f));
            SetPrivateField(view, "healthBarWorldWidth", 1.8f);
            SetPrivateField(view, "healthBarWorldHeight", 0.18f);
            SetPrivateField(view, "healthBarHeightOffset", 0.56f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(ball, TrackingMoverPrefabPath);
            Object.DestroyImmediate(ball);
            return prefab.GetComponent<TrackingMoverView>();
        }

        static CustomTargetView EnsureCustomPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CustomPrefabPath);
            if (existing != null)
                return existing.GetComponent<CustomTargetView>();

            var root = new GameObject("CustomTarget");
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(root.transform, false);
            board.transform.localScale = new Vector3(1.2f, 1.2f, 0.1f);
            SetLayerRecursive(board, GameLayers.Target);
            Object.DestroyImmediate(board.GetComponent<Collider>());

            var zoneGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            zoneGo.name = "HitZone";
            zoneGo.transform.SetParent(root.transform, false);
            zoneGo.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            zoneGo.transform.localScale = Vector3.one * 0.45f;
            SetLayerRecursive(zoneGo, GameLayers.Target);
            Object.DestroyImmediate(zoneGo.GetComponent<Rigidbody>());

            var zone = zoneGo.AddComponent<HitZoneView>();
            SetPrivateField(zone, "zoneKind", (int)HitZoneKind.Custom);
            SetPrivateField(zone, "countsAsScore", true);
            SetPrivateField(zone, "zoneRenderer", zoneGo.GetComponent<Renderer>());
            SetPrivateField(zone, "normalColor", new Color(1f, 0.85f, 0.2f, 1f));

            var custom = root.AddComponent<CustomTargetView>();
            SetPrivateField(custom, "hitZones", new[] { zone });

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CustomPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<CustomTargetView>();
        }

        static void EnsureFlyingLevelAsset(
            string path,
            string displayName,
            FlyingTargetView flying,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<FlyingObjectsLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<FlyingObjectsLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("flyingPrefab").objectReferenceValue = flying;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsureCharacterLevelAsset(
            string path,
            string displayName,
            CharacterTargetView character,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<CharacterHeadshotLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<CharacterHeadshotLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 0f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(8f, 0f, 0.5f);
            so.FindProperty("characterPrefab").objectReferenceValue = character;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsureCustomLevelAsset(
            string path,
            string displayName,
            CustomTargetView custom,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<CustomHitZonesLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<CustomHitZonesLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 2f, 7.7f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(8f, 0f, 0.5f);
            so.FindProperty("customTargetPrefab").objectReferenceValue = custom;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsureBouncingLevelAsset(
            string path,
            string displayName,
            BouncingTargetView bouncing,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<BouncingBallsLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<BouncingBallsLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 0.45f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(6f, 2.5f, 4f);
            so.FindProperty("minSpawnDistance").floatValue = 1.1f;
            so.FindProperty("bouncingPrefab").objectReferenceValue = bouncing;
            so.FindProperty("spawnInterval").floatValue = 0.55f;
            so.FindProperty("maxAlive").intValue = 6;
            so.FindProperty("minUpwardSpeed").floatValue = 5f;
            so.FindProperty("maxUpwardSpeed").floatValue = 7f;
            so.FindProperty("lateralSpeed").floatValue = 1.8f;
            so.FindProperty("lifeTime").floatValue = 0f;
            so.FindProperty("bounciness").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsureTrackingLevelAsset(
            string path,
            string displayName,
            TrackingTargetView tracking,
            int requiredKills)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TrackingBallLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<TrackingBallLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredKills;
            so.FindProperty("ammo").intValue = 0;
            so.FindProperty("allowsShooting").boolValue = false;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 0.45f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(6f, 2.5f, 4f);
            so.FindProperty("minSpawnDistance").floatValue = 1.1f;
            so.FindProperty("trackingPrefab").objectReferenceValue = tracking;
            so.FindProperty("maxHealth").floatValue = 100f;
            so.FindProperty("drainPerSecond").floatValue = 40f;
            so.FindProperty("minUpwardSpeed").floatValue = 5f;
            so.FindProperty("maxUpwardSpeed").floatValue = 7f;
            so.FindProperty("lateralSpeed").floatValue = 1.8f;
            so.FindProperty("bounciness").floatValue = 1f;
            so.FindProperty("respawnDelay").floatValue = 0.35f;
            so.FindProperty("aimMaxDistance").floatValue = 200f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsureTrackingMoversLevelAsset(
            string path,
            string displayName,
            TrackingMoverView movers,
            int requiredKills)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TrackingMoversLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<TrackingMoversLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredKills;
            so.FindProperty("ammo").intValue = 0;
            so.FindProperty("allowsShooting").boolValue = false;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 2f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(8f, 3.2f, 0.5f);
            so.FindProperty("minSpawnDistance").floatValue = 1.2f;
            so.FindProperty("moverPrefab").objectReferenceValue = movers;
            so.FindProperty("spawnInterval").floatValue = 0.65f;
            so.FindProperty("maxAlive").intValue = 5;
            so.FindProperty("maxHealth").floatValue = 70f;
            so.FindProperty("drainPerSecond").floatValue = 45f;
            so.FindProperty("speedMin").floatValue = 1.6f;
            so.FindProperty("speedMax").floatValue = 3.2f;
            so.FindProperty("respawnDelay").floatValue = 0f;
            so.FindProperty("aimMaxDistance").floatValue = 200f;
            so.FindProperty("moveDirection").enumValueIndex = (int)FlyDirection.Random;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsureStaticLevelAsset(
            string path,
            string displayName,
            FlyingTargetView staticTarget,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<StaticBallsLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<StaticBallsLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 2f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(8f, 3f, 0.5f);
            so.FindProperty("minSpawnDistance").floatValue = 1.4f;
            so.FindProperty("staticPrefab").objectReferenceValue = staticTarget;
            so.FindProperty("spawnInterval").floatValue = 0.7f;
            so.FindProperty("maxAlive").intValue = 6;
            so.FindProperty("lifeTime").floatValue = 4.5f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsureFlickLevelAsset(
            string path,
            string displayName,
            FlyingTargetView flickTarget,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<FlickTargetsLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<FlickTargetsLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 2f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(10f, 4f, 0.5f);
            so.FindProperty("minSpawnDistance").floatValue = 1.8f;
            so.FindProperty("flickPrefab").objectReferenceValue = flickTarget;
            so.FindProperty("spawnInterval").floatValue = 0.45f;
            so.FindProperty("maxAlive").intValue = 1;
            so.FindProperty("lifeTimeMin").floatValue = 0.35f;
            so.FindProperty("lifeTimeMax").floatValue = 0.8f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static PeekTargetView EnsurePeekPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PeekPrefabPath);
            if (existing != null)
                return existing.GetComponent<PeekTargetView>();

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "PeekTarget";
            ball.transform.localScale = Vector3.one * 0.5f;
            SetLayerRecursive(ball, GameLayers.Target);
            Object.DestroyImmediate(ball.GetComponent<Rigidbody>());

            var view = ball.AddComponent<PeekTargetView>();
            SetPrivateField(view, "targetRenderer", ball.GetComponent<Renderer>());
            SetPrivateField(view, "targetCollider", ball.GetComponent<Collider>());
            SetPrivateField(view, "hiddenColor", new Color(0.35f, 0.35f, 0.4f, 1f));
            SetPrivateField(view, "peekColor", new Color(1f, 0.55f, 0.15f, 1f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(ball, PeekPrefabPath);
            Object.DestroyImmediate(ball);
            return prefab.GetComponent<PeekTargetView>();
        }

        static void EnsurePeekLevelAsset(
            string path,
            string displayName,
            PeekTargetView peekTarget,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<PeekTargetsLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<PeekTargetsLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 1.2f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(8f, 2f, 0.5f);
            so.FindProperty("minSpawnDistance").floatValue = 1.5f;
            so.FindProperty("peekPrefab").objectReferenceValue = peekTarget;
            so.FindProperty("coverCount").intValue = 4;
            so.FindProperty("coverSpacing").floatValue = 2.2f;
            so.FindProperty("coverSize").vector3Value = new Vector3(0.9f, 2f, 0.35f);
            so.FindProperty("peekSideOffset").floatValue = 1.15f;
            so.FindProperty("hideDepth").floatValue = 0.55f;
            so.FindProperty("targetHeight").floatValue = 1.15f;
            so.FindProperty("peekDurationMin").floatValue = 0.5f;
            so.FindProperty("peekDurationMax").floatValue = 1.5f;
            so.FindProperty("hideDurationMin").floatValue = 0.8f;
            so.FindProperty("hideDurationMax").floatValue = 2f;
            so.FindProperty("moveDuration").floatValue = 0.18f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsureRailsLevelAsset(
            string path,
            string displayName,
            FlyingTargetView railTarget,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<MovingRailsLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<MovingRailsLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 2f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(8f, 3f, 0.5f);
            so.FindProperty("minSpawnDistance").floatValue = 1.4f;
            so.FindProperty("railPrefab").objectReferenceValue = railTarget;
            so.FindProperty("spawnInterval").floatValue = 1.1f;
            so.FindProperty("maxAlive").intValue = 4;
            so.FindProperty("railCount").intValue = 3;
            so.FindProperty("railWidth").floatValue = 7f;
            so.FindProperty("railSpacingY").floatValue = 1.1f;
            so.FindProperty("speedMin").floatValue = 2.2f;
            so.FindProperty("speedMax").floatValue = 4.5f;
            so.FindProperty("lifetime").floatValue = 8f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static PriorityTargetView EnsurePriorityPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PriorityPrefabPath);
            if (existing != null)
                return existing.GetComponent<PriorityTargetView>();

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "PriorityTarget";
            ball.transform.localScale = Vector3.one * 0.55f;
            SetLayerRecursive(ball, GameLayers.Target);
            Object.DestroyImmediate(ball.GetComponent<Rigidbody>());

            var view = ball.AddComponent<PriorityTargetView>();
            SetPrivateField(view, "targetRenderer", ball.GetComponent<Renderer>());
            SetPrivateField(view, "normalColor", new Color(0.45f, 0.5f, 0.55f, 1f));
            SetPrivateField(view, "priorityColor", new Color(1f, 0.82f, 0.12f, 1f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(ball, PriorityPrefabPath);
            Object.DestroyImmediate(ball);
            return prefab.GetComponent<PriorityTargetView>();
        }

        static MultiHitTargetView EnsureMultiHitPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(MultiHitPrefabPath);
            if (existing != null)
                return existing.GetComponent<MultiHitTargetView>();

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "MultiHitTarget";
            ball.transform.localScale = Vector3.one * 0.7f;
            SetLayerRecursive(ball, GameLayers.Target);
            Object.DestroyImmediate(ball.GetComponent<Rigidbody>());

            var view = ball.AddComponent<MultiHitTargetView>();
            SetPrivateField(view, "targetRenderer", ball.GetComponent<Renderer>());

            var prefab = PrefabUtility.SaveAsPrefabAsset(ball, MultiHitPrefabPath);
            Object.DestroyImmediate(ball);
            return prefab.GetComponent<MultiHitTargetView>();
        }

        static void EnsurePopupLevelAsset(
            string path,
            string displayName,
            PeekTargetView duck,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<PopupDucksLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<PopupDucksLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 0.2f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(8f, 1f, 2f);
            so.FindProperty("minSpawnDistance").floatValue = 1.2f;
            so.FindProperty("duckPrefab").objectReferenceValue = duck;
            so.FindProperty("columns").intValue = 5;
            so.FindProperty("rows").intValue = 2;
            so.FindProperty("spacing").vector2Value = new Vector2(1.6f, 1.3f);
            so.FindProperty("upHeight").floatValue = 1.1f;
            so.FindProperty("downOffset").floatValue = 1.1f;
            so.FindProperty("upDurationMin").floatValue = 0.7f;
            so.FindProperty("upDurationMax").floatValue = 1.6f;
            so.FindProperty("downDurationMin").floatValue = 0.6f;
            so.FindProperty("downDurationMax").floatValue = 1.8f;
            so.FindProperty("moveDuration").floatValue = 0.2f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsurePriorityLevelAsset(
            string path,
            string displayName,
            PriorityTargetView priority,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<PriorityTargetsLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<PriorityTargetsLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 2f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(8f, 3f, 0.5f);
            so.FindProperty("minSpawnDistance").floatValue = 1.6f;
            so.FindProperty("priorityPrefab").objectReferenceValue = priority;
            so.FindProperty("maxAlive").intValue = 5;
            so.FindProperty("respawnDelay").floatValue = 0.35f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsurePrecisionLevelAsset(
            string path,
            string displayName,
            FlyingTargetView circle,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<PrecisionCirclesLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<PrecisionCirclesLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 2f, 0f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(7f, 3f, 0f);
            so.FindProperty("minSpawnDistance").floatValue = 1.2f;
            so.FindProperty("circlePrefab").objectReferenceValue = circle;
            so.FindProperty("spawnInterval").floatValue = 0.9f;
            so.FindProperty("maxAlive").intValue = 5;
            so.FindProperty("lifeTime").floatValue = 5f;
            so.FindProperty("scaleMin").floatValue = 0.35f;
            so.FindProperty("scaleMax").floatValue = 0.7f;
            so.FindProperty("nearDistance").floatValue = 6f;
            so.FindProperty("farDistance").floatValue = 14f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void EnsureDoubleTapLevelAsset(
            string path,
            string displayName,
            MultiHitTargetView multiHit,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<DoubleTapLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<DoubleTapLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 2f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(8f, 3f, 0.5f);
            so.FindProperty("minSpawnDistance").floatValue = 1.6f;
            so.FindProperty("multiHitPrefab").objectReferenceValue = multiHit;
            so.FindProperty("spawnInterval").floatValue = 0.85f;
            so.FindProperty("maxAlive").intValue = 4;
            so.FindProperty("hitPoints").intValue = 3;
            so.FindProperty("lifeTime").floatValue = 6f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static GalleryTargetView EnsureGalleryPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GalleryPrefabPath);
            if (existing != null)
                return existing.GetComponent<GalleryTargetView>();

            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "GalleryTarget";
            root.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            SetLayerRecursive(root, GameLayers.Target);
            Object.DestroyImmediate(root.GetComponent<Rigidbody>());

            var view = root.AddComponent<GalleryTargetView>();
            SetPrivateField(view, "renderers", new Object[] { root.GetComponent<Renderer>() });
            SetPrivateField(view, "normalColor", new Color(0.75f, 0.75f, 0.78f, 1f));
            SetPrivateField(view, "highlightColor", new Color(1f, 0.82f, 0.12f, 1f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GalleryPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<GalleryTargetView>();
        }

        static void EnsureGalleryLevelAsset(
            string path,
            string displayName,
            GalleryTargetView prefab,
            int requiredHits,
            int ammo)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ShootingGalleryLevelDefinition>(path);
            var asset = existing != null ? existing : ScriptableObject.CreateInstance<ShootingGalleryLevelDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("requiredHits").intValue = requiredHits;
            so.FindProperty("ammo").intValue = ammo;
            so.FindProperty("allowsShooting").boolValue = true;
            so.FindProperty("autoStart").boolValue = true;
            so.FindProperty("spawnCenter").vector3Value = new Vector3(0f, 1.1f, 8f);
            so.FindProperty("spawnSize").vector3Value = new Vector3(6f, 1.5f, 0f);
            so.FindProperty("minSpawnDistance").floatValue = 1.2f;
            so.FindProperty("columns").intValue = 5;
            so.FindProperty("rows").intValue = 2;
            so.FindProperty("spacing").vector2Value = new Vector2(1.4f, 1.2f);
            so.FindProperty("facingYaw").floatValue = 180f;
            so.FindProperty("retargetDelay").floatValue = 0.35f;
            so.FindProperty("normalColor").colorValue = new Color(0.75f, 0.75f, 0.78f, 1f);
            so.FindProperty("highlightColor").colorValue = new Color(1f, 0.82f, 0.12f, 1f);

            var prefabsProp = so.FindProperty("targetPrefabs");
            prefabsProp.arraySize = 1;
            prefabsProp.GetArrayElementAtIndex(0).objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            if (layer < 0)
                return;
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        static void SetPrivateField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SetPrivateField(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null || !prop.isArray)
                return;

            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetPrivateField(Object target, string fieldName, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (prop.propertyType == SerializedPropertyType.Enum)
                    prop.enumValueIndex = value;
                else
                    prop.intValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SetPrivateField(Object target, string fieldName, bool value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.boolValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SetPrivateField(Object target, string fieldName, Color value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.colorValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SetPrivateField(Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.floatValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
