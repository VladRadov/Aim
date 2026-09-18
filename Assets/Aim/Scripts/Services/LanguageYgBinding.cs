using System;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Services
{
    /// <summary>
    /// Writes ru/en into YG LanguageYG without referencing Assembly-CSharp.
    /// </summary>
    public static class LanguageYgBinding
    {
        static readonly Type LanguageYgType =
            Type.GetType("YG.LanguageLegacy.LanguageYG, Assembly-CSharp");

        public static void SetTranslations(Component textComponent, string ru, string en)
        {
            if (textComponent == null)
                return;

            ru ??= string.Empty;
            en ??= ru;

            if (LanguageYgType != null)
            {
                var lang = textComponent.GetComponent(LanguageYgType);
                if (lang == null)
                    lang = textComponent.gameObject.AddComponent(LanguageYgType);

                LanguageYgType.GetMethod("Serialize")?.Invoke(lang, null);
                LanguageYgType.GetField("ru")?.SetValue(lang, ru);
                LanguageYgType.GetField("en")?.SetValue(lang, en);
                LanguageYgType.GetField("text")?.SetValue(lang, ru);
                LanguageYgType.GetField("changeOnlyFont")?.SetValue(lang, false);
                LanguageYgType.GetMethod("SwitchLanguage", Type.EmptyTypes)?.Invoke(lang, null);
                return;
            }

            if (textComponent is Text uiText)
                uiText.text = ru;
        }
    }
}
