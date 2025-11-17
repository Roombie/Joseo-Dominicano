using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;

public class PlatformContinueText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text targetText;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString tapToContinue;
    [SerializeField] private LocalizedString pressKeyToContinue;

    [Header("Input Action")]
    [SerializeField] private InputActionReference continueAction;

    private string lastDevice = "";
    private bool localizationReady = false;
    private bool inputReady = false;

    private void OnEnable()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        InputSystem.onEvent += OnInputDeviceChanged;

        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        // Wait for localization
        yield return LocalizationSettings.InitializationOperation;
        localizationReady = true;

        // Wait until an input device actually exists
        while (!HasInputDevice())
        {
            yield return null;
        }

        inputReady = true;

        UpdateLocalizedText();
    }

    private bool HasInputDevice()
    {
        return Keyboard.current != null ||
               Gamepad.current != null ||
               Mouse.current != null ||
               Touchscreen.current != null;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        InputSystem.onEvent -= OnInputDeviceChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (localizationReady && inputReady)
            UpdateLocalizedText();
    }

    private void OnInputDeviceChanged(InputEventPtr eventPtr, InputDevice device)
    {
        if (!localizationReady) return;
        if (!inputReady) return;

        if (device == null) return;
        string deviceName = device.layout;

        if (deviceName != lastDevice)
        {
            lastDevice = deviceName;
            UpdateLocalizedText();
        }
    }

    private void UpdateLocalizedText()
    {
        if (!localizationReady || !inputReady)
            return;

        if (Application.isMobilePlatform)
        {
            LoadLocalizedText(tapToContinue, null);
        }
        else
        {
            string control = GetReadableBinding();
            LoadLocalizedText(pressKeyToContinue, control);
        }
    }

    private void LoadLocalizedText(LocalizedString localizedString, string argument)
    {
        var handle = localizedString.GetLocalizedStringAsync();

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                if (argument != null)
                    targetText.text = string.Format(op.Result, argument);
                else
                    targetText.text = op.Result;
            }
        };
    }

    private string GetReadableBinding()
    {
        var action = continueAction?.action;
        if (action == null)
            return "Key";

        foreach (var binding in action.bindings)
        {
            if (!string.IsNullOrEmpty(binding.effectivePath))
            {
                return InputControlPath.ToHumanReadableString(
                    binding.effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice
                );
            }
        }

        return "Key";
    }
}