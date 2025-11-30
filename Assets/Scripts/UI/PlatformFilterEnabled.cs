using UnityEngine;
using UnityEngine.InputSystem; // For Touchscreen

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

                // Extra: if this WebGL build is running on a touch device (phone/tablet),
                // also mark it as "Mobile".
                if (IsWebGLMobileLike())
                    currentMask |= PlatformMask.Mobile;
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

    /// <summary>
    /// Returns true when running as WebGL on a *touch* device (phone/tablet).
    /// Used to treat WebGL-on-phone as "Mobile" as well.
    /// </summary>
    private bool IsWebGLMobileLike()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // New Input System: if there's a Touchscreen, assume it's a mobile-like device
        return Touchscreen.current != null;
#else
        return false;
#endif
    }
}