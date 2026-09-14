using System.IO;
using Aim;
using Aim.Models;
using Aim.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aim.Editor
{
    public static class MainMenuUiSetup
    {
        static readonly Color PanelBody = new(0.09f, 0.11f, 0.16f, 0.96f);
        static readonly Color PanelBorder = new(0.32f, 0.62f, 0.92f, 0.55f);
        static readonly Color HeaderBar = new(0.14f, 0.22f, 0.34f, 1f);
        static readonly Color Accent = new(0.38f, 0.78f, 1f, 1f);
        static readonly Color SoftText = new(0.78f, 0.84f, 0.92f, 1f);
        static readonly Color DimmerColor = new(0.02f, 0.04f, 0.08f, 0.55f);
        static readonly Color CampaignFill = new(0.18f, 0.42f, 0.68f, 1f);
        static readonly Color ModesFill = new(0.22f, 0.34f, 0.48f, 1f);
        static readonly Color CardFill = new(0.10f, 0.13f, 0.18f, 1f);
        static readonly Color StepButtonFill = new(0.20f, 0.30f, 0.42f, 1f);

        [MenuItem("Aim/Rebuild Main Menu UI")]
        public static void RebuildMainMenuUi()
        {
            EnsureModeCovers(forceRepaint: true);

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found. Run Aim/Setup Game Scene first.");
                return;
            }

            var existing = Object.FindAnyObjectByType<MainMenuView>();
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var view = CreateMainMenuUi(canvas.transform);
            var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
            if (bootstrap != null)
                SetObjectField(bootstrap, "mainMenuView", view);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = view.gameObject;
            LanguageYgTextUtility.ConfigureAutoTranslateLanguages();
            Debug.Log("Main Menu UI rebuilt and linked to Bootstrap.");
        }

        static MainMenuView CreateMainMenuUi(Transform canvasTransform)
        {
            var root = new GameObject("MainMenuUI", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvasTransform, false);
            StretchFull(root.GetComponent<RectTransform>());
            var rootGroup = root.GetComponent<CanvasGroup>();

            var dimmer = CreateImage(root.transform, "Dimmer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, DimmerColor, true, raycast: true);
            StretchFull(dimmer.rectTransform);

            var rootPanel = CreatePanel(root.transform, "RootPanel", new Vector2(380f, 250f));
            CreateHeader(rootPanel.transform, "Тренажёр прицела");
            var campaign = CreateWideButton(rootPanel.transform, "CampaignButton", new Vector2(0f, 18f), "Кампания", CampaignFill);
            var modes = CreateWideButton(rootPanel.transform, "ModesButton", new Vector2(0f, -58f), "Выбор режима", ModesFill);

            var modesPanel = CreatePanel(root.transform, "ModesPanel", new Vector2(560f, 390f));
            modesPanel.SetActive(false);
            var modesBody = modesPanel.transform.Find("Body") ?? modesPanel.transform;
            CreateHeader(modesPanel.transform, "Режимы");
            var modesBack = CreateSmallButton(modesPanel.transform, "ModesBackButton", new Vector2(-220f, 150f), "Назад");

            var scrollGo = new GameObject("ModesScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
            scrollGo.transform.SetParent(modesBody, false);
            var scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.offsetMin = new Vector2(12f, 12f);
            scrollRectTransform.offsetMax = new Vector2(-12f, -52f);
            var scrollImage = scrollGo.GetComponent<Image>();
            scrollImage.sprite = BuiltinBackground();
            scrollImage.type = Image.Type.Sliced;
            scrollImage.color = new Color(0.05f, 0.07f, 0.1f, 0.35f);
            scrollImage.raycastTarget = true;
            var mask = scrollGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = new GameObject("ModesGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var gridRect = contentGo.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.sizeDelta = new Vector2(0f, 0f);

            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.cellSize = new Vector2(118f, 92f);
            grid.spacing = new Vector2(10f, 10f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = gridRect;
            scroll.viewport = scrollRectTransform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var cardPrefab = CreateModeCardPrefab(root.transform);
            cardPrefab.SetActive(false);

            var setupPanel = CreatePanel(root.transform, "SetupPanel", new Vector2(400f, 320f));
            setupPanel.SetActive(false);
            var setupTitle = CreateHeader(setupPanel.transform, "Настройка");
            var setupBack = CreateSmallButton(setupPanel.transform, "SetupBackButton", new Vector2(-150f, 115f), "Назад");

            var hitsRow = CreateValueRow(
                setupPanel.transform,
                "HitsRow",
                new Vector2(0f, 35f),
                "Цели",
                out var hitsSlider,
                out var hitsValue,
                out var hitsMinus,
                out var hitsPlus);
            _ = hitsRow;
            var ammoRow = CreateValueRow(
                setupPanel.transform,
                "AmmoRow",
                new Vector2(0f, -30f),
                "Патроны",
                out var ammoSlider,
                out var ammoValue,
                out var ammoMinus,
                out var ammoPlus);

            hitsSlider.minValue = 1;
            hitsSlider.maxValue = 50;
            hitsSlider.wholeNumbers = true;
            hitsSlider.value = 10;
            ammoSlider.minValue = 1;
            ammoSlider.maxValue = 100;
            ammoSlider.wholeNumbers = true;
            ammoSlider.value = 15;

            var play = CreateWideButton(setupPanel.transform, "PlayButton", new Vector2(0f, -110f), "Играть", CampaignFill);

            var view = root.AddComponent<MainMenuView>();
            SetObjectField(view, "rootGroup", rootGroup);
            SetObjectField(view, "rootPanel", rootPanel);
            SetObjectField(view, "modesPanel", modesPanel);
            SetObjectField(view, "setupPanel", setupPanel);
            SetObjectField(view, "campaignButton", campaign);
            SetObjectField(view, "modesButton", modes);
            SetObjectField(view, "modesBackButton", modesBack);
            SetObjectField(view, "setupBackButton", setupBack);
            SetObjectField(view, "playButton", play);
            SetObjectField(view, "hitsMinusButton", hitsMinus);
            SetObjectField(view, "hitsPlusButton", hitsPlus);
            SetObjectField(view, "ammoMinusButton", ammoMinus);
            SetObjectField(view, "ammoPlusButton", ammoPlus);
            SetObjectField(view, "modesGrid", gridRect);
            SetObjectField(view, "setupTitleText", setupTitle);
            SetObjectField(view, "hitsValueText", hitsValue);
            SetObjectField(view, "ammoValueText", ammoValue);
            SetObjectField(view, "hitsSlider", hitsSlider);
            SetObjectField(view, "ammoSlider", ammoSlider);
            SetObjectField(view, "ammoRow", ammoRow);
            SetObjectField(view, "modeCardPrefab", cardPrefab);

            return view;
        }

        static GameObject CreateModeCardPrefab(Transform parent)
        {
            var card = new GameObject("ModeCardPrefab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            card.transform.SetParent(parent, false);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(118f, 92f);
            var layoutElement = card.GetComponent<LayoutElement>();
            layoutElement.minWidth = 118f;
            layoutElement.minHeight = 92f;
            layoutElement.preferredWidth = 118f;
            layoutElement.preferredHeight = 92f;
            var image = card.GetComponent<Image>();
            image.sprite = BuiltinBackground();
            image.type = Image.Type.Sliced;
            image.color = CardFill;
            image.raycastTarget = true;

            var coverGo = new GameObject("Cover", typeof(RectTransform), typeof(Image));
            coverGo.transform.SetParent(card.transform, false);
            var coverRect = coverGo.GetComponent<RectTransform>();
            coverRect.anchorMin = new Vector2(0.5f, 1f);
            coverRect.anchorMax = new Vector2(0.5f, 1f);
            coverRect.pivot = new Vector2(0.5f, 1f);
            coverRect.anchoredPosition = new Vector2(0f, -4f);
            coverRect.sizeDelta = new Vector2(110f, 58f);
            var cover = coverGo.GetComponent<Image>();
            cover.sprite = BuiltinBackground();
            cover.type = Image.Type.Simple;
            cover.preserveAspect = false;
            cover.raycastTarget = false;
            cover.color = Color.white;

            var title = CreateLabel(card.transform, "Title", "Режим", new Vector2(0f, -34f), 12, FontStyle.Bold, TextAnchor.MiddleCenter, SoftText);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            title.rectTransform.pivot = new Vector2(0.5f, 0f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 6f);
            title.rectTransform.sizeDelta = new Vector2(110f, 24f);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            return card;
        }

        static GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            CreateImage(go.transform, "Border", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, PanelBorder, true);
            CreateImage(go.transform, "Body", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(10f, 10f), PanelBody, true);
            return go;
        }

        static Text CreateHeader(Transform panel, string title)
        {
            var body = panel.Find("Body") ?? panel;
            var header = CreateImage(body, "Header", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(((RectTransform)body).sizeDelta.x, 44f), HeaderBar, true);
            header.rectTransform.anchoredPosition = Vector2.zero;
            CreateImage(header.transform, "Accent", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(header.rectTransform.sizeDelta.x, 2f), Accent, true);
            var label = CreateLabel(header.transform, "Title", title, new Vector2(0f, -12f), 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            label.rectTransform.sizeDelta = new Vector2(320f, 28f);
            return label;
        }

        static Button CreateWideButton(Transform panel, string name, Vector2 pos, string label, Color fill)
        {
            var body = panel.Find("Body") ?? panel;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(body, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(280f, 56f);
            var image = go.GetComponent<Image>();
            image.sprite = BuiltinBackground();
            image.type = Image.Type.Sliced;
            image.color = fill;
            image.raycastTarget = true;
            var text = CreateLabel(go.transform, "Label", label, Vector2.zero, 18, FontStyle.Bold, TextAnchor.MiddleCenter, SoftText);
            text.rectTransform.sizeDelta = new Vector2(260f, 36f);
            return go.GetComponent<Button>();
        }

        static Button CreateSmallButton(Transform panel, string name, Vector2 pos, string label)
        {
            var body = panel.Find("Body") ?? panel;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(body, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(84f, 32f);
            var image = go.GetComponent<Image>();
            image.sprite = BuiltinBackground();
            image.type = Image.Type.Sliced;
            image.color = ModesFill;
            image.raycastTarget = true;
            var text = CreateLabel(go.transform, "Label", label, Vector2.zero, 13, FontStyle.Bold, TextAnchor.MiddleCenter, SoftText);
            text.rectTransform.sizeDelta = new Vector2(76f, 24f);
            return go.GetComponent<Button>();
        }

        static GameObject CreateValueRow(
            Transform panel,
            string name,
            Vector2 pos,
            string label,
            out Slider slider,
            out Text valueText,
            out Button minusButton,
            out Button plusButton)
        {
            var body = panel.Find("Body") ?? panel;
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(body, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = pos;
            rowRect.sizeDelta = new Vector2(350f, 56f);

            var labelText = CreateLabel(row.transform, "Label", label, new Vector2(-130f, 12f), 15, FontStyle.Bold, TextAnchor.MiddleLeft, SoftText);
            labelText.rectTransform.sizeDelta = new Vector2(110f, 24f);

            minusButton = CreateStepButton(row.transform, "Minus", new Vector2(40f, 12f), "-");
            plusButton = CreateStepButton(row.transform, "Plus", new Vector2(150f, 12f), "+");

            valueText = CreateLabel(row.transform, "Value", "0", new Vector2(95f, 12f), 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            valueText.rectTransform.sizeDelta = new Vector2(60f, 28f);

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchoredPosition = new Vector2(20f, -14f);
            sliderRect.sizeDelta = new Vector2(260f, 18f);
            var sliderBg = sliderGo.GetComponent<Image>();
            sliderBg.sprite = BuiltinBackground();
            sliderBg.type = Image.Type.Sliced;
            sliderBg.color = new Color(0.16f, 0.20f, 0.28f, 1f);
            sliderBg.raycastTarget = true;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            StretchFull(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(6f, 4f);
            fillAreaRect.offsetMax = new Vector2(-6f, -4f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = BuiltinBackground();
            fill.type = Image.Type.Sliced;
            fill.color = Accent;
            fill.raycastTarget = false;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            StretchFull(handleAreaRect);
            handleAreaRect.offsetMin = new Vector2(8f, 0f);
            handleAreaRect.offsetMax = new Vector2(-8f, 0f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(handleArea.transform, false);
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 18f);
            var handle = handleGo.GetComponent<Image>();
            handle.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            handle.color = Color.white;
            handle.raycastTarget = true;

            slider = sliderGo.GetComponent<Slider>();
            slider.targetGraphic = handle;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.ColorTint;
            return row;
        }

        static Button CreateStepButton(Transform parent, string name, Vector2 pos, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(36f, 28f);
            var image = go.GetComponent<Image>();
            image.sprite = BuiltinBackground();
            image.type = Image.Type.Sliced;
            image.color = StepButtonFill;
            image.raycastTarget = true;
            var text = CreateLabel(go.transform, "Label", label, Vector2.zero, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            text.rectTransform.sizeDelta = new Vector2(30f, 24f);
            return go.GetComponent<Button>();
        }

        static Image CreateImage(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size,
            Color color,
            bool sliced,
            bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = BuiltinBackground();
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
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
            rect.sizeDelta = new Vector2(200f, 30f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.text = content;
            LanguageYgTextUtility.AttachToTextComponent(text, content);
            return text;
        }

        static Sprite BuiltinBackground() =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

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
            if (prop == null)
                return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureModeCovers(bool forceRepaint)
        {
            const string dir = "Assets/Aim/Resources/Modes";
            if (!AssetDatabase.IsValidFolder("Assets/Aim/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Aim"))
                    AssetDatabase.CreateFolder("Assets", "Aim");
                AssetDatabase.CreateFolder("Assets/Aim", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Aim/Resources", "Modes");

            foreach (LevelType type in System.Enum.GetValues(typeof(LevelType)))
            {
                var path = $"{dir}/{type.ToString().ToLowerInvariant()}_icon.png";
                if (forceRepaint || !File.Exists(path))
                    WriteModeCover(path, type);
                ConfigureSprite(path);
            }

            AssetDatabase.Refresh();
        }

        static void WriteModeCover(string path, LevelType type)
        {
            const int w = 256;
            const int h = 144;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            var bgA = CoverBgA(type);
            var bgB = CoverBgB(type);
            var accent = CoverAccent(type);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var t = (x / (float)(w - 1) + y / (float)(h - 1)) * 0.5f;
                var blended = Color.Lerp((Color)bgA, (Color)bgB, t);
                var nx = (x / (float)(w - 1) - 0.5f) * 2f;
                var ny = (y / (float)(h - 1) - 0.5f) * 2f;
                var vig = Mathf.Clamp01(1.15f - Mathf.Sqrt(nx * nx + ny * ny) * 0.55f);
                blended *= vig;
                var c = (Color32)blended;
                c.a = 255;
                pixels[y * w + x] = c;
            }

            PaintModeGlyph(pixels, w, h, type, accent);

            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        static void PaintModeGlyph(Color32[] pixels, int w, int h, LevelType type, Color32 accent)
        {
            var cx = w * 0.5f;
            var cy = h * 0.52f;
            switch (type)
            {
                case LevelType.CharacterHeadshot:
                    FillCircle(pixels, w, h, cx, cy - 8f, 22f, accent);
                    FillCircle(pixels, w, h, cx, cy + 28f, 28f, accent);
                    break;
                case LevelType.FlyingObjects:
                case LevelType.StaticBalls:
                case LevelType.BouncingBalls:
                    FillCircle(pixels, w, h, cx - 34f, cy + 8f, 14f, accent);
                    FillCircle(pixels, w, h, cx + 8f, cy - 16f, 18f, accent);
                    FillCircle(pixels, w, h, cx + 40f, cy + 18f, 12f, accent);
                    break;
                case LevelType.TrackingBall:
                    FillRing(pixels, w, h, cx, cy, 34f, 6f, accent);
                    FillCircle(pixels, w, h, cx, cy, 10f, accent);
                    break;
                case LevelType.TrackingMovers:
                    FillCircle(pixels, w, h, cx - 30f, cy, 12f, accent);
                    FillCircle(pixels, w, h, cx, cy - 10f, 12f, accent);
                    FillCircle(pixels, w, h, cx + 30f, cy + 8f, 12f, accent);
                    FillRect(pixels, w, h, cx - 40f, cy + 24f, 80f, 4f, accent);
                    break;
                case LevelType.FlickTargets:
                    FillCircle(pixels, w, h, cx - 36f, cy + 10f, 10f, accent);
                    FillCircle(pixels, w, h, cx + 36f, cy - 14f, 10f, accent);
                    DrawLine(pixels, w, h, cx - 20f, cy + 4f, cx + 20f, cy - 8f, 3f, accent);
                    break;
                case LevelType.PeekTargets:
                case LevelType.PopupDucks:
                    FillRect(pixels, w, h, cx - 50f, cy - 10f, 28f, 50f, new Color32(30, 40, 55, 255));
                    FillCircle(pixels, w, h, cx - 10f, cy + 4f, 16f, accent);
                    break;
                case LevelType.MovingRails:
                    FillRect(pixels, w, h, cx - 70f, cy - 4f, 140f, 6f, accent);
                    FillCircle(pixels, w, h, cx - 20f, cy - 4f, 14f, Color.white);
                    FillCircle(pixels, w, h, cx + 28f, cy - 4f, 14f, accent);
                    break;
                case LevelType.PriorityTargets:
                    FillCircle(pixels, w, h, cx - 28f, cy, 14f, new Color32(120, 130, 140, 255));
                    FillCircle(pixels, w, h, cx + 22f, cy, 18f, new Color32(255, 210, 70, 255));
                    break;
                case LevelType.PrecisionCircles:
                    FillRing(pixels, w, h, cx, cy, 28f, 3f, accent);
                    FillRing(pixels, w, h, cx, cy, 16f, 3f, accent);
                    FillCircle(pixels, w, h, cx, cy, 4f, accent);
                    break;
                case LevelType.DoubleTap:
                    FillCircle(pixels, w, h, cx, cy, 24f, accent);
                    FillRect(pixels, w, h, cx - 18f, cy + 30f, 36f, 8f, new Color32(80, 200, 120, 255));
                    break;
                case LevelType.ShootingGallery:
                    FillRect(pixels, w, h, cx - 50f, cy - 20f, 24f, 40f, accent);
                    FillRect(pixels, w, h, cx - 10f, cy - 10f, 24f, 30f, accent);
                    FillRect(pixels, w, h, cx + 30f, cy - 26f, 24f, 46f, accent);
                    break;
                case LevelType.CustomHitZones:
                    FillCircle(pixels, w, h, cx, cy, 30f, new Color32(70, 90, 120, 255));
                    FillCircle(pixels, w, h, cx + 10f, cy + 8f, 10f, accent);
                    break;
                default:
                    FillCircle(pixels, w, h, cx, cy, 22f, accent);
                    break;
            }
        }

        static Color32 CoverBgA(LevelType type) => type switch
        {
            LevelType.TrackingBall or LevelType.TrackingMovers => new Color32(18, 42, 58, 255),
            LevelType.PriorityTargets => new Color32(42, 28, 18, 255),
            LevelType.PrecisionCircles => new Color32(20, 28, 48, 255),
            LevelType.CharacterHeadshot => new Color32(36, 20, 28, 255),
            _ => new Color32(16, 28, 44, 255)
        };

        static Color32 CoverBgB(LevelType type) => type switch
        {
            LevelType.TrackingBall or LevelType.TrackingMovers => new Color32(28, 78, 92, 255),
            LevelType.PriorityTargets => new Color32(78, 48, 22, 255),
            LevelType.PrecisionCircles => new Color32(36, 56, 96, 255),
            LevelType.CharacterHeadshot => new Color32(72, 32, 48, 255),
            _ => new Color32(28, 56, 88, 255)
        };

        static Color32 CoverAccent(LevelType type) => type switch
        {
            LevelType.PriorityTargets => new Color32(255, 210, 70, 255),
            LevelType.DoubleTap => new Color32(120, 220, 160, 255),
            LevelType.FlickTargets => new Color32(255, 140, 90, 255),
            _ => new Color32(120, 200, 255, 255)
        };

        static void FillCircle(Color32[] px, int w, int h, float cx, float cy, float r, Color32 color)
        {
            var r2 = r * r;
            var minX = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1));
            var maxX = Mathf.Min(w - 1, Mathf.CeilToInt(cx + r + 1));
            var minY = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1));
            var maxY = Mathf.Min(h - 1, Mathf.CeilToInt(cy + r + 1));
            for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                if (dx * dx + dy * dy <= r2)
                    px[y * w + x] = color;
            }
        }

        static void FillRing(Color32[] px, int w, int h, float cx, float cy, float r, float thickness, Color32 color)
        {
            var outer2 = r * r;
            var inner = Mathf.Max(0f, r - thickness);
            var inner2 = inner * inner;
            var minX = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1));
            var maxX = Mathf.Min(w - 1, Mathf.CeilToInt(cx + r + 1));
            var minY = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1));
            var maxY = Mathf.Min(h - 1, Mathf.CeilToInt(cy + r + 1));
            for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                var d2 = dx * dx + dy * dy;
                if (d2 <= outer2 && d2 >= inner2)
                    px[y * w + x] = color;
            }
        }

        static void FillRect(Color32[] px, int w, int h, float x0, float y0, float rw, float rh, Color32 color)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(x0));
            var maxX = Mathf.Min(w - 1, Mathf.CeilToInt(x0 + rw));
            var minY = Mathf.Max(0, Mathf.FloorToInt(y0));
            var maxY = Mathf.Min(h - 1, Mathf.CeilToInt(y0 + rh));
            for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
                px[y * w + x] = color;
        }

        static void DrawLine(Color32[] px, int w, int h, float x0, float y0, float x1, float y1, float thickness, Color32 color)
        {
            var steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)));
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)Mathf.Max(1, steps);
                FillCircle(px, w, h, Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), thickness, color);
            }
        }

        static void ConfigureSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
        }
    }
}
