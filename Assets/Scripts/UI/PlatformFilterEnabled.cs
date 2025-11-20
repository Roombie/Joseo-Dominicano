using UnityEngine;

[System.Flags]
public enum PlatformMask
{
    None        = 0,
    Editor      = 1 << 0,
    Standalone  = 1 << 1,  // Windows/Mac/Linux players
    Mobile      = 1 << 2,  // Android / iOS
    WebGL       = 1 << 3,
    Console     = 1 << 4,  // PS / Xbox / Switch, etc.
}

public class PlatformFilterEnabler : MonoBehaviour
{
    [Header("Platforms where this should be ENABLED")]
    public PlatformMask activeOn =
        PlatformMask.Editor |
        PlatformMask.Standalone |
        PlatformMask.WebGL;

    [Header("Targets")]
    [Tooltip("If true, this GameObject will be enabled/disabled.")]
    public bool affectSelf = true;

    [Tooltip("Optional extra GameObjects to toggle with the same rule.")]
    public GameObject[] extraTargets;

    private void Awake()
    {
        bool shouldBeActive = IsCurrentPlatformAllowed();
        Apply(shouldBeActive);
    }

    private bool IsCurrentPlatformAllowed()
    {
        PlatformMask currentMask = PlatformMask.None;

#if UNITY_EDITOR
        currentMask |= PlatformMask.Editor;
#endif

        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.LinuxPlayer:
                currentMask |= PlatformMask.Standalone;
                break;

            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
                currentMask |= PlatformMask.Mobile;
                break;

            case RuntimePlatform.WebGLPlayer:
                currentMask |= PlatformMask.WebGL;
                break;

            // Ajusta según los consoles que uses
            case RuntimePlatform.PS4:
            case RuntimePlatform.PS5:
            case RuntimePlatform.XboxOne:
            case RuntimePlatform.GameCoreXboxSeries:
            case RuntimePlatform.Switch:
                currentMask |= PlatformMask.Console;
                break;
        }

        // If ANY of the bits of currentMask are enabled in activeOn → allowed
        return (activeOn & currentMask) != 0;
    }

    private void Apply(bool enable)
    {
        if (affectSelf)
            gameObject.SetActive(enable);

        if (extraTargets == null) return;

        for (int i = 0; i < extraTargets.Length; i++)
        {
            if (extraTargets[i] != null)
                extraTargets[i].SetActive(enable);
        }
    }
}