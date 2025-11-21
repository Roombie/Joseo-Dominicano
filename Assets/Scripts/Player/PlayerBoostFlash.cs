using System.Collections;
using UnityEngine;

public class PlayerBoostFlash : MonoBehaviour
{
    [Header("Target Object")]
    [Tooltip("GameObject that contains the SpriteRenderer and boost material. If null, this GameObject is used.")]
    [SerializeField] private GameObject boostObject;

    [Header("Sprite (Overlay)")]
    [Tooltip("SpriteRenderer of the boost object (overlay). If null, it will be fetched from boostObject.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Color / Flash")]
    [SerializeField] private Color boostColor = Color.cyan;
    [SerializeField] private float flashInDuration = 0.08f;
    [SerializeField] private float flashOutDuration = 0.25f;
    [SerializeField] private AnimationCurve flashCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Flip Sync")]
    [Tooltip("Sprite original del jugador, del cual copiaremos el flip.")]
    [SerializeField] private SpriteRenderer sourceSprite;
    [SerializeField] private bool syncFlipX = false;
    [SerializeField] private bool syncFlipY = true;


    [Header("Call Protection")]
    [SerializeField] private bool preventMultipleCalls = true;
    [SerializeField] private float minTimeBetweenCalls = 0.05f;

    private Coroutine _flashRoutine;
    private bool _isInitialized;
    private bool _boostActive;
    private float _lastCallTime;
    private Color _transparentBoost;

    private void Awake()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        if (!_isInitialized || spriteRenderer == null || sourceSprite == null)
            return;

        if (syncFlipX)
            spriteRenderer.flipX = sourceSprite.flipX;

        if (syncFlipY)
            spriteRenderer.flipY = sourceSprite.flipY;
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        // Default to this GameObject if none is assigned
        if (boostObject == null)
            boostObject = gameObject;

        // Try to grab the SpriteRenderer from the target object if not set
        if (spriteRenderer == null && boostObject != null)
            spriteRenderer = boostObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"{nameof(PlayerBoostFlash)}: No SpriteRenderer found on boostObject.", this);
            enabled = false;
            return;
        }

        // Base transparent boost color
        _transparentBoost = boostColor;
        _transparentBoost.a = 0f;

        spriteRenderer.color = _transparentBoost;
        spriteRenderer.enabled = false;

        _isInitialized = true;
    }

    /// <summary>
    /// One-shot pulse (e.g. for dash, pickup, etc.).
    /// </summary>
    public void PlayBoostFlash()
    {
        if (!_isInitialized || spriteRenderer == null) return;

        if (preventMultipleCalls)
        {
            float dt = Time.time - _lastCallTime;
            if (dt < minTimeBetweenCalls) return;
            _lastCallTime = Time.time;
        }

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(PulseRoutine());
    }

    /// <summary>
    /// Call when sprint starts (OnSprintStart).
    /// </summary>
    public void StartBoostFlash()
    {
        if (!_isInitialized || spriteRenderer == null) return;

        // Already active: don't restart if we're protecting from multiple calls
        if (_boostActive && preventMultipleCalls)
            return;

        _boostActive = true;

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(BoostStartRoutine());
    }

    /// <summary>
    /// Call when sprint ends (OnSprintEnd).
    /// </summary>
    public void StopBoostFlash()
    {
        if (!_isInitialized || spriteRenderer == null) return;

        if (!_boostActive && preventMultipleCalls)
            return;

        _boostActive = false;

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(BoostEndRoutine());
    }

    // --- COROUTINES ---

    private IEnumerator PulseRoutine()
    {
        spriteRenderer.enabled = true;

        // Fade in
        yield return StartCoroutine(
            FlashPhaseRoutine(_transparentBoost, boostColor, flashInDuration)
        );

        // Fade out
        yield return StartCoroutine(
            FlashPhaseRoutine(boostColor, _transparentBoost, flashOutDuration)
        );

        spriteRenderer.enabled = false;
        _flashRoutine = null;
    }

    private IEnumerator BoostStartRoutine()
    {
        spriteRenderer.enabled = true;

        // Fade in and stay at boost color until StopBoostFlash
        yield return StartCoroutine(
            FlashPhaseRoutine(_transparentBoost, boostColor, flashInDuration)
        );

        spriteRenderer.color = boostColor;
    }

    private IEnumerator BoostEndRoutine()
    {
        // Fade out to transparent
        yield return StartCoroutine(
            FlashPhaseRoutine(boostColor, _transparentBoost, flashOutDuration)
        );

        spriteRenderer.enabled = false;
        _flashRoutine = null;
    }

    private IEnumerator FlashPhaseRoutine(Color startColor, Color endColor, float duration)
    {
        if (duration <= 0f)
        {
            spriteRenderer.color = endColor;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float curved = flashCurve.Evaluate(progress);

            spriteRenderer.color = Color.Lerp(startColor, endColor, curved);
            yield return null;
        }

        spriteRenderer.color = endColor;
    }

    private void OnDisable()
    {
        if (spriteRenderer == null) return;

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }

        spriteRenderer.color = _transparentBoost;
        spriteRenderer.enabled = false;
        _boostActive = false;
    }
}