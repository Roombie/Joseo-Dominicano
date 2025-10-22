using UnityEngine;
using TMPro;

#if HAS_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
#endif

public class ToggleTextVisual : MonoBehaviour, IToggleVisual
{
    [SerializeField] private TextMeshProUGUI label;

    [Header("Fallback (no localization)")]
    [SerializeField] private string onTextFallback = "Toggle_On";
    [SerializeField] private string offTextFallback = "Toggle_Off";

#if HAS_LOCALIZATION
    [Header("Localization (optional)")]
    [SerializeField] private bool useLocalization = true;
    [SerializeField] private LocalizedString onText;
    [SerializeField] private LocalizedString offText;
    [SerializeField] private LocalizeStringEvent labelLocalizer;
#endif

    private bool _isOn;
    private bool _pressed; // ignored for text but kept for interface compliance

    public void SetOn(bool isOn)       { _isOn = isOn; }
    public void SetPressed(bool press) { _pressed = press; }

    public void RefreshNow()
    {
        if (!label) return;

#if HAS_LOCALIZATION
        if (useLocalization)
        {
            if (labelLocalizer)
            {
                labelLocalizer.StringReference = _isOn ? onText : offText;
                labelLocalizer.RefreshString();
            }
            else
            {
                var handle = (_isOn ? onText : offText).GetLocalizedStringAsync();
                handle.Completed += op =>
                {
                    if (label) label.text = op.Result;
                };
            }
            return;
        }
#endif
        // Fallback without Localization
        label.text = _isOn ? onTextFallback : offTextFallback;
    }
}