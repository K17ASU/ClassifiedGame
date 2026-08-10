using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Переключает язык игры и сохраняет выбор игрока.
/// </summary>
public class LanguageSelector : MonoBehaviour
{
    private const string LanguagePreferenceKey =
        "SelectedLanguage";

    private const string RussianLocaleCode = "ru";
    private const string EnglishLocaleCode = "en";

    private bool localizationIsReady;

    private IEnumerator Start()
    {
        yield return LocalizationSettings
            .InitializationOperation;

        localizationIsReady = true;

        LoadSavedLanguage();
    }

    public void SelectRussian()
    {
        SelectLanguage(RussianLocaleCode);
    }

    public void SelectEnglish()
    {
        SelectLanguage(EnglishLocaleCode);
    }

    private void SelectLanguage(string localeCode)
    {
        if (!localizationIsReady)
        {
            Debug.LogWarning(
                "Система локализации ещё загружается."
            );

            return;
        }

        Locale locale =
            LocalizationSettings
                .AvailableLocales
                .GetLocale(localeCode);

        if (locale == null)
        {
            Debug.LogError(
                $"Локаль с кодом {localeCode} не найдена."
            );

            return;
        }

        LocalizationSettings.SelectedLocale =
            locale;

        PlayerPrefs.SetString(
            LanguagePreferenceKey,
            localeCode
        );

        PlayerPrefs.Save();
    }

    private void LoadSavedLanguage()
    {
        if (!PlayerPrefs.HasKey(
                LanguagePreferenceKey))
        {
            return;
        }

        string savedLocaleCode =
            PlayerPrefs.GetString(
                LanguagePreferenceKey
            );

        Locale savedLocale =
            LocalizationSettings
                .AvailableLocales
                .GetLocale(savedLocaleCode);

        if (savedLocale == null)
        {
            Debug.LogWarning(
                $"Сохранённая локаль " +
                $"{savedLocaleCode} не найдена."
            );

            return;
        }

        LocalizationSettings.SelectedLocale =
            savedLocale;
    }
}