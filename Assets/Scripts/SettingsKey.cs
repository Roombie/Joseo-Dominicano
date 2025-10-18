using System.Collections.Generic;

public enum SettingType
{
    MusicEnabledKey,
    SoundEnabledKey,
}

public static class SettingsKeys
{
    private static readonly Dictionary<SettingType, string> keys = new()
    {
        { SettingType.MusicEnabledKey, MusicEnabledKey },
        { SettingType.SoundEnabledKey, SoundEnabledKey },
    };

    public static string Get(SettingType type) => keys.TryGetValue(type, out var value) ? value : type.ToString();

    // Audio
    public const string MusicEnabledKey = "MusicEnabled";
    public const string SoundEnabledKey = "SoundEnabled";
}