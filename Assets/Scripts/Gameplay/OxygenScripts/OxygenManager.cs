using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public abstract class OxygenableBehaviour : MonoBehaviour
{
    public event System.Action<bool> isMovingEvent;
    protected void SetMoveEvent(bool value)
    {
        isMovingEvent?.Invoke(value);
    }
    public event System.Action<bool> isSprintingEvent;
    protected void SetSprintingEvent(bool value)
    {
        isSprintingEvent?.Invoke(value);
    }
}

public class OxygenManager : MonoBehaviour
{
    [SerializeField] private FloatVariable oxygenLevel;
    [SerializeField] private FloatVariable oxygenDepletionRate;
    [SerializeField] private FloatVariable maxTotalOxygen;

    [SerializeField] private float movingDepletionChange = 1.2f;
    [SerializeField] private float sprintingDepletionChange = 1.7f;
    [SerializeField] private float currentMoveDepletionModifier = 1;

    [Header("Hazards")]
    [SerializeField] float hazardDamage = 10f;

    [Header("Low Oxygen Warning")]
    [SerializeField] private UIGradientMultiplyController oxygenWarningController;
    [SerializeField] private float lowOxygenThreshold = 0.3f; // 30%
    
    [Header("Gradient Pulse Settings")]
    [SerializeField] private float normalGradientOffset = 0f;
    [SerializeField] private float pulseGradientOffset = 0.5f;
    [SerializeField] private float normalGradientDerivation = 0f;
    [SerializeField] private float pulseGradientDerivation = 0.3f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Low Oxygen Music")]
    [SerializeField] private AudioClip lowOxygenMusic;
    [SerializeField] private float musicFadeDuration = 0.5f;
    [SerializeField] private float lowOxygenMusicVolume = 0.8f;

    private bool isLowOxygenWarningActive = false;
    private float currentPulseValue = 0f;
    private AudioSource lowOxygenAudioSource;
    private Coroutine musicFadeCoroutine;

    [SerializeField] private Slider oxygenBar;
    [SerializeField] private TextMeshProUGUI oxygenLvlText;

    [SerializeField] private OxygenableBehaviour oxygenable;
    [SerializeField] private PlayerHazardListener hazard;

    [SerializeField] private bool consumingOxygen = false;
    public UnityEvent onOxygenDepleted;

    bool ismoving = false;
    float currentDepletionRate = 1;

    void Start()
    {
        if (oxygenable == null)
            oxygenable = FindAnyObjectByType<OxygenableBehaviour>();

        // Crear AudioSource para la música de bajo oxígeno
        lowOxygenAudioSource = gameObject.AddComponent<AudioSource>();
        lowOxygenAudioSource.clip = lowOxygenMusic;
        lowOxygenAudioSource.volume = 0f;
        lowOxygenAudioSource.loop = true;
        lowOxygenAudioSource.playOnAwake = false;

        ResetOxygen();
        currentDepletionRate = oxygenDepletionRate.value;
        currentMoveDepletionModifier = movingDepletionChange;

        // Initialize warning system - deactivate at start
        if (oxygenWarningController != null)
        {
            oxygenWarningController.gameObject.SetActive(false);
            // Reset gradient to normal values
            oxygenWarningController.SetGradientOffset(normalGradientOffset);
            oxygenWarningController.SetGradientDerivation(normalGradientDerivation);
        }
    }

    private void OnEnable()
    {
        if (oxygenable != null)
        {
            oxygenable.isMovingEvent += UpdateBoolIfMoving;
            oxygenable.isSprintingEvent += ChangeDepletionRateIfSprinting;
            if (hazard != null)
            {
                hazard.OnHazardCollided.AddListener((Hazard) => ChangeOxygenLevel(-Hazard.damage));
            }
            else
            {
                Debug.LogWarning("No PlayerHazardListener found in the scene. Please add one to detect hazard collisions.");
            }
        }
    }

