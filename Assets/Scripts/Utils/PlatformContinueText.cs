using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.InputSystem.LowLevel;

public class PlatformContinueText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text targetText;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString tapToContinue;         // “Tap to continue”
    [SerializeField] private LocalizedString pressKeyToContinue;    // “Press {0} to continue”

    [Header("Input Action")]
    [SerializeField] private InputActionReference continueAction;

    private string lastDevice = "";

    private void OnEnable()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        InputSystem.onEvent += OnInputDeviceChanged;
        UpdateLocalizedText();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        InputSystem.onEvent -= OnInputDeviceChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        UpdateLocalizedText();
    }

    private void OnInputDeviceChanged(InputEventPtr eventPtr, InputDevice device)
    {
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
        bool isMobile = Application.isMobilePlatform;

        if (isMobile)
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
        AsyncOperationHandle<string> handle = localizedString.GetLocalizedStringAsync();
        handle.Completed += op =>
        {
            if (argument != null)
                targetText.text = string.Format(op.Result, argument);
            else
                targetText.text = op.Result;
        };
    }

    private string GetReadableBinding()
    {
        if (continueAction?.action == null)
            return "Key";

        var action = continueAction.action;

        // Try to detect what control scheme is being used
        if (Gamepad.current != null)
        {
            // Try to get gamepad binding
            foreach (var binding in action.bindings)
            {
                if (binding.effectivePath.Contains("Gamepad"))
                {
                    return ToReadable(binding.effectivePath);
                }
            }
        }
        else if (Keyboard.current != null)
        {
            // Try to get keyboard binding
            foreach (var binding in action.bindings)
            {
                if (binding.effectivePath.Contains("Keyboard"))
                {
                    return ToReadable(binding.effectivePath);
                }
            }
        }

        // Fallback to first usable binding
        if (action.bindings.Count > 0)
            return ToReadable(action.bindings[0].effectivePath);

        return "Key";
    }

    private string ToReadable(string path)
    {
        return InputControlPath.ToHumanReadableString(
            path,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }
}