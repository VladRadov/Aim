using Aim;
using Aim.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        static readonly Color DimmerColor = new(0.02f, 0.04f, 0.08f, 0.78f);
        static readonly Color NextButtonFill = new(0.18f, 0.42f, 0.68f, 1f);
        static readonly Color RewardButtonFill = new(0.55f, 0.40f, 0.12f, 1f);
        static readonly Color NextButtonBorder = new(0.45f, 0.82f, 1f, 0.95f);
        static readonly Color RewardButtonBorder = new(1f, 0.82f, 0.35f, 0.95f);

        [MenuItem("Aim/Rebuild Level Win UI")]
        public static void RebuildLevelWinUi()
        {
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
            Debug.Log("Level Win UI rebuilt and linked to Bootstrap.");
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

            var panelRootGo = new GameObject("WinPanel", typeof(RectTransform), typeof(CanvasGroup));
            panelRootGo.transform.SetParent(root.transform, false);
            var panelRoot = panelRootGo.GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.sizeDelta = new Vector2(520f, 360f);
            panelRoot.anchoredPosition = Vector2.zero;
            var panelGroup = panelRootGo.GetComponent<CanvasGroup>();

            CreateImage(panelRootGo.transform, "Shadow", new Vector2(0.5f, 0.5f), new Vector2(8f, -10f), new Vector2(528f, 368f), PanelShadow, true);
            CreateImage(panelRootGo.transform, "Border", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 360f), PanelBorder, true);
            var body = CreateImage(panelRootGo.transform, "Body", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(508f, 348f), PanelBody, true);

            var header = CreateImage(body.transform, "Header", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(508f, 84f), HeaderBar, true);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);

            CreateImage(header.transform, "AccentLine", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(508f, 3f), Accent, true);

            var title = CreateLabel(header.transform, "Title", "Уровень пройден", new Vector2(0f, -28f), 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            title.rectTransform.sizeDelta = new Vector2(460f, 48f);

            var bonus = CreateLabel(body.transform, "WinBonus", "+10", new Vector2(0f, 55f), 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.35f, 1f));
            bonus.rectTransform.sizeDelta = new Vector2(280f, 44f);

            var nextButton = CreateActionButton(
                body.transform,
                "NextLevelButton",
                new Vector2(-115f, -85f),
                "Следующий",
                "Assets/Aim/Sprites/next_level_icon.png",
                NextButtonFill,
                NextButtonBorder,
                out var nextGroup);

            var rewardButton = CreateActionButton(
                body.transform,
                "RewardAdButton",
                new Vector2(115f, -85f),
                "x2 за рекламу",
                "Assets/Aim/Sprites/reward_ad_icon.png",
                RewardButtonFill,
                RewardButtonBorder,
                out _);

            var view = root.AddComponent<LevelWinView>();
            SetObjectField(view, "nextLevelButton", nextButton);
            SetObjectField(view, "rewardAdButton", rewardButton);
            SetObjectField(view, "dimmerGroup", dimmerGroup);
            SetObjectField(view, "panelGroup", panelGroup);
            SetObjectField(view, "panelRoot", panelRoot);
            SetObjectField(view, "titleText", title);
            SetObjectField(view, "winBonusText", bonus);

            // Ensure next button can fade when unavailable.
            if (nextGroup == null)
                nextButton.gameObject.AddComponent<CanvasGroup>();

            return view;
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
            rect.sizeDelta = new Vector2(210f, 92f);

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
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            fill.type = Image.Type.Sliced;
            fill.color = fillColor;
            fill.raycastTarget = false;

            var cardAccent = CreateImage(fillGo.transform, "TopAccent", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(204f, 3f), Accent, true);
            cardAccent.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            cardAccent.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            cardAccent.rectTransform.pivot = new Vector2(0.5f, 1f);

            CreateSettingsStyleBadge(fillGo.transform, new Vector2(0f, 14f), iconPath);

            var labelText = CreateLabel(fillGo.transform, "Label", label, new Vector2(0f, -26f), 18, FontStyle.Bold, TextAnchor.MiddleCenter, SoftText);
            labelText.rectTransform.sizeDelta = new Vector2(190f, 28f);

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

        static void CreateSettingsStyleBadge(Transform parent, Vector2 anchoredPos, string iconPath)
        {
            var borderGo = new GameObject("IconBadge", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(parent, false);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.anchoredPosition = anchoredPos;
            borderRect.sizeDelta = new Vector2(48f, 48f);
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
            fillRect.sizeDelta = new Vector2(40f, 40f);
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
            iconRect.sizeDelta = new Vector2(26f, 26f);
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
    }
}
