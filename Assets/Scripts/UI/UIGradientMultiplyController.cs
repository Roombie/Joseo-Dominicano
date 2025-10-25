using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for Shader Graph-based gradient multiply UI shader.
/// Unity 6.2 (UGUI, TMP via Graphic, SpriteRenderer, MeshRenderer).
/// - Uses exact Shader Graph Reference names (underscored).
/// - Timeline-safe "NoRecord" methods that do NOT write back to serialized fields.
/// - Avoids material recreation in OnValidate (prevents preview snapping).
/// </summary>
public class UIGradientMultiplyController : MonoBehaviour
{
    [Header("Target")]
    public Graphic uiGraphic;            // UGUI/TMP (via Graphic)
    public Renderer targetRenderer;      // SpriteRenderer/MeshRenderer (optional)
    public Material explicitMaterial;    // if you want to drive a shared material

    [Header("Property Name Overrides (exact Reference names)")]
    public string propMainTex        = "_MainTex";
    public string propColorA         = "_Color_A";
    public string propColorB         = "_Color_B";
    public string propGradOffset     = "_Gradient_Offset";
    public string propGradDerivation = "_Gradient_Derivation";
    public string propCustomTex      = "_Custom_Texture";
    public string propCustomSpeed    = "_Custom_Speed";
    public string propUVOverride     = "_UV_Override";

    [Header("Live Values (Inspector)")]
    public Texture mainTexture;
    public Color colorA = Color.white;
    public Color colorB = Color.white;

    [Range(-1f, 1f)] public float gradientOffset = 0f;
    [Range(0f, 1f)]  public float gradientDerivation = 0f;

    public Texture customTexture;
    public Vector2 customSpeed = Vector2.zero;
    public Texture uvOverride;

    [Header("Ranges (for clamping)")]
    public Vector2 gradientOffsetRange = new Vector2(-1f, 1f);
    public Vector2 gradientDerivationRange = new Vector2(0f, 1f);

    [Header("Optional: control the Enum Keyword 'Type'")]
    public bool controlTypeKeyword = false;
    [Tooltip("EXACT keyword names from your Shader Graph Enum Keyword, e.g. TYPE_LINEAR, TYPE_RADIAL, TYPE_CUSTOM")]
    public string[] typeKeywords;
    public int typeIndex = 0;

    [Header("Debug")]
    public bool debugMode = false;

    // cache
    Material _instancedMaterial;
    bool _ownsInstance;
    bool _isAcquiringMaterial = false;

    // IDs
    int idMainTex=-1, idColorA=-1, idColorB=-1, idGradOffset=-1, idGradDerivation=-1, idCustomTex=-1, idCustomSpeed=-1, idUVOverride=-1;

    void Reset() => TryAutoAssign();

    void Awake()
    {
        TryAutoAssign();
        AcquireMaterialInstance();
        ResolveIDs();
        ApplyAll();
    }

    void OnEnable()
    {
        // Ensure material exists and properties are set
        if (_instancedMaterial == null)
        {
            AcquireMaterialInstance();
            ResolveIDs();
        }
        ApplyAll();
    }

    void Start()
    {
        if (debugMode) Debug.Log($"Start - Initializing gradient controller");
        
        // Ensure material is acquired and properties are set
        if (_instancedMaterial == null)
        {
            AcquireMaterialInstance();
        }
        else
        {
            ResolveIDs();
            ApplyAll();
        }
        
        // Double-check after one frame
        StartCoroutine(VerifyAfterFrame());
    }

