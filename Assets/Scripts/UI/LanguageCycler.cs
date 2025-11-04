using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageCycler : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    public FlagRegistry flagRegistry;

    private async void OnEnable()
    {
        await LocalizationSettings.InitializationOperation.Task;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshLabel();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        RefreshLabel();
    }

    public async void CycleLanguage()
    {
        await LocalizationSettings.InitializationOperation.Task;

        var locales = LocalizationSettings.AvailableLocales?.Locales;
        if (locales == null || locales.Count == 0) return;

        var cur = LocalizationSettings.SelectedLocale;
        int i = Mathf.Max(0, locales.IndexOf(cur));

        var next = locales[(i + 1) % locales.Count];
        LocalizationSettings.SelectedLocale = next;

        PlayerPrefs.SetString(SettingsKeys.LanguageKey, next.Identifier.Code);
        PlayerPrefs.Save();

        RefreshLabel();
    }

    private void RefreshLabel()
    {
        var locale = LocalizationSettings.SelectedLocale;
        if (label == null)
        {
            Debug.LogWarning("[LanguageCycler] Label is not assigned.");
            return;
        }

        if (locale == null)
        {
            label.text = "—";
            return;
        }

        string code = locale.Identifier.Code;
        string name = locale.Identifier.CultureInfo != null ?
                      locale.Identifier.CultureInfo.NativeName :
                      locale.LocaleName;

        var entry = flagRegistry != null ? flagRegistry.Get(code) : null;

        // Try TMP font sprite asset
        if (entry != null && entry.HasSprite && label.spriteAsset != null)
        {
            label.text = $"<sprite name=\"{entry.spriteName}\"> {name.ToUpperInvariant()}";
            return;
        }

        // Try emoji fallback
        if (entry != null && !string.IsNullOrEmpty(entry.emoji))
        {
            label.text = $"{entry.emoji} {name.ToUpperInvariant()}";
            return;
        }

        // Pure fallback text
        label.text = name.ToUpperInvariant();
    }
}