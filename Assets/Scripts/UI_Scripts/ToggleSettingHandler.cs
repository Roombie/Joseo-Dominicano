using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleSettingHandler : MonoBehaviour, ISettingHandler
{
    [Header("Setting Config")]
    [SettingTypeFilter(SettingType.MusicEnabledKey, SettingType.SoundEnabledKey)]
    [SerializeField] private SettingType settingType;

    [Header("UI")]
    [SerializeField] private Toggle toggle;

    [Header("SFX (optional)")]
    [SerializeField] private AudioClip toggleSound;

    public SettingType SettingType => settingType;

    private bool _currentValue;
    private bool _pressed;
    private ISettingsProvider _provider;
    private List<IToggleVisual> _visuals;

    private void Awake()
    {
        if (!toggle) toggle = GetComponent<Toggle>();
        _provider = GetComponentInParent<ISettingsProvider>();
        _visuals = GetComponentsInChildren<MonoBehaviour>(true).OfType<IToggleVisual>().ToList();

        if (toggle) toggle.onValueChanged.AddListener(HandleUIToggleChanged);
    }

    private void OnEnable()
    {
        ApplyFromSaved();   // sync value from prefs and apply to game once
        NotifyVisuals();
    }

    private void OnDestroy()
    {
        if (toggle) toggle.onValueChanged.RemoveListener(HandleUIToggleChanged);
    }

    private void HandleUIToggleChanged(bool isOn)
    {
        Apply(isOn);
        Save();
        _provider?.ApplySetting(settingType, _currentValue ? 1 : 0);

        if (toggleSound)
            AudioSource.PlayClipAtPoint(toggleSound, Camera.main ? Camera.main.transform.position : Vector3.zero);
    }

    // Optional: wire a Button to this to behave like a press-to-toggle
    public void Toggle()
    {
        Apply(!_currentValue);
        Save();
        _provider?.ApplySetting(settingType, _currentValue ? 1 : 0);
        if (toggle) toggle.SetIsOnWithoutNotify(_currentValue);
    }

    public void Apply(bool value)
    {
        _currentValue = value;
        if (toggle && toggle.isOn != value)
            toggle.SetIsOnWithoutNotify(value);
        NotifyVisuals();
    }

    public void Apply(int index) { /* not used for toggles */ }

    public void ApplyFromSaved()
    {
        // Default ON (1). Change if you want default OFF.
        int def = 1;
        _currentValue = PlayerPrefs.GetInt(SettingsKeys.Get(settingType), def) == 1;

        if (toggle) toggle.SetIsOnWithoutNotify(_currentValue);

        // Apply to the game immediately so UI and runtime match
        _provider?.ApplySetting(settingType, _currentValue ? 1 : 0);
    }

    public void Save()
    {
        PlayerPrefs.SetInt(SettingsKeys.Get(settingType), _currentValue ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void RefreshUI() => NotifyVisuals();

    private void NotifyVisuals()
    {
        if (_visuals == null) return;
        foreach (var v in _visuals)
        {
            v.SetOn(_currentValue);
            v.SetPressed(_pressed);
            v.RefreshNow();
        }
    }
}