    IEnumerator VerifyAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        
        if (debugMode)
        {
            var mat = GetMat();
            if (mat != null && idColorA >= 0 && idColorB >= 0)
            {
                var currentColorA = mat.GetColor(idColorA);
                var currentColorB = mat.GetColor(idColorB);
                Debug.Log($"After Frame Verify - Material ColorA: {currentColorA}, ColorB: {currentColorB}");
                Debug.Log($"Expected - Inspector ColorA: {colorA}, ColorB: {colorB}");
            }
        }
    }

    void OnDestroy()
    {
        if (_ownsInstance && _instancedMaterial)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(_instancedMaterial);
            else Destroy(_instancedMaterial);
#else
            Destroy(_instancedMaterial);
#endif
        }
    }

    void OnDisable()
    {
        // Clean up material when component is disabled in editor
#if UNITY_EDITOR
        if (!Application.isPlaying && _ownsInstance && _instancedMaterial)
        {
            DestroyImmediate(_instancedMaterial);
            _instancedMaterial = null;
        }
#endif
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!isActiveAndEnabled) return;

        // Ensure alpha values are clamped in inspector too
        colorA = Clamp01A(colorA);
        colorB = Clamp01A(colorB);

        // keep ranges sane
        if (gradientOffsetRange.x > gradientOffsetRange.y)
            (gradientOffsetRange.x, gradientOffsetRange.y) = (gradientOffsetRange.y, gradientOffsetRange.x);
        if (gradientDerivationRange.x > gradientDerivationRange.y)
            (gradientDerivationRange.x, gradientDerivationRange.y) = (gradientDerivationRange.y, gradientDerivationRange.x);

        // clamp inspector values
        gradientOffset = ClampToRange(gradientOffset, gradientOffsetRange);
        gradientDerivation = ClampToRange(gradientDerivation, gradientDerivationRange);

        TryAutoAssign();

        // Validate property names exist in shader
        if (GetMat() != null && GetMat().shader != null)
        {
            ValidateProperty(propColorA, "_Color_A");
            ValidateProperty(propColorB, "_Color_B");
            ValidateProperty(propGradOffset, "_Gradient_Offset");
            ValidateProperty(propGradDerivation, "_Gradient_Derivation");
        }

        // IMPORTANT: do NOT recreate material here
        ResolveIDs();
        ApplyAll();
    }
    
    public void ForceRefresh()
    {
        if (debugMode) Debug.Log("ForceRefresh - Complete material refresh");
        
        // Clear current instance
        if (_ownsInstance && _instancedMaterial)
        {
            DestroyImmediate(_instancedMaterial);
        }
        _instancedMaterial = null;
        
        // Re-acquire everything
        AcquireMaterialInstance();
        ResolveIDs();
        ApplyAll();
    }

    void ValidateProperty(string propName, string displayName)
    {
        if (!string.IsNullOrEmpty(propName) && 
            GetMat() != null && 
            GetMat().shader != null && 
            ToIDIfExists(GetMat().shader, propName) == -1)
        {
            Debug.LogWarning($"Property '{propName}' ({displayName}) not found in shader", this);
        }
    }
