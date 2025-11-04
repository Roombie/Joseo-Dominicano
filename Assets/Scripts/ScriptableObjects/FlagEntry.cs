using UnityEngine;

[CreateAssetMenu(fileName = "FlagEntry", menuName = "Localization/Flag Entry")]
public class FlagEntry : ScriptableObject
{
    [Header("Locale Code (Unity Localization)")]
    public string localeCode = "en"; // example: en, es, fr

    [Header("TMP Sprite Name (optional)")]
    public string spriteName; // example: us, es, fr

    [Header("Emoji Fallback")]
    public string emoji = "🌐";

    public bool HasSprite => !string.IsNullOrEmpty(spriteName);
}