using UnityEngine;
using UnityEngine.UI;

public class BackgroundUVScaler : MonoBehaviour
{
    [Header("Reference resolution (where UV was tuned)")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Header("UV values at reference resolution")]
    [Tooltip("UV width (W) used at the reference 16:9 resolution")]
    public float baseUVWidth = 4f;

    [Tooltip("UV height (H) used at the reference 16:9 resolution")]
    public float baseUVHeight = 2.14f;

    [SerializeField] private RawImage rawImage;

    private float referenceAspect;
    private float widthRatio;
    private int lastWidth, lastHeight;

    private void Awake()
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();

        referenceAspect = referenceResolution.x / referenceResolution.y;

        // Ideal width at reference aspect if UVs were perfectly proportional
        float idealWidthAtRef = baseUVHeight * referenceAspect;

        // Correction factor to preserve the original designer-tuned baseUVWidth
        widthRatio = idealWidthAtRef > 0f ? baseUVWidth / idealWidthAtRef : 1f;
    }

    private void Start()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        ApplyUV();
    }

    private void Update()
    {
        // Recalculate only when the resolution/aspect ratio actually changes
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            ApplyUV();
        }
    }

    private void ApplyUV()
    {
        if (rawImage == null || rawImage.texture == null)
            return;

        float currentAspect = (float)Screen.width / Screen.height;

        var rect = rawImage.uvRect;

        // Keep the same pattern "height" as at the reference resolution
        rect.height = baseUVHeight;

        // Adjust width based on current aspect ratio, respecting the original baseUVWidth
        float uvWidth = baseUVHeight * currentAspect * widthRatio;
        rect.width = uvWidth;

        // Do NOT modify rect.x or rect.y so the scrolling logic remains intact
        rawImage.uvRect = rect;
    }
}