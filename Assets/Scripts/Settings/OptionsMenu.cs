using UnityEngine;
using System.Collections;

public class OptionsMenu : MonoBehaviour, ISettingsProvider
{
    private void Start() => StartCoroutine(RefreshNextFrame());

    private IEnumerator RefreshNextFrame()
    {
        yield return new WaitForEndOfFrame();
        foreach (var toggle in FindObjectsByType<ToggleSettingHandler>(FindObjectsSortMode.None))
            toggle.RefreshUI();
    }

    // Not used by ToggleSettingHandler; keep to satisfy the interface
    public string[] GetOptions(SettingType type) => System.Array.Empty<string>();

    public int GetSavedIndex(SettingType type) => 0;

    // ToggleSettingHandler still calls these:
    public void ApplySetting(SettingType type, bool on) => ApplySetting(type, on ? 1 : 0);

    public void ApplySetting(SettingType type, int index)
    {
        // 0=Off, 1=On -> handled by SettingsManager
        SettingsManager.Apply(type, index);
    }

    public void SaveSetting(SettingType type, int index)
    {
        ApplySetting(type, index);
        PlayerPrefs.Save();
    }
}