    private void OnDisable()
    {
        if (oxygenable != null)
        {
            oxygenable.isMovingEvent -= UpdateBoolIfMoving;
            oxygenable.isSprintingEvent -= ChangeDepletionRateIfSprinting;
            if (hazard != null)
            {
                hazard.OnHazardCollided.RemoveListener((Hazard) => ChangeOxygenLevel(-Hazard.damage));
            }
        }
        
        // Ensure warning is deactivated when OxygenManager is disabled
        if (oxygenWarningController != null)
        {
            oxygenWarningController.gameObject.SetActive(false);
        }
        
        // Stop low oxygen music
        StopLowOxygenMusic();
    }

    private void Update()
    {
        UpdateLowOxygenWarning();
    }

    public void ResetOxygen()
    {
        StopAllCoroutines();
        consumingOxygen = false;
        oxygenLevel.value = maxTotalOxygen.value;
        oxygenLvlText.text = oxygenLevel.value.ToString();
        UpdateOxygenBar();
        
        // Deactivate warning effect and reset gradient
        if (oxygenWarningController != null)
        {
            oxygenWarningController.gameObject.SetActive(false);
            oxygenWarningController.SetGradientOffset(normalGradientOffset);
            oxygenWarningController.SetGradientDerivation(normalGradientDerivation);
        }
        
        // Stop low oxygen music
        StopLowOxygenMusic();
        
        isLowOxygenWarningActive = false;
        currentPulseValue = 0f;
    }

    public void PauseOxygen()
    {
        StopAllCoroutines();
        consumingOxygen = false;
        UpdateOxygenBar();
        
        // Deactivate warning when paused
        if (oxygenWarningController != null)
        {
            oxygenWarningController.gameObject.SetActive(false);
        }
        
        // Stop low oxygen music
        StopLowOxygenMusic();
    }

    public void ConsumeOxygen()
    {
        consumingOxygen = true;
        StartCoroutine(OxygenDepletion());
    }

    private IEnumerator OxygenDepletion()
    {
        while (consumingOxygen)
        {
            yield return new WaitForSecondsRealtime(currentDepletionRate);
            ChangeOxygenLevel(-1);
        }
    }

    public void BoostOxygenTotal(float increment)
    {
        oxygenLevel.value += increment;
        oxygenLvlText.text = oxygenLevel.value.ToString();
        maxTotalOxygen.value += increment;
    }

    public void ChangeOxygenLevel(float value)
    {
        if (oxygenLevel.value > 0)
        {
            oxygenLevel.value = oxygenLevel.value + value > maxTotalOxygen.value ? maxTotalOxygen.value : oxygenLevel.value += value;
            oxygenLvlText.text = oxygenLevel.value.ToString();
            UpdateOxygenBar();
        }
        else if (consumingOxygen)
        {
            StopAllCoroutines();
            consumingOxygen = false;
            onOxygenDepleted.Invoke();
        }
    }

    void UpdateOxygenBar()
    {
        oxygenBar.value = oxygenLevel.value / maxTotalOxygen.value;
    }

