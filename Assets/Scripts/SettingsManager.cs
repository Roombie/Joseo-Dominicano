using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start() => StartCoroutine(ApplyAfterFirstFrame());

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyAllFromPrefs();
        RefreshSettingsUI();
    }

    private IEnumerator ApplyAfterFirstFrame()
    {
        yield return new WaitForEndOfFrame();
        ApplyAllFromPrefs();
        RefreshSettingsUI();
    }

    /// Apply enabled flags -> mixer mute state (On = unmute to saved volume, Off = mute)
    public void ApplyAllFromPrefs()
    {
        bool musicOn = PlayerPrefs.GetInt(SettingsKeys.MusicEnabledKey, 1) == 1;
        bool sfxOn   = PlayerPrefs.GetInt(SettingsKeys.SoundEnabledKey,  1) == 1;

        // Don’t overwrite saved loudness; just mute/unmute at the mixer.
        AudioManager.Instance?.SetMuted(SettingType.MusicEnabledKey, !musicOn);
        AudioManager.Instance?.SetMuted(SettingType.SoundEnabledKey,  !sfxOn);
    }

    // Static convenience
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

            case SettingType.SoundEnabledKey:
                PlayerPrefs.SetInt(SettingsKeys.SoundEnabledKey, turnOn ? 1 : 0);
                AudioManager.Instance?.SetMuted(SettingType.SoundEnabledKey, !turnOn);
                break;
        }

        PlayerPrefs.Save();
    }

    private void RefreshSettingsUI()
    {
        foreach (var toggle in FindObjectsByType<ToggleSettingHandler>(FindObjectsSortMode.None))
            toggle.RefreshUI();
    }
}