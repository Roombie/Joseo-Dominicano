using System.Collections.Generic;

public enum SettingType
{
    MusicEnabledKey,
    SFXEnabledKey,
    LanguageKey
}

public static class SettingsKeys
{
    private static readonly Dictionary<SettingType, string> keys = new()
    {
        { SettingType.MusicEnabledKey, MusicEnabledKey },
        { SettingType.SFXEnabledKey, SFXEnabledKey },
        { SettingType.LanguageKey, LanguageKey }
    };

    public static string Get(SettingType type) => keys.TryGetValue(type, out var value) ? value : type.ToString();

    // Audio
    public const string MusicEnabledKey = "MusicEnabled";
    public const string SFXEnabledKey = "SFXEnabled";
    public const string LanguageKey = "Language";
}