#endif

    // When Timeline/Animation writes serialized fields (only fires for animated serialized fields)
    void OnDidApplyAnimationProperties() => ApplyAll();

    // -------- Public setters (Inspector / your code) --------
    public void SetColorA(Color c) { c.a = Mathf.Clamp01(c.a); colorA = c; SetColor(idColorA, c); }
    public void SetColorB(Color c) { c.a = Mathf.Clamp01(c.a); colorB = c; SetColor(idColorB, c); }

    public void SetGradientOffset(float v)
    {
        gradientOffset = ClampToRange(v, gradientOffsetRange);
        SetFloat(idGradOffset, gradientOffset);
    }

    public void SetGradientDerivation(float v)
    {
        gradientDerivation = ClampToRange(v, gradientDerivationRange);
        SetFloat(idGradDerivation, gradientDerivation);
    }

    public void SetCustomTexture(Texture t) { customTexture = t; if (idCustomTex>=0) GetMat()?.SetTexture(idCustomTex, t); }
    public void SetUVOverride(Texture t)    { uvOverride = t;    if (idUVOverride>=0) GetMat()?.SetTexture(idUVOverride, t); }
    public void SetCustomSpeed(Vector2 v)   { customSpeed = v;   if (idCustomSpeed>=0) GetMat()?.SetVector(idCustomSpeed, v); }
    public void SetTypeIndex(int i)         { typeIndex = i;     ApplyTypeKeyword(); }
    public void SetMainTexture(Texture t)   { mainTexture = t;   if (idMainTex>=0) GetMat()?.SetTexture(idMainTex, t); }

    // -------- Timeline-facing: material-only (NO RECORD) --------
    public void ApplyColorA_NoRecord(Color c)
    {
        if (debugMode) Debug.Log($"ApplyColorA_NoRecord: {c}");
        SetColor(idColorA, c); // do NOT touch 'colorA' field
    }

    public void ApplyColorB_NoRecord(Color c)
    {
        if (debugMode) Debug.Log($"ApplyColorB_NoRecord: {c}");
        SetColor(idColorB, c); // do NOT touch 'colorB' field
    }

    public void ApplyGradientOffset_NoRecord(float v)
    {
        v = ClampToRange(v, gradientOffsetRange);
        SetFloat(idGradOffset, v);
    }

    public void ApplyGradientDerivation_NoRecord(float v)
    {
        v = ClampToRange(v, gradientDerivationRange);
        SetFloat(idGradDerivation, v);
    }

    public void ApplyCustomSpeed_NoRecord(Vector2 v)
    {
        if (idCustomSpeed>=0) GetMat()?.SetVector(idCustomSpeed, v);
    }

    // -------- internals --------
    static float ClampToRange(float v, Vector2 range)
    {
        if (range.x > range.y) (range.x, range.y) = (range.y, range.x);
        return Mathf.Clamp(v, range.x, range.y);
    }

    void TryAutoAssign()
    {
        if (uiGraphic == null) uiGraphic = GetComponent<Graphic>();
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>(); // fine to remain null for UI
    }

    void AcquireMaterialInstance()
    {
        if (_isAcquiringMaterial) return;
        _isAcquiringMaterial = true;

        try

        {
            if (debugMode) Debug.Log($"AcquireMaterialInstance - Starting material acquisition");

            // Store current values before material creation
            var storedColorA = colorA;
            var storedColorB = colorB;

            if (explicitMaterial != null)
            {
                var inst = new Material(explicitMaterial);
                if (debugMode) Debug.Log($"Created material from explicit material: {inst.name}");
                AssignMat(inst, owns: true);

                // Apply current values immediately to override defaults
                ResolveIDs();
                ApplyAll();

                // CRITICAL: Always assign the material to uiGraphic
                if (uiGraphic)
                {
                    uiGraphic.material = _instancedMaterial;
                    if (debugMode) Debug.Log($"Assigned material to UI Graphic");
                }
                return;
            }

            if (uiGraphic != null)
            {
                // keep existing instance if already ours
                if (_instancedMaterial != null && uiGraphic.material == _instancedMaterial)
                {
                    if (debugMode) Debug.Log($"Using existing material instance");
                    return;
                }

                var src = uiGraphic.material;
                if (src == null && uiGraphic.defaultMaterial != null)
                    src = uiGraphic.defaultMaterial;

                var inst = src != null ? new Material(src) : new Material(Shader.Find("Sprites/Default"));
                if (debugMode) Debug.Log($"Created new material instance from source: {src?.name}");

                AssignMat(inst, owns: true);

                // CRITICAL: Always assign the material instance
                uiGraphic.material = _instancedMaterial;

                // Apply current values immediately after assignment
                ResolveIDs();
                ApplyAll();
                return;
            }

            if (targetRenderer != null)
            {
                var src = targetRenderer.sharedMaterial;
                if (src != null)
                {
                    var inst = new Material(src);
                    if (debugMode) Debug.Log($"Created material for renderer: {inst.name}");
                    targetRenderer.sharedMaterial = inst;
                    AssignMat(inst, owns: true);
                    ResolveIDs();
                    ApplyAll();
                }
            }
        }
        finally
        {
            _isAcquiringMaterial = false;
        }
    }
    void AssignMat(Material m, bool owns)
    {
        if (_ownsInstance && _instancedMaterial && _instancedMaterial != m)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(_instancedMaterial);
            else Destroy(_instancedMaterial);
#else
            Destroy(_instancedMaterial);
#endif
        }
        _instancedMaterial = m;
        _ownsInstance = owns;
    }

    public Material GetMat()
    {
        if (_instancedMaterial != null) return _instancedMaterial;
        
        // Safety check to avoid infinite recursion
        if (_isAcquiringMaterial) return null;
        
        AcquireMaterialInstance();
        return _instancedMaterial;
    }

    void ResolveIDs()
    {
        var mat = GetMat();
        if (mat == null || mat.shader == null) 
        {
            if (debugMode) Debug.LogWarning("ResolveIDs: No material or shader found!");
            return;
        }

        if (debugMode) Debug.Log($"Resolving property IDs for shader: {mat.shader.name}");

        idMainTex        = ToIDIfExists(mat.shader, propMainTex);
        idColorA         = ToIDIfExists(mat.shader, propColorA);
        idColorB         = ToIDIfExists(mat.shader, propColorB);
        idGradOffset     = ToIDIfExists(mat.shader, propGradOffset);
        idGradDerivation = ToIDIfExists(mat.shader, propGradDerivation);
        idCustomTex      = ToIDIfExists(mat.shader, propCustomTex);
        idCustomSpeed    = ToIDIfExists(mat.shader, propCustomSpeed);
        idUVOverride     = ToIDIfExists(mat.shader, propUVOverride);

        if (debugMode)
        {
            Debug.Log($"Property IDs resolved - ColorA: {idColorA}, ColorB: {idColorB}");
            Debug.Log($"Current values - ColorA: {colorA}, ColorB: {colorB}");
        }
    }

    static int ToIDIfExists(Shader shader, string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;
        int count = shader.GetPropertyCount();
        for (int i = 0; i < count; i++)
            if (shader.GetPropertyName(i) == name)
                return Shader.PropertyToID(name);
        return -1;
    }

    void ApplyAll()
    {
        var mat = GetMat();
        if (mat == null) 
        {
            if (debugMode) Debug.LogWarning("ApplyAll: No material found!");
            return;
        }

        // Ensure alpha values are clamped before applying
        var clampedColorA = Clamp01A(colorA);
        var clampedColorB = Clamp01A(colorB);
        
        var off = ClampToRange(gradientOffset, gradientOffsetRange);
        var der = ClampToRange(gradientDerivation, gradientDerivationRange);

        if (debugMode) 
        {
            Debug.Log($"ApplyAll - ColorA: {clampedColorA}, ColorB: {clampedColorB}");
            Debug.Log($"ApplyAll - Material instance: {mat.name}, shader: {mat.shader?.name}");
        }

        if (idMainTex >= 0 && mainTexture) 
        {
            mat.SetTexture(idMainTex, mainTexture);
            if (debugMode) Debug.Log($"Applying MainTex: {mainTexture.name}");
        }
        
        if (idColorA >= 0) 
        {
            mat.SetColor(idColorA, clampedColorA);
            if (debugMode) Debug.Log($"Applied ColorA to material: {clampedColorA}");
        }
        else if (debugMode) Debug.LogWarning("ColorA property not found in shader!");
        
        if (idColorB >= 0) 
        {
            mat.SetColor(idColorB, clampedColorB);
            if (debugMode) Debug.Log($"Applied ColorB to material: {clampedColorB}");
        }
        else if (debugMode) Debug.LogWarning("ColorB property not found in shader!");
        if (idGradOffset >= 0) 
        {
            mat.SetFloat(idGradOffset, off);
            if (debugMode) Debug.Log($"Applying GradientOffset: {off}");
        }
        if (idGradDerivation >= 0) 
        {
            mat.SetFloat(idGradDerivation, der);
            if (debugMode) Debug.Log($"Applying GradientDerivation: {der}");
        }
        if (idCustomTex >= 0 && customTexture) 
        {
            mat.SetTexture(idCustomTex, customTexture);
            if (debugMode) Debug.Log($"Applying CustomTexture: {customTexture.name}");
        }
        if (idCustomSpeed >= 0) 
        {
            mat.SetVector(idCustomSpeed, customSpeed);
            if (debugMode) Debug.Log($"Applying CustomSpeed: {customSpeed}");
        }
        if (idUVOverride >= 0 && uvOverride) 
        {
            mat.SetTexture(idUVOverride, uvOverride);
            if (debugMode) Debug.Log($"Applying UVOverride: {uvOverride.name}");
        }

        if (controlTypeKeyword) ApplyTypeKeyword();

        if (uiGraphic) 
        {
            // Ensure the graphic is using our material instance
            if (uiGraphic.material != _instancedMaterial)
            {
                uiGraphic.material = _instancedMaterial;
                if (debugMode) Debug.Log($"Corrected material assignment on UI Graphic");
            }
            
            uiGraphic.SetMaterialDirty();
            uiGraphic.SetVerticesDirty();
            uiGraphic.SetAllDirty();
            
            // Only call RecalculateClipping if it's a MaskableGraphic (Image, Text, TMP_Text, etc.)
            if (uiGraphic is MaskableGraphic maskableGraphic)
            {
                maskableGraphic.RecalculateClipping();
            }

            if (debugMode) Debug.Log($"Refreshed UI Graphic: {uiGraphic.GetType().Name}");
        }

        if (Application.isPlaying && targetRenderer != null)
        {
            targetRenderer.enabled = false;
            targetRenderer.enabled = true;
        }
    }

    static Color Clamp01A(Color c)
    {
        c.a = Mathf.Clamp01(c.a);
        return c;
    }

    void ApplyTypeKeyword()
    {
        var mat = GetMat(); 
        if (mat == null) return;
        if (typeKeywords == null || typeKeywords.Length == 0) return;

        for (int i = 0; i < typeKeywords.Length; i++)
        {
            var kw = typeKeywords[i];
            if (string.IsNullOrEmpty(kw)) continue;
#if UNITY_600_0_OR_NEWER
            mat.SetKeyword(kw, i == Mathf.Clamp(typeIndex, 0, typeKeywords.Length - 1));
#else
            if (i == Mathf.Clamp(typeIndex, 0, typeKeywords.Length - 1)) mat.EnableKeyword(kw);
            else mat.DisableKeyword(kw);
#endif
        }
    }

    void SetFloat(int id, float v)    { if (id >= 0) GetMat()?.SetFloat(id, v); }
    void SetColor(int id, Color v)    { if (id >= 0) GetMat()?.SetColor(id, v); }
    void SetVector(int id, Vector4 v) { if (id >= 0) GetMat()?.SetVector(id, v); }
}