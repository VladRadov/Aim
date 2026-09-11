using Aim;
using Aim.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aim.Editor
{
    public static class LevelTipUiSetup
    {
        static readonly Color PanelBody = new(0.09f, 0.11f, 0.16f, 0.98f);
        static readonly Color PanelBorder = new(0.32f, 0.62f, 0.92f, 0.55f);
        static readonly Color PanelShadow = new(0f, 0f, 0f, 0.45f);
        static readonly Color HeaderBar = new(0.14f, 0.22f, 0.34f, 1f);
        static readonly Color Accent = new(0.38f, 0.78f, 1f, 1f);
        static readonly Color SoftText = new(0.78f, 0.84f, 0.92f, 1f);
        static readonly Color ModeText = new(1f, 0.86f, 0.35f, 1f);

        [MenuItem("Aim/Rebuild Level Tip UI")]
        public static void RebuildLevelTipUi()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found. Run Aim/Setup Game Scene first.");
                return;
            }

            var existing = Object.FindAnyObjectByType<LevelTipView>();
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var view = CreateLevelTipUi(canvas.transform);
            var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
            if (bootstrap != null)
            {
                var so = new SerializedObject(bootstrap);
                var prop = so.FindProperty("levelTipView");
                if (prop != null)
                {
                    prop.objectReferenceValue = view;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(bootstrap);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Level Tip UI rebuilt. Save the scene.");
        }

        static LevelTipView CreateLevelTipUi(Transform canvasTransform)
        {
            var root = new GameObject("LevelTipUI", typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
            StretchFull(root.GetComponent<RectTransform>());

            var panelGo = new GameObject("TipPanel", typeof(RectTransform), typeof(CanvasGroup));
            panelGo.transform.SetParent(root.transform, false);
            var panelRoot = panelGo.GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 1f);
            panelRoot.anchorMax = new Vector2(0.5f, 1f);
            panelRoot.pivot = new Vector2(0.5f, 1f);
            panelRoot.sizeDelta = new Vector2(460f, 132f);
            panelRoot.anchoredPosition = new Vector2(0f, -20f);
            var panelGroup = panelGo.GetComponent<CanvasGroup>();

            CreateImage(panelGo.transform, "Shadow", new Vector2(0.5f, 0.5f), new Vector2(4f, -5f), new Vector2(466f, 138f), PanelShadow);
            CreateImage(panelGo.transform, "Border", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 132f), PanelBorder);
            var body = CreateImage(panelGo.transform, "Body", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(448f, 120f), PanelBody);

            var header = CreateImage(body.transform, "Header", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(448f, 30f), HeaderBar);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);

            CreateImage(header.transform, "AccentLine", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(448f, 2f), Accent);

            var title = CreateLabel(
                panelGo.transform,
                "Title",
                "Уровень",
                new Vector2(0f, -8f),
                17,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white,
                new Vector2(424f, 22f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;

            var bodyLabel = CreateLabel(
                panelGo.transform,
                "Body",
                "Описание уровня",
                new Vector2(0f, -36f),
                13,
                FontStyle.Normal,
                TextAnchor.UpperCenter,
                SoftText,
                new Vector2(424f, 54f));
            bodyLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            bodyLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            bodyLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            bodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyLabel.verticalOverflow = VerticalWrapMode.Truncate;
            bodyLabel.lineSpacing = 0.95f;

            var mode = CreateLabel(
                panelGo.transform,
                "Mode",
                "Стрельба · 1 выстрел",
                new Vector2(0f, 10f),
                12,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                ModeText,
                new Vector2(424f, 20f));
            mode.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            mode.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            mode.rectTransform.pivot = new Vector2(0.5f, 0f);
            mode.horizontalOverflow = HorizontalWrapMode.Wrap;
            mode.verticalOverflow = VerticalWrapMode.Truncate;

            var view = root.AddComponent<LevelTipView>();
            SetObjectField(view, "panelGroup", panelGroup);
            SetObjectField(view, "panelRoot", panelRoot);
            SetObjectField(view, "titleText", title);
            SetObjectField(view, "bodyText", bodyLabel);
            SetObjectField(view, "modeText", mode);
            return view;
        }

        static Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color)
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
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
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
            Color color,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
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
