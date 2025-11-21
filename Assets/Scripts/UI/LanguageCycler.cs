using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageCycler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI References")]
    public TMP_Text label;
    public FlagRegistry flagRegistry;

    private bool isPressed = false;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshLabel(false);
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _) => RefreshLabel(isPressed);

    public void CycleLanguage()
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales.Count == 0) return;

        var current = LocalizationSettings.SelectedLocale;
        int index = Mathf.Max(0, locales.IndexOf(current));
        var next = locales[(index + 1) % locales.Count];

        // Cambio directo sin await
        LocalizationSettings.SelectedLocale = next;

        // Guardar preferencia
        PlayerPrefs.SetString(SettingsKeys.LanguageKey, next.Identifier.Code);
        PlayerPrefs.Save();

        RefreshLabel(isPressed);
    }

    private void RefreshLabel(bool pressed)
    {
        if (label == null || flagRegistry == null) return;

        var locale = LocalizationSettings.SelectedLocale;
        if (locale == null) return;

        string code = locale.Identifier.Code;
        var entry = flagRegistry.Get(code);
        if (entry == null)
        {
            label.text = code.ToUpperInvariant();
            return;
        }

        string displayName = locale.Identifier.CultureInfo != null ? 
            locale.Identifier.CultureInfo.NativeName.ToUpperInvariant() : 
            locale.LocaleName.ToUpperInvariant();

        string spriteName = pressed ? entry.pressedSpriteName : entry.normalSpriteName;

        if (!string.IsNullOrEmpty(spriteName) && label.spriteAsset != null)
        {
            label.text = $"<sprite name=\"{spriteName}\"> {displayName}";
            return;
        }

        if (!string.IsNullOrEmpty(entry.labelSpriteName) && label.spriteAsset != null)
        {
            label.text = $"<sprite name=\"{entry.labelSpriteName}\"> {displayName}";
            return;
        }

        if (!string.IsNullOrEmpty(entry.emoji))
        {
            label.text = $"{entry.emoji} {displayName}";
            return;
        }

        label.text = displayName;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        RefreshLabel(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        RefreshLabel(false);
    }
}