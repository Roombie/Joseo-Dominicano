using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;

public class PlatformContinueText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text targetText;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString tapToContinue;      // "Tap to continue" / "Toca para continuar"
    [SerializeField] private LocalizedString pressKeyToContinue; // "Press {0} to continue" / "Presiona {0} para continuar"

    [Header("Input Action")]
    [SerializeField] private InputActionReference continueAction; // Input action used to continue (e.g. Submit/Confirm)

    [Header("Control Name Localization")]
    [SerializeField] private TableReference controlNamesTable = "ControlNames";    // String table that maps effectivePath -> localized control name

    private string lastDeviceLayout = "";
    private InputType lastInputType = InputType.Unknown;

    private bool localizationReady = false;
    private bool inputReady = false;

    private enum InputType
    {
        Unknown,
        KeyboardMouse,
        Gamepad,
        Touch
    }

    private void OnEnable()
    {
        // Auto-assign TMP_Text if not set in the Inspector
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        // Listen to locale changes to update text when language changes
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        // Listen to input events to detect device changes (keyboard/gamepad/touch, etc.)
        InputSystem.onEvent += OnInputDeviceChanged;

        // Start async initialization
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        // Wait until the Localization system finishes its initialization
        yield return LocalizationSettings.InitializationOperation;
        localizationReady = true;

        // Wait until at least one input device exists
        while (!HasInputDevice())
        {
            yield return null;
        }

        inputReady = true;

        // Initial text update once everything is ready
        UpdateLocalizedText();
    }

    /// <summary>
    /// Checks if there is at least one input device available.
    /// </summary>
    private bool HasInputDevice()
    {
        return Keyboard.current != null ||
               Gamepad.current != null ||
               Mouse.current != null ||
               Touchscreen.current != null;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        InputSystem.onEvent -= OnInputDeviceChanged;
    }

    /// <summary>
    /// Called when the selected locale changes.
    /// </summary>
    private void OnLocaleChanged(Locale locale)
    {
        if (localizationReady && inputReady)
            UpdateLocalizedText();
    }

    /// <summary>
    /// Called on any input event. Used to detect device layout changes and refresh the text.
    /// </summary>
    private void OnInputDeviceChanged(InputEventPtr eventPtr, InputDevice device)
    {
        if (!localizationReady) return;
        if (!inputReady) return;
        if (device == null) return;

        string deviceLayout = device.layout;

        // Detect a coarse input type (PC + mobile, no consoles)
        lastInputType = ClassifyInputDevice(device);
        lastDeviceLayout = deviceLayout;

        // Always update when the "kind" of device changes
        UpdateLocalizedText();
    }

    /// <summary>
    /// Maps the device to a simple input type for binding selection.
    /// </summary>
    private InputType ClassifyInputDevice(InputDevice device)
    {
        if (device is Keyboard || device is Mouse)
            return InputType.KeyboardMouse;

        if (device is Gamepad)
            return InputType.Gamepad;

        if (device is Touchscreen)
            return InputType.Touch;

        return InputType.Unknown;
    }

    /// <summary>
    /// Updates the continue text based on platform (mobile vs desktop) and current locale.
    /// </summary>
    private void UpdateLocalizedText()
    {
        if (!localizationReady || !inputReady)
            return;

        if (Application.isMobilePlatform || lastInputType == InputType.Touch)
        {
            // Simple mobile text: no control name argument
            LoadLocalizedText(tapToContinue, null);
        }
        else
        {
            // On desktop we show the control name (e.g. "Space", "A", etc.)
            string controlId = GetControlIdForLocalization();  // raw effectivePath used as table key
            string fallbackReadable = GetReadableBinding();    // human readable string as fallback

            LoadLocalizedTextWithLocalizedControl(pressKeyToContinue, controlId, fallbackReadable);
        }
    }

    /// <summary>
    /// Loads a localized string and optionally formats it with a single argument.
    /// </summary>
    private void LoadLocalizedText(LocalizedString localizedString, string argument)
    {
        var handle = localizedString.GetLocalizedStringAsync();

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                if (!string.IsNullOrEmpty(argument))
                    targetText.text = string.Format(op.Result, argument);
                else
                    targetText.text = op.Result;
            }
        };
    }

    /// <summary>
    /// Loads the main localized string and injects a localized control name as argument.
    /// Falls back to a human-readable control name if no entry is found in the table.
    /// </summary>
    private void LoadLocalizedTextWithLocalizedControl(
        LocalizedString localizedString,
        string controlId,
        string fallbackReadable)
    {
        // If we have no valid control ID, just use the fallback readable name
        if (string.IsNullOrEmpty(controlId))
        {
            LoadLocalizedText(localizedString, fallbackReadable);
            return;
        }

        // Request the string table that stores control names
        var tableHandle = LocalizationSettings.StringDatabase.GetTableAsync(controlNamesTable);

        tableHandle.Completed += tableOp =>
        {
            string controlText = fallbackReadable;

            // If the table loaded successfully, try to find the entry by controlId
            if (tableOp.Status == AsyncOperationStatus.Succeeded && tableOp.Result != null)
            {
                var entry = tableOp.Result.GetEntry(controlId);
                if (entry != null && !string.IsNullOrEmpty(entry.LocalizedValue))
                {
                    controlText = entry.LocalizedValue;
                }
            }

            // Now get the main line (e.g. "Press {0} to continue") and inject controlText
            var lineHandle = localizedString.GetLocalizedStringAsync();

            lineHandle.Completed += lineOp =>
            {
                if (lineOp.Status == AsyncOperationStatus.Succeeded)
                {
                    targetText.text = string.Format(lineOp.Result, controlText);
                }
            };
        };
    }

    /// <summary>
    /// Returns the effectivePath of the binding that best matches the last input type.
    /// Example: "&lt;Keyboard&gt;/space", "&lt;Gamepad&gt;/buttonSouth".
    /// </summary>
    private string GetControlIdForLocalization()
    {
        var action = continueAction?.action;
        if (action == null)
            return string.Empty;

        // First pass: try to match the binding to the last input type
        foreach (var binding in action.bindings)
        {
            if (string.IsNullOrEmpty(binding.effectivePath))
                continue;

            if (BindingMatchesLastInputType(binding.effectivePath))
                return binding.effectivePath;
        }

        // Fallback: just use the first binding with an effective path
        foreach (var binding in action.bindings)
        {
            if (!string.IsNullOrEmpty(binding.effectivePath))
                return binding.effectivePath;
        }

        return string.Empty;
    }

    /// <summary>
    /// Returns a human-readable control name from the binding that best matches
    /// the last input type. Used as fallback when no localized name is found.
    /// </summary>
    private string GetReadableBinding()
    {
        var action = continueAction?.action;
        if (action == null)
            return "Key";

        // First pass: try to match the binding to the last input type
        foreach (var binding in action.bindings)
        {
            if (string.IsNullOrEmpty(binding.effectivePath))
                continue;

            if (BindingMatchesLastInputType(binding.effectivePath))
            {
                return InputControlPath.ToHumanReadableString(
                    binding.effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice
                );
            }
        }

        // Fallback: first valid binding
        foreach (var binding in action.bindings)
        {
            if (string.IsNullOrEmpty(binding.effectivePath))
                continue;

            return InputControlPath.ToHumanReadableString(
                binding.effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice
            );
        }

        return "Key";
    }

    /// <summary>
    /// Checks if the effectivePath looks like it belongs to the last input type
    /// (keyboard/mouse vs gamepad). Since you don't target consoles, gamepad
    /// is treated generically.
    /// </summary>
    private bool BindingMatchesLastInputType(string effectivePath)
    {
        if (lastInputType == InputType.KeyboardMouse)
            return effectivePath.StartsWith("<Keyboard>") || effectivePath.StartsWith("<Mouse>");

        if (lastInputType == InputType.Gamepad)
            return effectivePath.StartsWith("<Gamepad>");

        if (lastInputType == InputType.Touch)
            return effectivePath.StartsWith("<Touchscreen>");

        return false;
    }
}