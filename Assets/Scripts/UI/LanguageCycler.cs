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

    [Header("Spam control")]
    [Tooltip("Tiempo minimo entre cambios de idioma (en segundos, tiempo real).")]
    public float minChangeInterval = 0.4f;

    private bool isPressed = false;
    private bool isChangingLanguage = false;
    private float lastChangeTime = -999f;

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

    // Llamado por el OnClick del boton en la UI
    public async void CycleLanguage()
    {
        // Si ya hay un cambio en curso, ignoramos este click
        if (isChangingLanguage)
            return;

        // Pequeño cooldown para no disparar cambios demasiado seguidos
        if (Time.unscaledTime - lastChangeTime < minChangeInterval)
            return;

        isChangingLanguage = true;
        lastChangeTime = Time.unscaledTime;

        await LocalizationSettings.InitializationOperation.Task;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales.Count == 0)
        {
            isChangingLanguage = false;
            return;
        }

        var current = LocalizationSettings.SelectedLocale;
        int index = Mathf.Max(0, locales.IndexOf(current));
        var next = locales[(index + 1) % locales.Count];

        // Tu forma original de cambiar idioma (tu version del package no tiene ChangeLocaleAsync)
        LocalizationSettings.SelectedLocale = next;

        PlayerPrefs.SetString(SettingsKeys.LanguageKey, next.Identifier.Code);
        PlayerPrefs.Save();

        RefreshLabel(isPressed);

        // Dejamos un pelin de margen antes de permitir otro cambio
        // para que el sistema de variants/Addressables termine tranquilo.
        await System.Threading.Tasks.Task.Delay(
            Mathf.RoundToInt(minChangeInterval * 1000f)
        );

        isChangingLanguage = false;
    }

    // Refresh label according to state (normal/pressed)
    // ⬇️ ESTE ES TU CODIGO ORIGINAL, NO LO TOCAMOS
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