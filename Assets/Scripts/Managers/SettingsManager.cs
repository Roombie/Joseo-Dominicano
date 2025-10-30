using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[DefaultExecutionOrder(-100)] // run early
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private Coroutine _applyRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged; // persist locale on any change
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            Instance = null;
        }
    }

    private void Start() => RunApplyForScene();

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RunApplyForScene();

    /// Prevent stacking coroutines when scenes load quickly
    private void RunApplyForScene()
    {
        if (_applyRoutine != null) StopCoroutine(_applyRoutine);
        _applyRoutine = StartCoroutine(ApplyForScene());
    }

    /// Single flow per scene: ensure locale, apply settings, refresh UI
    private IEnumerator ApplyForScene()
    {
        // Make sure Localization is ready
        var init = LocalizationSettings.InitializationOperation;
        if (!init.IsDone) yield return init;

        // Apply saved locale (or keep current if none saved)
        yield return EnsureLocaleFromPrefs();

        // Apply the rest of settings and refresh UI
        ApplyAllFromPrefs();
        RefreshSettingsUI();

        _applyRoutine = null;
    }

    // Locale persistence & application

    private void OnLocaleChanged(Locale newLocale)
    {
        if (newLocale == null) return;
        PlayerPrefs.SetString(SettingsKeys.LanguageKey, newLocale.Identifier.Code);
        PlayerPrefs.Save();
        // UI that uses LocalizeStringEvent/LocalizedString updates automatically
    }

    /// Read PlayerPrefs and set SelectedLocale if needed.
    private IEnumerator EnsureLocaleFromPrefs()
    {
        string fallback = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";
        string code = PlayerPrefs.GetString(SettingsKeys.LanguageKey, fallback);

        var wanted = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (wanted != null && LocalizationSettings.SelectedLocale != wanted)
            LocalizationSettings.SelectedLocale = wanted;

        yield break;
    }

    // Other settings

    public void ApplyAllFromPrefs()
    {
        bool musicOn = PlayerPrefs.GetInt(SettingsKeys.MusicEnabledKey, 1) == 1;
        bool sfxOn   = PlayerPrefs.GetInt(SettingsKeys.SFXEnabledKey, 1) == 1;

        // Do not overwrite saved loudness; only mute/unmute on the mixer.
        AudioManager.Instance?.SetMuted(SettingType.MusicEnabledKey, !musicOn);
        AudioManager.Instance?.SetMuted(SettingType.SFXEnabledKey, !sfxOn);
    }

    // Optional convenience helpers
    public static void Apply(SettingType type, bool on) => Apply(type, on ? 1 : 0);

    public static void Apply(SettingType type, int index)
    {
        if (Instance == null) return;
        Instance.ApplySetting(type, index);
    }

    public void ApplySetting(SettingType type, bool on) => ApplySetting(type, on ? 1 : 0);

    /// 0 = Off (mute), 1 = On (unmute to saved value)
    public void ApplySetting(SettingType type, int index)
    {
        bool turnOn = index != 0;

        switch (type)
        {
            case SettingType.MusicEnabledKey:
                PlayerPrefs.SetInt(SettingsKeys.MusicEnabledKey, turnOn ? 1 : 0);
                AudioManager.Instance?.SetMuted(SettingType.MusicEnabledKey, !turnOn);
                break;

            case SettingType.SFXEnabledKey:
                PlayerPrefs.SetInt(SettingsKeys.SFXEnabledKey, turnOn ? 1 : 0);
                AudioManager.Instance?.SetMuted(SettingType.SFXEnabledKey, !turnOn);
                break;
        }

        PlayerPrefs.Save();
    }

    private void RefreshSettingsUI()
    {
        var toggles = FindObjectsByType<ToggleSettingHandler>(FindObjectsSortMode.None);
        for (int i = 0; i < toggles.Length; i++)
            toggles[i].RefreshUI();
    }
}