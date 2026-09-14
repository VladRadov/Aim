using Aim;
using Aim.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aim.Editor
{
    public static class SettingsUiSetup
    {
        static readonly Color IconCircle = new(0.16f, 0.24f, 0.36f, 1f);
        static readonly Color IconCircleBorder = new(0.40f, 0.70f, 0.95f, 0.85f);
        static readonly Color PanelBody = new(0.09f, 0.11f, 0.16f, 0.98f);
        static readonly Color PanelBorder = new(0.32f, 0.62f, 0.92f, 0.55f);
        static readonly Color PanelShadow = new(0f, 0f, 0f, 0.55f);
        static readonly Color HeaderBar = new(0.14f, 0.22f, 0.34f, 1f);
        static readonly Color Accent = new(0.38f, 0.78f, 1f, 1f);
        static readonly Color SoftText = new(0.78f, 0.84f, 0.92f, 1f);
        static readonly Color RowCard = new(0.13f, 0.17f, 0.24f, 0.95f);
        static readonly Color TrackBg = new(0.06f, 0.08f, 0.12f, 1f);
        static readonly Color DimmerColor = new(0.02f, 0.04f, 0.08f, 0.78f);

        [MenuItem("Aim/Rebuild Settings UI")]
        public static void RebuildSettingsUi()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found. Run Aim/Setup Game Scene or create a Canvas first.");
                return;
            }

            EnsureEventSystem();

            var existing = Object.FindAnyObjectByType<SettingsView>();
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var settings = CreateSettingsUi(canvas.transform);
            var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
            if (bootstrap != null)
            {
                SetObjectField(bootstrap, "settingsView", settings);

                var audioRoot = GameObject.Find("GameAudio") ?? new GameObject("GameAudio");
                var music = audioRoot.GetComponent<AudioSource>() ?? audioRoot.AddComponent<AudioSource>();
                music.loop = true;
                music.playOnAwake = false;
                music.spatialBlend = 0f;

                var sfxTransform = audioRoot.transform.Find("SfxSource");
                AudioSource sfx;
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

                SetObjectField(bootstrap, "musicSource", music);
                SetObjectField(bootstrap, "sfxSource", sfx);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = settings.gameObject;
            Debug.Log("Settings UI rebuilt with darker icon badges and polished panel.");
        }

        static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (oldModule != null)
                    Object.DestroyImmediate(oldModule);
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        static SettingsView CreateSettingsUi(Transform canvasTransform)
        {
            var root = new GameObject("SettingsUI", typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
            StretchFull(root.GetComponent<RectTransform>());

            var settingsButton = CreateCircleIconButton(
                root.transform,
                "SettingsButton",
                new Vector2(1f, 1f),
                new Vector2(-18f, -18f),
                new Vector2(58f, 58f),
                "Assets/Aim/Sprites/settings_icon.png",
                30f);

            var dimmerGo = new GameObject("Dimmer", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
            dimmerGo.transform.SetParent(root.transform, false);
            StretchFull(dimmerGo.GetComponent<RectTransform>());
            var dimmerImage = dimmerGo.GetComponent<Image>();
            dimmerImage.color = DimmerColor;
            dimmerImage.raycastTarget = true;
            var dimmerGroup = dimmerGo.GetComponent<CanvasGroup>();
            var dimmerButton = dimmerGo.GetComponent<Button>();
            dimmerButton.transition = Selectable.Transition.None;

            var panelRootGo = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasGroup));
            panelRootGo.transform.SetParent(root.transform, false);
            var panelRoot = panelRootGo.GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.sizeDelta = new Vector2(520f, 390f);
            panelRoot.anchoredPosition = Vector2.zero;
            var panelGroup = panelRootGo.GetComponent<CanvasGroup>();

            CreateImage(
                panelRootGo.transform,
                "Shadow",
                new Vector2(0.5f, 0.5f),
                new Vector2(8f, -10f),
                new Vector2(528f, 398f),
                PanelShadow,
                "UI/Skin/Background.psd",
                true);

            CreateImage(
                panelRootGo.transform,
                "Border",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(520f, 390f),
                PanelBorder,
                "UI/Skin/Background.psd",
                true);

            var body = CreateImage(
                panelRootGo.transform,
                "Body",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(508f, 378f),
                PanelBody,
                "UI/Skin/Background.psd",
                true);

            var header = CreateImage(
                body.transform,
                "Header",
                new Vector2(0.5f, 1f),
                new Vector2(0f, 0f),
                new Vector2(508f, 78f),
                HeaderBar,
                "UI/Skin/Background.psd",
                true);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);

            CreateImage(
                header.transform,
                "AccentLine",
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(508f, 3f),
                Accent,
                "UI/Skin/UISprite.psd",
                true);

            CreateLabel(header.transform, "Title", "НАСТРОЙКИ", new Vector2(0f, -22f), 32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            var closeButton = CreateCloseButton(body.transform);

            var musicSlider = CreateSliderCard(
                body.transform,
                "MusicRow",
                new Vector2(0f, 35f),
                "Assets/Aim/Sprites/music_icon.png",
                "Музыка");

            var sfxSlider = CreateSliderCard(
                body.transform,
                "SfxRow",
                new Vector2(0f, -75f),
                "Assets/Aim/Sprites/sfx_icon.png",
                "Звуки");

            var view = root.AddComponent<SettingsView>();
            SetObjectField(view, "settingsButton", settingsButton);
            SetObjectField(view, "closeButton", closeButton);
            SetObjectField(view, "dimmerButton", dimmerButton);
            SetObjectField(view, "dimmerGroup", dimmerGroup);
            SetObjectField(view, "panelGroup", panelGroup);
            SetObjectField(view, "panelRoot", panelRoot);
            SetObjectField(view, "musicSlider", musicSlider);
            SetObjectField(view, "sfxSlider", sfxSlider);

            return view;
        }

        static Button CreateCircleIconButton(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size,
            string iconPath,
            float iconSize)
        {
            var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var border = buttonGo.GetComponent<Image>();
            border.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            border.color = IconCircleBorder;
            border.raycastTarget = true;
            border.preserveAspect = true;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(buttonGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.5f, 0.5f);
            fillRect.anchorMax = new Vector2(0.5f, 0.5f);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.sizeDelta = size - new Vector2(6f, 6f);
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            fill.color = IconCircle;
            fill.raycastTarget = false;
            fill.preserveAspect = true;

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

            var button = buttonGo.GetComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = IconCircleBorder;
            colors.highlightedColor = Accent;
            colors.pressedColor = new Color(0.25f, 0.55f, 0.85f, 1f);
            button.colors = colors;
            return button;
        }

        static Button CreateCloseButton(Transform parent)
        {
            var buttonGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-14f, -14f);
            rect.sizeDelta = new Vector2(34f, 34f);

            var image = buttonGo.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            image.color = new Color(0.22f, 0.28f, 0.38f, 1f);

            CreateLabel(buttonGo.transform, "X", "✕", Vector2.zero, 18, FontStyle.Bold, TextAnchor.MiddleCenter, SoftText);

            var button = buttonGo.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        static Slider CreateSliderCard(Transform parent, string name, Vector2 anchoredPos, string iconPath, string label)
        {
            var card = CreateImage(
                parent,
                name,
                new Vector2(0.5f, 0.5f),
                anchoredPos,
                new Vector2(460f, 92f),
                RowCard,
                "UI/Skin/Background.psd",
                true);

            var accent = CreateImage(
                card.transform,
                "SideAccent",
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0f),
                new Vector2(4f, 92f),
                Accent,
                "UI/Skin/UISprite.psd",
                true);
            var accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0.5f);
            accentRect.anchorMax = new Vector2(0f, 0.5f);
            accentRect.pivot = new Vector2(0f, 0.5f);

            CreateBadge(card.transform, new Vector2(18f, 0f), iconPath);

            var labelText = CreateLabel(card.transform, "Label", label, new Vector2(-40f, 24f), 20, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            labelText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            labelText.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            labelText.rectTransform.pivot = new Vector2(0f, 0.5f);
            labelText.rectTransform.anchoredPosition = new Vector2(78f, 22f);
            labelText.rectTransform.sizeDelta = new Vector2(240f, 28f);

            return CreateRoundHandleSlider(card.transform, new Vector2(78f, -14f), new Vector2(350f, 28f));
        }

        static void CreateBadge(Transform parent, Vector2 anchoredPos, string iconPath)
        {
            var borderGo = new GameObject("IconBadge", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(parent, false);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 0.5f);
            borderRect.anchorMax = new Vector2(0f, 0.5f);
            borderRect.pivot = new Vector2(0f, 0.5f);
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

        static Slider CreateRoundHandleSlider(Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(parent, false);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0.5f);
            sliderRect.anchorMax = new Vector2(0f, 0.5f);
            sliderRect.pivot = new Vector2(0f, 0.5f);
            sliderRect.anchoredPosition = anchoredPos;
            sliderRect.sizeDelta = size;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderGo.transform, false);
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.offsetMin = new Vector2(0f, -6f);
            bgRect.offsetMax = new Vector2(0f, 6f);
            var bgImage = background.GetComponent<Image>();
            bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            bgImage.type = Image.Type.Sliced;
            bgImage.color = TrackBg;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRect.offsetMin = new Vector2(8f, -5f);
            fillAreaRect.offsetMax = new Vector2(-8f, 5f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.GetComponent<Image>();
            fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fillImage.type = Image.Type.Sliced;
            fillImage.color = Accent;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(12f, 0f);
            handleAreaRect.offsetMax = new Vector2(-12f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            // Keep a true circle: fixed anchors, equal size, no stretch.
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(22f, 22f);
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            handleImage.color = Color.white;
            handleImage.preserveAspect = true;
            handleImage.raycastTarget = true;

            var ring = new GameObject("HandleRing", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(handle.transform, false);
            var ringRect = ring.GetComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(28f, 28f);
            var ringImage = ring.GetComponent<Image>();
            ringImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            ringImage.color = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            ringImage.raycastTarget = false;
            ringImage.preserveAspect = true;
            ring.transform.SetAsFirstSibling();

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 0.7f;
            return slider;
        }

        static Image CreateImage(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size,
            Color color,
            string builtinSprite,
            bool sliced)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinSprite);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        static Text CreateLabel(
            Transform parent,
            string name,
            string content,
            Vector2 anchoredPos,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(400f, 40f);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            LanguageYgTextUtility.AttachToTextComponent(text, content);
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
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
