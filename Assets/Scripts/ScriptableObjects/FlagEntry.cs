using UnityEngine;

[CreateAssetMenu(fileName = "FlagEntry", menuName = "Localization/Flag Entry")]
public class FlagEntry : ScriptableObject
{
    [Header("Unity Localization Code (en, es, fr...)")]
    public string localeCode = "en";

    [Header("TMP Sprite Names (from your TMP Sprite Asset)")]
    public string normalSpriteName;   // e.g. "flag_us_normal"
    public string pressedSpriteName;  // e.g. "flag_us_pressed"

    [Header("TMP Sprite Name shown in label (optional)")]
    public string labelSpriteName; // e.g. "flag_us"

    [Header("Emoji fallback if no TMP sprite")]
    public string emoji = "🌐";
}