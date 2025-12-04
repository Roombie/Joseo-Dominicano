using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class PlatformUIPositioner : MonoBehaviour
{
    // Los mismos flags que usas en PlatformFilterEnabler
    public PlatformMask standaloneMask = PlatformMask.Standalone | PlatformMask.Editor;
    public PlatformMask mobileMask     = PlatformMask.Mobile;
    public PlatformMask webGLMask      = PlatformMask.WebGL;

    [System.Serializable]
    public struct UIConfig
    {
        public UIAnchorPreset anchorPreset;
        public Vector2 anchoredPosition;
    }

    [Header("Config Standalone / Editor (PC)")]
    public UIConfig standaloneConfig;

    [Header("Config Mobile (Android / iOS / WebGL en móvil)")]
    public UIConfig mobileConfig;

    [Header("Config WebGL (PC navegador)")]
    public UIConfig webGLConfig;

#if UNITY_EDITOR
    [Header("Editor Simulation (igual que PlatformFilterEnabler)")]
    [Tooltip("Si está activo, se usa editorSimulatedMask en lugar de la plataforma real.")]
    public bool simulateInEditor = false;

    [Tooltip(
        "Máscara simulada en el Editor.\n" +
        "- WebGL                → WebGL en PC\n" +
        "- Mobile               → móvil nativo\n" +
        "- WebGL | Mobile       → WebGL abierto en un teléfono"
    )]
    public PlatformMask editorSimulatedMask = PlatformMask.Standalone;
#endif

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        // En Play y en Editor queremos aplicar la posición correcta
        ApplyForCurrentPlatform();
    }

    private void OnEnable()
    {
        rect = GetComponent<RectTransform>();
        ApplyForCurrentPlatform();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;

        rect = GetComponent<RectTransform>();
        ApplyForCurrentPlatform();
    }

    private void ApplyForCurrentPlatform()
    {
        PlatformMask current = GetCurrentPlatformMask();

        // Importante el orden:
        // 1) Si tiene Mobile → mobileConfig (esto incluye WebGL|Mobile)
        if ((current & mobileMask) != 0)
        {
            ApplyConfig(mobileConfig);
        }
        // 2) Si no es mobile pero sí WebGL → webGLConfig
        else if ((current & webGLMask) != 0)
        {
            ApplyConfig(webGLConfig);
        }
        // 3) Si no, Standalone
        else if ((current & standaloneMask) != 0)
        {
            ApplyConfig(standaloneConfig);
        }
        // Si nada matchea, no tocamos nada (se queda como esté)
    }

    private void ApplyConfig(UIConfig config)
    {
        if (rect == null) rect = GetComponent<RectTransform>();

        ApplyAnchorPreset(rect, config.anchorPreset);
        rect.anchoredPosition = config.anchoredPosition;
    }

    // ======== Construir currentMask igual que PlatformFilterEnabler ========
    private PlatformMask GetCurrentPlatformMask()
    {
        PlatformMask currentMask = PlatformMask.None;

#if UNITY_EDITOR
        if (simulateInEditor)
        {
            // Usar la máscara simulada + Editor
            currentMask = editorSimulatedMask | PlatformMask.Editor;
        }
        else
        {
            // Comportamiento normal en Editor: solo marcamos Editor
            currentMask |= PlatformMask.Editor;
        }
#else
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

                // Igual que IsWebGLMobileLike(): si es WebGL en móvil, también Mobile
#if UNITY_WEBGL
                if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld)
                    currentMask |= PlatformMask.Mobile;
#endif
                break;

            case RuntimePlatform.PS4:
            case RuntimePlatform.PS5:
            case RuntimePlatform.XboxOne:
            case RuntimePlatform.GameCoreXboxSeries:
            case RuntimePlatform.Switch:
                currentMask |= PlatformMask.Console;
                break;
        }
#endif

        return currentMask;
    }

    // === Anchor Presets básicos (los del cuadrito) ===
    public enum UIAnchorPreset
    {
        BottomLeft,
        BottomCenter,
        BottomRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        TopLeft,
        TopCenter,
        TopRight
    }

    private void ApplyAnchorPreset(RectTransform rt, UIAnchorPreset preset)
    {
        Vector2 min, max, pivot;

        switch (preset)
        {
            case UIAnchorPreset.BottomLeft:
                min = max = new Vector2(0f, 0f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            case UIAnchorPreset.BottomCenter:
                min = max = new Vector2(0.5f, 0f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            case UIAnchorPreset.BottomRight:
                min = max = new Vector2(1f, 0f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            case UIAnchorPreset.MiddleLeft:
                min = max = new Vector2(0f, 0.5f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            case UIAnchorPreset.MiddleCenter:
                min = max = new Vector2(0.5f, 0.5f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            case UIAnchorPreset.MiddleRight:
                min = max = new Vector2(1f, 0.5f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            case UIAnchorPreset.TopLeft:
                min = max = new Vector2(0f, 1f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            case UIAnchorPreset.TopCenter:
                min = max = new Vector2(0.5f, 1f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            case UIAnchorPreset.TopRight:
                min = max = new Vector2(1f, 1f);
                pivot = new Vector2(0.5f, 0.5f);
                break;

            default:
                min = rt.anchorMin;
                max = rt.anchorMax;
                pivot = rt.pivot;
                break;
        }

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
    }
}