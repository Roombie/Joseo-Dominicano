using UnityEngine;
using UnityEngine.InputSystem; // For Touchscreen (if you ever want to use it again)

[System.Flags]
public enum PlatformMask
{
    None        = 0,
    Editor      = 1 << 0,
    Standalone  = 1 << 1,  // Windows/Mac/Linux players
    Mobile      = 1 << 2,  // Android / iOS (+ WebGL on phone via extra check)
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

    [Tooltip("If true, ALL bits in activeOn must be present in currentMask (AND logic). " +
             "If false, ANY bit is enough (OR logic, default).")]
    public bool requireAllFlags = false;

    [Header("Platforms where this should be DISABLED (even if activeOn matches)")]
    [Tooltip("If any of these bits are present in currentMask, the object will be disabled.")]
    public PlatformMask excludeOn = PlatformMask.None;

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

                // If this WebGL build is running on a mobile-like device, also mark it as Mobile.
                if (IsWebGLMobileLike())
                    currentMask |= PlatformMask.Mobile;
                break;
            
            case RuntimePlatform.PS4:
            case RuntimePlatform.PS5:
            case RuntimePlatform.XboxOne:
            case RuntimePlatform.GameCoreXboxSeries:
            case RuntimePlatform.Switch:
                currentMask |= PlatformMask.Console;
                break;
        }

        // First, decide if this platform is allowed according to activeOn + requireAllFlags
        bool allowed;
        if (requireAllFlags)
        {
            // ALL bits in activeOn must be present in currentMask
            allowed = (currentMask & activeOn) == activeOn;
        }
        else
        {
            // ANY bit in activeOn is enough
            allowed = (currentMask & activeOn) != 0;
        }

        // Then, force-disable if any excluded bits are present
        if (allowed && (currentMask & excludeOn) != 0)
        {
            allowed = false;
        }

        return allowed;
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

    /// <summary>
    /// Returns true when running as WebGL on a *mobile-like* device (phone/tablet).
    /// Used to treat WebGL-on-phone as "Mobile" as well.
    /// </summary>
    private bool IsWebGLMobileLike()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Unity's own mobile detection
        if (Application.isMobilePlatform)
            return true;

        if (SystemInfo.deviceType == DeviceType.Handheld)
            return true;

        // Simple heuristic based on screen size (helps in some edge cases)
        if (Mathf.Min(Screen.width, Screen.height) < 900)
            return true;

        return false;
#else
        return false;
#endif
    }
}