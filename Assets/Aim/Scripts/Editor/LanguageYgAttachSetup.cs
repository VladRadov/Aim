using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aim.Editor
{
    /// <summary>
    /// Attaches YG LanguageYG via reflection (YG lives in Assembly-CSharp).
    /// </summary>
    public static class LanguageYgTextUtility
    {
        static readonly Regex DynamicTextRegex = new(
            @"^[\d\s:%+./\-—,xX×]+$",
            RegexOptions.Compiled);

        static readonly Dictionary<string, string> RuToEn = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Уровень пройден"] = "Level complete",
            ["Уровень провален"] = "Level failed",
            ["Новый рекорд!"] = "New record!",
            ["Новый рекорд точности!"] = "New accuracy record!",
            ["Новый рекорд времени!"] = "New time record!",
            ["Новые рекорды!"] = "New records!",
            ["Точность"] = "Accuracy",
            ["Время"] = "Time",
            ["Монеты"] = "Coins",
            ["Выстрелы"] = "Shots",
            ["Попадания"] = "Hits",
            ["Ещё раз"] = "Retry",
            ["Дальше"] = "Next",
            ["В меню"] = "Menu",
            ["Кампания"] = "Campaign",
            ["Выбор режима"] = "Select mode",
            ["Режимы"] = "Modes",
            ["Настройка"] = "Setup",
            ["Назад"] = "Back",
            ["Играть"] = "Play",
            ["Цели"] = "Targets",
            ["Патроны"] = "Ammo",
            ["Режим"] = "Mode",
            ["Тренажёр прицела"] = "Aim Trainer",
            ["НАСТРОЙКИ"] = "SETTINGS",
            ["МАГАЗИН"] = "SHOP",
            ["ОРУЖИЕ"] = "WEAPONS",
            ["Купить"] = "Buy",
            ["Экипировать"] = "Equip",
            ["Экипировано"] = "Equipped",
            ["Куплено"] = "Owned",
            ["Надето"] = "Equipped",
            ["Заблокировано"] = "Locked",
            ["Оружие"] = "Weapon",
            ["Звуки"] = "Sounds",
            ["Музыка"] = "Music",
            ["Описание уровня"] = "Level description",
            ["Стрельба · 1 выстрел"] = "Shooting · 1 shot",
            ["Стрельба · 1 выстрел · без полоски HP"] = "Shooting · 1 shot · no HP bar",
            ["Без стрельбы · полоска HP · держи прицел"] = "No shooting · HP bar · hold the crosshair",
            ["Стрельба · несколько попаданий · есть полоска HP"] = "Shooting · several hits · HP bar",
            ["Стрельба"] = "Shooting",
            ["Без стрельбы"] = "No shooting",
            ["Уровень"] = "Level",
            ["Нужно монет"] = "Need coins",
            ["Хедшоты"] = "Headshots",
            ["Летящие цели"] = "Flying targets",
            ["Зоны попадания"] = "Hit zones",
            ["Тир"] = "Gallery",
            ["Прыгающие шары"] = "Bouncing balls",
            ["Ведение цели"] = "Tracking ball",
            ["Слежка за роем"] = "Tracking swarm",
            ["Статичные шары"] = "Static balls",
            ["Флик-шоты"] = "Flick shots",
            ["Выглядывание"] = "Peek targets",
            ["Рельсы"] = "Rails",
            ["Поп-ап мишени"] = "Popup targets",
            ["Приоритет"] = "Priority",
            ["Точные круги"] = "Precision circles",
            ["Мультихит"] = "Multi-hit",
        };

        static readonly Dictionary<string, string> EnToRu = new(StringComparer.OrdinalIgnoreCase);

        static LanguageYgTextUtility()
        {
            foreach (var pair in RuToEn)
            {
                // Never register identity pairs (en == ru) — they leave English in the ru field.
                if (string.Equals(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!EnToRu.ContainsKey(pair.Value))
                    EnToRu[pair.Value] = pair.Key;
            }
        }

        static Type LanguageYgType =>
            Type.GetType("YG.LanguageLegacy.LanguageYG, Assembly-CSharp");

        static Type TmpTextType =>
            Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");

        [MenuItem("Aim/Attach LanguageYG To All Texts")]
        public static void AttachToAllTextsInOpenScenes()
        {
            ConfigureAutoTranslateLanguages();

            var count = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                    count += AttachInHierarchy(root.transform);
            }

            count += AttachInPrefabs("Assets/Aim");

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log(
                $"LanguageYG attached/updated on {count} text object(s). Languages: ru + en. " +
                "For missing EN fill fields or run Tools/YG2/Auto Translate Langs/Auto Localization Masse.");
        }

        static int AttachInPrefabs(string folder)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            var count = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var before = count;
                    count += AttachInHierarchy(root.transform);
                    if (count != before)
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return count;
        }

        public static void AttachToTextComponent(Component textComponent, string preferredSource = null)
        {
            if (textComponent == null)
                return;

            AttachToGameObject(textComponent.gameObject, preferredSource ?? ReadText(textComponent));
        }

        public static void AttachAsDynamicText(Component textComponent, string preferredSource = null)
        {
            if (textComponent == null)
                return;

            AttachToGameObject(textComponent.gameObject, preferredSource ?? ReadText(textComponent), forceChangeOnlyFont: true);
        }

        public static int AttachInHierarchy(Transform root)
        {
            if (root == null)
                return 0;

            var count = 0;
            var texts = root.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                AttachToGameObject(texts[i].gameObject, texts[i].text);
                count++;
            }

            var tmpType = TmpTextType;
            if (tmpType != null)
            {
                var tmps = root.GetComponentsInChildren(tmpType, true);
                for (var i = 0; i < tmps.Length; i++)
                {
                    var component = tmps[i] as Component;
                    if (component == null)
                        continue;

                    AttachToGameObject(component.gameObject, ReadText(component));
                    count++;
                }
            }

            return count;
        }

        static void AttachToGameObject(GameObject go, string sourceText, bool forceChangeOnlyFont = false)
        {
            var type = LanguageYgType;
            if (type == null)
            {
                Debug.LogWarning(
                    "LanguageYG type not found. Import AutoTranslateLangs and ensure Unity finishes compiling.");
                return;
            }

            var lang = go.GetComponent(type);
            if (lang == null)
                lang = go.AddComponent(type);

            var serialize = type.GetMethod("Serialize");
            serialize?.Invoke(lang, null);

            sourceText ??= string.Empty;
            var isDynamic = forceChangeOnlyFont || IsDynamicText(sourceText) || IsRuntimeDrivenLabel(go.name);
            ResolveRuEn(sourceText, out var ru, out var en);

            var so = new SerializedObject(lang);
            SetBool(so, "componentTextField", true);
            SetBool(so, "changeOnlyFont", isDynamic);
            // Keep Russian as the authored/default text (project default lang is ru).
            SetString(so, "text", isDynamic ? sourceText : ru);
            SetString(so, "ru", ru);
            SetString(so, "en", en);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!isDynamic)
                ApplyVisibleText(go, ru);

            EditorUtility.SetDirty(lang);
            EditorUtility.SetDirty(go);
        }

        static bool IsRuntimeDrivenLabel(string objectName)
        {
            return objectName is "Name" or "Price" or "Record";
        }

        static void ApplyVisibleText(GameObject go, string value)
        {
            var uiText = go.GetComponent<Text>();
            if (uiText != null)
            {
                uiText.text = value;
                EditorUtility.SetDirty(uiText);
            }

            var tmpType = TmpTextType;
            if (tmpType == null)
                return;

            var tmp = go.GetComponent(tmpType);
            if (tmp == null)
                return;

            var textProp = tmpType.GetProperty("text");
            textProp?.SetValue(tmp, value);
            EditorUtility.SetDirty(tmp);
        }

        static void ResolveRuEn(string source, out string ru, out string en)
        {
            ru = source ?? string.Empty;
            en = source ?? string.Empty;

            if (string.IsNullOrWhiteSpace(source))
                return;

            var trimmed = source.Trim();

            // Source already Russian → map to English.
            if (RuToEn.TryGetValue(trimmed, out var mappedEn) &&
                !string.Equals(trimmed, mappedEn, StringComparison.OrdinalIgnoreCase))
            {
                ru = trimmed;
                en = mappedEn;
                return;
            }

            // Source English → map to Russian.
            if (EnToRu.TryGetValue(trimmed, out var mappedRu))
            {
                ru = mappedRu;
                en = trimmed;
                return;
            }

            // Cyrillic source without dictionary entry: keep ru, leave en as-is until translated.
            if (ContainsCyrillic(trimmed))
            {
                ru = trimmed;
                en = trimmed;
            }
        }

        static bool ContainsCyrillic(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c >= '\u0400' && c <= '\u04FF')
                    return true;
            }

            return false;
        }

        public static bool IsDynamicText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            var trimmed = text.Trim();
            if (trimmed == "x2" || trimmed == "✕" || trimmed == "—")
                return true;

            return DynamicTextRegex.IsMatch(trimmed);
        }

        static string ReadText(Component component)
        {
            if (component is Text uiText)
                return uiText.text;

            var textProp = component.GetType().GetProperty("text");
            if (textProp != null && textProp.PropertyType == typeof(string))
                return textProp.GetValue(component) as string ?? string.Empty;

            return string.Empty;
        }

        static void SetString(SerializedObject so, string name, string value)
        {
            var prop = so.FindProperty(name);
            if (prop != null)
                prop.stringValue = value ?? string.Empty;
        }

        static void SetBool(SerializedObject so, string name, bool value)
        {
            var prop = so.FindProperty(name);
            if (prop != null)
                prop.boolValue = value;
        }

        public static void ConfigureAutoTranslateLanguages()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/PluginYourGames/Resources/SettingsYG2.asset");
            if (asset == null)
                return;

            var so = new SerializedObject(asset);
            var langs = so.FindProperty("AutoTranslateLangs.languages");
            if (langs == null)
                return;

            void SetLang(string code, bool enabled)
            {
                var p = langs.FindPropertyRelative(code);
                if (p != null)
                    p.boolValue = enabled;
            }

            SetLang("ru", true);
            SetLang("en", true);
            SetLang("tr", false);
            SetLang("az", false);
            SetLang("be", false);
            SetLang("he", false);
            SetLang("hy", false);
            SetLang("ka", false);
            SetLang("et", false);
            SetLang("fr", false);
            SetLang("kk", false);
            SetLang("ky", false);
            SetLang("lt", false);
            SetLang("lv", false);
            SetLang("ro", false);
            SetLang("tg", false);
            SetLang("tk", false);
            SetLang("uk", false);
            SetLang("uz", false);
            SetLang("es", false);
            SetLang("pt", false);
            SetLang("ar", false);
            SetLang("id", false);
            SetLang("ja", false);
            SetLang("it", false);
            SetLang("de", false);
            SetLang("hi", false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}
