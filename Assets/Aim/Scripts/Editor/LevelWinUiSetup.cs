using System.IO;
using Aim;
using Aim.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aim.Editor
{
    public static class LevelWinUiSetup
    {
        static readonly Color IconCircle = new(0.16f, 0.24f, 0.36f, 1f);
        static readonly Color IconCircleBorder = new(0.40f, 0.70f, 0.95f, 0.85f);
        static readonly Color PanelBody = new(0.09f, 0.11f, 0.16f, 0.98f);
        static readonly Color PanelBorder = new(0.32f, 0.62f, 0.92f, 0.55f);
        static readonly Color PanelShadow = new(0f, 0f, 0f, 0.55f);
        static readonly Color HeaderBar = new(0.14f, 0.22f, 0.34f, 1f);
        static readonly Color Accent = new(0.38f, 0.78f, 1f, 1f);
        static readonly Color SoftText = new(0.78f, 0.84f, 0.92f, 1f);
        static readonly Color MutedText = new(0.58f, 0.64f, 0.74f, 1f);
        static readonly Color DimmerColor = new(0.02f, 0.04f, 0.08f, 0.78f);
        static readonly Color NextButtonFill = new(0.18f, 0.42f, 0.68f, 1f);
        static readonly Color RetryButtonFill = new(0.22f, 0.34f, 0.48f, 1f);
        static readonly Color RewardButtonFill = new(0.55f, 0.40f, 0.12f, 1f);
        static readonly Color NextButtonBorder = new(0.45f, 0.82f, 1f, 0.95f);
        static readonly Color RetryButtonBorder = new(0.55f, 0.72f, 0.90f, 0.9f);
        static readonly Color RewardButtonBorder = new(1f, 0.82f, 0.35f, 0.95f);
        static readonly Color StatRowBg = new(0.12f, 0.16f, 0.24f, 0.92f);
        static readonly Color RecordColor = new(1f, 0.86f, 0.42f, 1f);
        static readonly Color StarLit = new(1f, 0.84f, 0.28f, 1f);
        static readonly Color StarDim = new(0.28f, 0.32f, 0.40f, 0.85f);

        const float PanelW = 400f;
        const float PanelH = 390f;

        [MenuItem("Aim/Rebuild Level Win UI")]
        public static void RebuildLevelWinUi()
        {
            EnsureStatIcons();

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found. Run Aim/Setup Game Scene first.");
                return;
            }

            var existing = Object.FindAnyObjectByType<LevelWinView>();
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var view = CreateLevelWinUi(canvas.transform);
            var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
            if (bootstrap != null)
                SetObjectField(bootstrap, "levelWinView", view);

            EnsureLevelsOnConfig();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = view.gameObject;
            Debug.Log("Level Win/Lose UI rebuilt and linked to Bootstrap.");
        }

        static void EnsureLevelsOnConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<Aim.Config.AimTrainerConfig>("Assets/Aim/Config/AimTrainerConfig.asset");
            if (config == null)
                return;

            var serialized = new SerializedObject(config);
            var levelsProp = serialized.FindProperty("levels");
            if (levelsProp == null)
                return;

            if (levelsProp.arraySize > 0)
                return;

            var flying = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_FlyingObjects.asset");
            var bouncing = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_BouncingBalls.asset");
            var tracking = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_TrackingBall.asset");
            var movers = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_TrackingMovers.asset");
            var staticBalls = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_StaticBalls.asset");
            var flick = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_FlickTargets.asset");
            var peek = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_PeekTargets.asset");
            var rails = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_MovingRails.asset");
            var popup = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_PopupDucks.asset");
            var priority = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_PriorityTargets.asset");
            var precision = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_PrecisionCircles.asset");
            var doubleTap = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_DoubleTap.asset");
            var character = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_CharacterHeadshot.asset");
            var custom = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_CustomHitZones.asset");
            var gallery = AssetDatabase.LoadAssetAtPath<Aim.Config.LevelDefinition>("Assets/Aim/Config/Levels/Level_ShootingGallery.asset");

            levelsProp.arraySize = 15;
            levelsProp.GetArrayElementAtIndex(0).objectReferenceValue = flick;
            levelsProp.GetArrayElementAtIndex(1).objectReferenceValue = peek;
            levelsProp.GetArrayElementAtIndex(2).objectReferenceValue = rails;
            levelsProp.GetArrayElementAtIndex(3).objectReferenceValue = popup;
            levelsProp.GetArrayElementAtIndex(4).objectReferenceValue = priority;
            levelsProp.GetArrayElementAtIndex(5).objectReferenceValue = precision;
            levelsProp.GetArrayElementAtIndex(6).objectReferenceValue = doubleTap;
            levelsProp.GetArrayElementAtIndex(7).objectReferenceValue = staticBalls;
            levelsProp.GetArrayElementAtIndex(8).objectReferenceValue = flying;
            levelsProp.GetArrayElementAtIndex(9).objectReferenceValue = bouncing;
            levelsProp.GetArrayElementAtIndex(10).objectReferenceValue = gallery;
            levelsProp.GetArrayElementAtIndex(11).objectReferenceValue = character;
            levelsProp.GetArrayElementAtIndex(12).objectReferenceValue = custom;
            levelsProp.GetArrayElementAtIndex(13).objectReferenceValue = tracking;
            levelsProp.GetArrayElementAtIndex(14).objectReferenceValue = movers;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        static LevelWinView CreateLevelWinUi(Transform canvasTransform)
        {
            var root = new GameObject("LevelWinUI", typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
            StretchFull(root.GetComponent<RectTransform>());

            var dimmerGo = new GameObject("Dimmer", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            dimmerGo.transform.SetParent(root.transform, false);
            StretchFull(dimmerGo.GetComponent<RectTransform>());
            var dimmerImage = dimmerGo.GetComponent<Image>();
            dimmerImage.color = DimmerColor;
            dimmerImage.raycastTarget = true;
            var dimmerGroup = dimmerGo.GetComponent<CanvasGroup>();

            var panelRootGo = new GameObject("ResultPanel", typeof(RectTransform), typeof(CanvasGroup));
            panelRootGo.transform.SetParent(root.transform, false);
            var panelRoot = panelRootGo.GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.sizeDelta = new Vector2(PanelW, PanelH);
            panelRoot.anchoredPosition = Vector2.zero;
            var panelGroup = panelRootGo.GetComponent<CanvasGroup>();

            CreateImage(panelRootGo.transform, "Shadow", new Vector2(0.5f, 0.5f), new Vector2(6f, -8f), new Vector2(PanelW + 8f, PanelH + 8f), PanelShadow, true);
            CreateImage(panelRootGo.transform, "Border", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelW, PanelH), PanelBorder, true);
            var body = CreateImage(panelRootGo.transform, "Body", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelW - 10f, PanelH - 10f), PanelBody, true);

            var header = CreateImage(body.transform, "Header", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(PanelW - 10f, 48f), HeaderBar, true);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);

            CreateImage(header.transform, "AccentLine", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(PanelW - 10f, 2f), Accent, true);

            var title = CreateLabel(header.transform, "Title", "Уровень пройден", new Vector2(0f, -16f), 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            title.rectTransform.sizeDelta = new Vector2(360f, 30f);

            var stars = CreateStarsRow(body.transform, new Vector2(0f, 112f));
            var record = CreateLabel(body.transform, "Record", "Новый рекорд!", new Vector2(0f, 84f), 14, FontStyle.Bold, TextAnchor.MiddleCenter, RecordColor);
            record.rectTransform.sizeDelta = new Vector2(360f, 20f);

            var accuracy = CreateStatCell(body.transform, "AccuracyStat", new Vector2(-94f, 40f), "Assets/Aim/Sprites/Stats/accuracy_icon.png", "Точность", "100%");
            var time = CreateStatCell(body.transform, "TimeStat", new Vector2(94f, 40f), "Assets/Aim/Sprites/Stats/time_icon.png", "Время", "0:00");
            var coins = CreateStatCell(body.transform, "CoinsStat", new Vector2(-94f, -16f), "Assets/Aim/Sprites/coin_icon.png", "Монеты", "+0");
            var shots = CreateStatCell(body.transform, "ShotsStat", new Vector2(94f, -16f), "Assets/Aim/Sprites/ammo_icon.png", "Выстрелы", "0/0");

            var retryButton = CreateActionButton(
                body.transform,
                "RetryButton",
                new Vector2(-118f, -100f),
                "Ещё раз",
                "Assets/Aim/Sprites/Stats/retry_icon.png",
                RetryButtonFill,
                RetryButtonBorder,
                out _);

            var nextButton = CreateActionButton(
                body.transform,
                "NextLevelButton",
                new Vector2(0f, -100f),
                "Дальше",
                "Assets/Aim/Sprites/next_level_icon.png",
                NextButtonFill,
                NextButtonBorder,
                out var nextGroup);

            var rewardButton = CreateActionButton(
                body.transform,
                "RewardAdButton",
                new Vector2(118f, -100f),
                "x2",
                "Assets/Aim/Sprites/reward_ad_icon.png",
                RewardButtonFill,
                RewardButtonBorder,
                out _);

            var menuButton = CreateMenuButton(body.transform, new Vector2(0f, -160f));

            var view = root.AddComponent<LevelWinView>();
            SetObjectField(view, "nextLevelButton", nextButton);
            SetObjectField(view, "retryButton", retryButton);
            SetObjectField(view, "rewardAdButton", rewardButton);
            SetObjectField(view, "menuButton", menuButton);
            SetObjectField(view, "dimmerGroup", dimmerGroup);
            SetObjectField(view, "panelGroup", panelGroup);
            SetObjectField(view, "panelRoot", panelRoot);
            SetObjectField(view, "titleText", title);
            SetObjectField(view, "recordText", record);
            SetObjectField(view, "accuracyValueText", accuracy.value);
            SetObjectField(view, "timeValueText", time.value);
            SetObjectField(view, "coinsValueText", coins.value);
            SetObjectField(view, "shotsValueText", shots.value);
            SetObjectField(view, "accuracyLabelText", accuracy.label);
            SetObjectField(view, "timeLabelText", time.label);
            SetObjectField(view, "coinsLabelText", coins.label);
            SetObjectField(view, "shotsLabelText", shots.label);
            SetColorField(view, "starLitColor", StarLit);
            SetColorField(view, "starDimColor", StarDim);

            var so = new SerializedObject(view);
            var starsProp = so.FindProperty("starImages");
            if (starsProp != null)
            {
                starsProp.arraySize = stars.Length;
                for (var i = 0; i < stars.Length; i++)
                    starsProp.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (nextGroup == null)
                nextButton.gameObject.AddComponent<CanvasGroup>();

            return view;
        }

        static Image[] CreateStarsRow(Transform parent, Vector2 anchoredPos)
        {
            var row = new GameObject("Stars", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = anchoredPos;
            rowRect.sizeDelta = new Vector2(170f, 34f);

            var starSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Aim/Sprites/Stats/star_icon.png");
            var images = new Image[3];
            var offsets = new[] { -50f, 0f, 50f };
            for (var i = 0; i < 3; i++)
            {
                var starGo = new GameObject($"Star{i + 1}", typeof(RectTransform), typeof(Image));
                starGo.transform.SetParent(row.transform, false);
                var rect = starGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(offsets[i], 0f);
                rect.sizeDelta = new Vector2(32f, 32f);
                var image = starGo.GetComponent<Image>();
                image.sprite = starSprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = StarDim;
                images[i] = image;
            }

            return images;
        }

        static (Text label, Text value) CreateStatCell(
            Transform parent,
            string name,
            Vector2 anchoredPos,
            string iconPath,
            string label,
            string value)
        {
            var rowGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            rowGo.transform.SetParent(parent, false);
            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = anchoredPos;
            rowRect.sizeDelta = new Vector2(178f, 48f);
            var bg = rowGo.GetComponent<Image>();
            bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            bg.type = Image.Type.Sliced;
            bg.color = StatRowBg;
            bg.raycastTarget = false;

            CreateSettingsStyleBadge(rowGo.transform, new Vector2(-66f, 0f), iconPath, 30f, 16f);

            var labelText = CreateLabel(rowGo.transform, "Label", label, new Vector2(18f, 8f), 12, FontStyle.Normal, TextAnchor.MiddleLeft, MutedText);
            labelText.rectTransform.sizeDelta = new Vector2(100f, 18f);

            var valueText = CreateLabel(rowGo.transform, "Value", value, new Vector2(18f, -10f), 16, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            valueText.rectTransform.sizeDelta = new Vector2(100f, 20f);

            return (labelText, valueText);
        }

        static Button CreateMenuButton(Transform parent, Vector2 anchoredPos)
        {
            var buttonGo = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(200f, 36f);

            var image = buttonGo.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.16f, 0.20f, 0.28f, 1f);

            var label = CreateLabel(buttonGo.transform, "Label", "В меню", Vector2.zero, 15, FontStyle.Bold, TextAnchor.MiddleCenter, SoftText);
            label.rectTransform.sizeDelta = new Vector2(180f, 28f);
            return buttonGo.GetComponent<Button>();
        }

        static Button CreateActionButton(
            Transform parent,
            string name,
            Vector2 anchoredPos,
            string label,
            string iconPath,
            Color fillColor,
            Color borderColor,
            out CanvasGroup canvasGroup)
        {
            var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            buttonGo.transform.SetParent(parent, false);
            canvasGroup = buttonGo.GetComponent<CanvasGroup>();

            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(108f, 64f);

            var border = buttonGo.GetComponent<Image>();
            border.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            border.type = Image.Type.Sliced;
            border.color = borderColor;
            border.raycastTarget = true;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(buttonGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            fill.type = Image.Type.Sliced;
            fill.color = fillColor;
            fill.raycastTarget = false;

            var cardAccent = CreateImage(fillGo.transform, "TopAccent", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(104f, 2f), Accent, true);
            cardAccent.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            cardAccent.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            cardAccent.rectTransform.pivot = new Vector2(0.5f, 1f);

            CreateSettingsStyleBadge(fillGo.transform, new Vector2(0f, 10f), iconPath, 28f, 15f);

            var labelText = CreateLabel(fillGo.transform, "Label", label, new Vector2(0f, -20f), 12, FontStyle.Bold, TextAnchor.MiddleCenter, SoftText);
            labelText.rectTransform.sizeDelta = new Vector2(96f, 18f);

            var button = buttonGo.GetComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = borderColor;
            colors.highlightedColor = Color.Lerp(borderColor, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(borderColor, Color.black, 0.2f);
            colors.disabledColor = new Color(0.35f, 0.38f, 0.42f, 0.7f);
            button.colors = colors;
            return button;
        }

        static void CreateSettingsStyleBadge(Transform parent, Vector2 anchoredPos, string iconPath, float badgeSize = 48f, float iconSize = 26f)
        {
            var borderGo = new GameObject("IconBadge", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(parent, false);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.anchoredPosition = anchoredPos;
            borderRect.sizeDelta = new Vector2(badgeSize, badgeSize);
            var border = borderGo.GetComponent<Image>();
            border.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            border.color = IconCircleBorder;
            border.preserveAspect = true;
            border.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(borderGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.5f, 0.5f);
            fillRect.anchorMax = new Vector2(0.5f, 0.5f);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.sizeDelta = new Vector2(badgeSize - 8f, badgeSize - 8f);
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            fill.color = IconCircle;
            fill.preserveAspect = true;
            fill.raycastTarget = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(fillGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = Color.white;
        }

        static Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color, bool sliced)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text CreateLabel(Transform parent, string name, string content, Vector2 anchoredPos, int fontSize, FontStyle style, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(400f, 40f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void SetObjectField(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SetColorField(Object target, string fieldName, Color value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.colorValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void EnsureStatIcons()
        {
            var dir = "Assets/Aim/Sprites/Stats";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Aim/Sprites"))
                    AssetDatabase.CreateFolder("Assets/Aim", "Sprites");
                AssetDatabase.CreateFolder("Assets/Aim/Sprites", "Stats");
            }

            WriteIconIfMissing($"{dir}/star_icon.png", PaintStar);
            WriteIconIfMissing($"{dir}/accuracy_icon.png", PaintAccuracy);
            WriteIconIfMissing($"{dir}/time_icon.png", PaintClock);
            WriteIconIfMissing($"{dir}/retry_icon.png", PaintRetry);
            AssetDatabase.Refresh();

            ConfigureSprite($"{dir}/star_icon.png");
            ConfigureSprite($"{dir}/accuracy_icon.png");
            ConfigureSprite($"{dir}/time_icon.png");
            ConfigureSprite($"{dir}/retry_icon.png");
        }

        static void ConfigureSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            if (importer.textureType == TextureImporterType.Sprite &&
                !importer.mipmapEnabled)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        static void WriteIconIfMissing(string path, System.Action<Color32[], int> painter)
        {
            if (File.Exists(path))
                return;

            const int size = 64;
            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, 0);

            painter(pixels, size);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        static void PaintStar(Color32[] pixels, int size)
        {
            var gold = new Color32(255, 214, 70, 255);
            var center = (size - 1) * 0.5f;
            var outer = size * 0.46f;
            var inner = size * 0.20f;

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var angle = Mathf.Atan2(dy, dx);
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                // 5-point star polar radius
                var a = ((angle + Mathf.PI * 0.5f) % (Mathf.PI * 2f) + Mathf.PI * 2f) % (Mathf.PI * 2f);
                var segment = Mathf.PI * 2f / 5f;
                var local = a % segment;
                var t = Mathf.Abs(local - segment * 0.5f) / (segment * 0.5f);
                var edge = Mathf.Lerp(outer, inner, t);
                if (r <= edge)
                    pixels[y * size + x] = gold;
            }
        }

        static void PaintAccuracy(Color32[] pixels, int size)
        {
            var color = new Color32(170, 220, 255, 255);
            var c = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x - c;
                var dy = y - c;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var onRing = Mathf.Abs(r - size * 0.28f) < 2.2f || Mathf.Abs(r - size * 0.12f) < 2f;
                var onCross = (Mathf.Abs(dx) < 1.8f && Mathf.Abs(dy) < size * 0.42f) ||
                              (Mathf.Abs(dy) < 1.8f && Mathf.Abs(dx) < size * 0.42f);
                if (onRing || onCross)
                    pixels[y * size + x] = color;
            }
        }

        static void PaintClock(Color32[] pixels, int size)
        {
            var color = new Color32(170, 220, 255, 255);
            var c = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x - c;
                var dy = y - c;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var onRing = Mathf.Abs(r - size * 0.32f) < 2.4f;
                var onHand = false;
                if (dx >= -1.6f && dx <= 1.6f && dy >= 0f && dy <= size * 0.22f)
                    onHand = true;
                if (dy >= -1.6f && dy <= 1.6f && dx >= 0f && dx <= size * 0.18f)
                    onHand = true;
                if (onRing || onHand)
                    pixels[y * size + x] = color;
            }
        }

        static void PaintRetry(Color32[] pixels, int size)
        {
            var color = new Color32(170, 220, 255, 255);
            var c = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x - c;
                var dy = y - c;
                var angle = Mathf.Atan2(dy, dx);
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var onArc = r > size * 0.22f && r < size * 0.34f && angle > -2.2f && angle < 2.0f;
                var onArrow = dx > size * 0.08f && dx < size * 0.28f && dy > size * 0.08f && dy < size * 0.28f &&
                              Mathf.Abs(dx - dy) < 4f;
                if (onArc || onArrow)
                    pixels[y * size + x] = color;
            }
        }
    }
}