    private void UpdateLowOxygenWarning()
    {
        if (!consumingOxygen || oxygenWarningController == null) return;

        float oxygenRatio = oxygenLevel.value / maxTotalOxygen.value;
        bool isLowOxygen = oxygenRatio <= lowOxygenThreshold;

        // Handle low oxygen music - inmediato sin intervals
        if (isLowOxygen && lowOxygenMusic != null)
        {
            if (!lowOxygenAudioSource.isPlaying)
            {
                StartLowOxygenMusic();
            }
            else if (lowOxygenAudioSource.volume < lowOxygenMusicVolume)
            {
                // Si ya está sonando pero no a volumen completo, hacer fade in
                StartLowOxygenMusic();
            }
        }
        else if (!isLowOxygen && lowOxygenAudioSource.isPlaying)
        {
            StopLowOxygenMusic();
        }

        if (isLowOxygen)
        {
            // Activate the warning object if not already active
            if (!oxygenWarningController.gameObject.activeInHierarchy)
            {
                oxygenWarningController.gameObject.SetActive(true);
            }

            // Calculate pulse effect based on how low oxygen is
            float severity = 1f - (oxygenRatio / lowOxygenThreshold); // 0 to 1 based on how far below threshold
            
            // Create pulsing effect using sine wave
            currentPulseValue = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f * severity;
            
            // Apply pulse to gradient offset and derivation
            float pulsedOffset = Mathf.Lerp(normalGradientOffset, pulseGradientOffset, currentPulseValue);
            float pulsedDerivation = Mathf.Lerp(normalGradientDerivation, pulseGradientDerivation, currentPulseValue);
            
            oxygenWarningController.SetGradientOffset(pulsedOffset);
            oxygenWarningController.SetGradientDerivation(pulsedDerivation);
            
            isLowOxygenWarningActive = true;
        }
        else if (isLowOxygenWarningActive)
        {
            // Fade out pulse effect when oxygen is restored
            currentPulseValue = Mathf.Lerp(currentPulseValue, 0f, Time.deltaTime * 2f);
            
            float pulsedOffset = Mathf.Lerp(normalGradientOffset, pulseGradientOffset, currentPulseValue);
            float pulsedDerivation = Mathf.Lerp(normalGradientDerivation, pulseGradientDerivation, currentPulseValue);
            
            oxygenWarningController.SetGradientOffset(pulsedOffset);
            oxygenWarningController.SetGradientDerivation(pulsedDerivation);
            
            if (currentPulseValue < 0.01f)
            {
                // Deactivate the object when fully faded out
                oxygenWarningController.gameObject.SetActive(false);
                oxygenWarningController.SetGradientOffset(normalGradientOffset);
                oxygenWarningController.SetGradientDerivation(normalGradientDerivation);
                isLowOxygenWarningActive = false;
                currentPulseValue = 0f;
            }
        }
        else if (oxygenWarningController.gameObject.activeInHierarchy)
        {
            // Safety check: if we're not in low oxygen state but object is active, deactivate it
            oxygenWarningController.gameObject.SetActive(false);
        }
    }

    private void StartLowOxygenMusic()
    {
        if (lowOxygenMusic != null && lowOxygenAudioSource != null)
        {
            // Detener fade anterior si existe
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }

            // Iniciar reproducción si no está sonando
            if (!lowOxygenAudioSource.isPlaying)
            {
                lowOxygenAudioSource.Play();
            }

            // Fade in inmediato
            musicFadeCoroutine = StartCoroutine(FadeMusic(lowOxygenAudioSource, lowOxygenAudioSource.volume, lowOxygenMusicVolume, musicFadeDuration));
        }
    }

    private void StopLowOxygenMusic()
    {
        if (lowOxygenAudioSource != null && lowOxygenAudioSource.isPlaying)
        {
            // Detener fade anterior si existe
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }

            // Fade out inmediato
            musicFadeCoroutine = StartCoroutine(FadeMusic(lowOxygenAudioSource, lowOxygenAudioSource.volume, 0f, musicFadeDuration, true));
        }
    }

    private IEnumerator FadeMusic(AudioSource audioSource, float fromVolume, float toVolume, float duration, bool stopAfterFade = false)
    {
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float volume = Mathf.Lerp(fromVolume, toVolume, timer / duration);
            audioSource.volume = volume;
            yield return null;
        }
        
        audioSource.volume = toVolume;
        
        if (stopAfterFade && toVolume <= 0f)
        {
            audioSource.Stop();
        }
    }

    void UpdateBoolIfMoving(bool movingState)
    {
        ismoving = movingState;
        ChangeDepletionRateIfMoving();
    }

    public void ChangeDepletionRateIfMoving()
    {
        if (ismoving)
        {
            TemporaryChangeOxygenDepletionRate(currentMoveDepletionModifier);
        }
        else
        {
            currentDepletionRate = oxygenDepletionRate.value;
        }
    }

    public void ChangeDepletionRateIfSprinting(bool isSprinting)
    {
        if (isSprinting)
        {
            currentMoveDepletionModifier = sprintingDepletionChange;
        }
        else
        {
            currentMoveDepletionModifier = movingDepletionChange;
        }
        ChangeDepletionRateIfMoving();
    }

    void TemporaryChangeOxygenDepletionRate(float value)
    {
        currentDepletionRate = oxygenDepletionRate.value * value;
    }
}