using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageCyclerTMP : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI References")]
    public TMP_Text label; // the label next to the flag
    public FlagRegistry flagRegistry; // drag your registry here

    private bool isPressed = false;

    private async void OnEnable()
    {
        await LocalizationSettings.InitializationOperation.Task;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshLabel(isPressed);
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _) => RefreshLabel(isPressed);

    // Switches to the next language
    public async void CycleLanguage()
    {
        await LocalizationSettings.InitializationOperation.Task;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales.Count == 0) return;

        var current = LocalizationSettings.SelectedLocale;
        int index = Mathf.Max(0, locales.IndexOf(current));
        var next = locales[(index + 1) % locales.Count];

        LocalizationSettings.SelectedLocale = next;

        PlayerPrefs.SetString(SettingsKeys.LanguageKey, next.Identifier.Code);
        PlayerPrefs.Save();

        RefreshLabel(isPressed);
    }

    // Refresh label according to state (normal/pressed)
    private void RefreshLabel(bool pressed)
    {
        if (label == null || flagRegistry == null)
            return;

        var locale = LocalizationSettings.SelectedLocale;
        if (locale == null) return;

        string code = locale.Identifier.Code;
        var entry = flagRegistry.Get(code);
        if (entry == null)
        {
            label.text = code.ToUpperInvariant();
            return;
        }

        string displayName =
            locale.Identifier.CultureInfo != null ?
            locale.Identifier.CultureInfo.NativeName.ToUpperInvariant() :
            locale.LocaleName.ToUpperInvariant();

        // choose the TMP sprite name according to pressed state
        string spriteName = pressed ? entry.pressedSpriteName : entry.normalSpriteName;

        // TMP Sprite Asset must contain the sprite name!
        if (!string.IsNullOrEmpty(spriteName) && label.spriteAsset != null)
        {
            label.text = $"<sprite name=\"{spriteName}\"> {displayName}";
            return;
        }

        // if no TMP sprite name -> fallback to label icon sprite name
        if (!string.IsNullOrEmpty(entry.labelSpriteName) && label.spriteAsset != null)
        {
            label.text = $"<sprite name=\"{entry.labelSpriteName}\"> {displayName}";
            return;
        }

        // final fallback emoji
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