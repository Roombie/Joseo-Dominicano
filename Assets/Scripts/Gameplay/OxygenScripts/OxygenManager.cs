using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.Audio;

public abstract class OxygenableBehaviour : MonoBehaviour
{
    public event System.Action<bool> isMovingEvent;
    protected void SetMoveEvent(bool value) => isMovingEvent?.Invoke(value);

    public event System.Action<bool> isSprintingEvent;
    protected void SetSprintingEvent(bool value) => isSprintingEvent?.Invoke(value);
}

public class OxygenManager : MonoBehaviour
{
    [SerializeField] private FloatVariable oxygenLevel;
    [SerializeField] private FloatVariable oxygenDepletionRate;
    [SerializeField] private FloatVariable maxTotalOxygen;

    [SerializeField] private float movingDepletionChange = 1.2f;
    [SerializeField] private float sprintingDepletionChange = 1.7f;
    private float currentMoveDepletionModifier = 1;

    [Header("Hazards")]
    [SerializeField] private float hazardDamage = 10f;
    [SerializeField] private AudioClip hazardDamageSound;
    [SerializeField] private AudioClip loseObjectSound;
    [SerializeField] private float invincibilityDuration = 0.5f;
    [SerializeField] private SpriteRenderer playerSprite;

    private bool isInvincible = false;
    private float invincibleTimer = 0f;
    private float flickerTimer = 0f;
    private bool flickerState = true;

    [Header("Low Oxygen Warning")]
    [SerializeField] private UIGradientMultiplyController oxygenWarningController;
    [SerializeField] private float lowOxygenThreshold = 0.3f;

    [Header("Gradient Pulse Settings")]
    [SerializeField] private float normalGradientOffset = 0f;
    [SerializeField] private float pulseGradientOffset = 0.5f;
    [SerializeField] private float normalGradientDerivation = 0f;
    [SerializeField] private float pulseGradientDerivation = 0.3f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Low Oxygen Music")]
    [SerializeField] private AudioClip lowOxygenMusic;
    [SerializeField] private AudioMixerGroup lowOxygenMusicMixerGroup;
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

    private bool ismoving = false;
    private float currentDepletionRate = 1;


    void Start()
    {
        if (oxygenable == null)
            oxygenable = FindAnyObjectByType<OxygenableBehaviour>();

        lowOxygenAudioSource = gameObject.AddComponent<AudioSource>();
        lowOxygenAudioSource.clip = lowOxygenMusic;
        lowOxygenAudioSource.volume = 0f;
        lowOxygenAudioSource.loop = true;
        lowOxygenAudioSource.playOnAwake = false;

        if (lowOxygenMusicMixerGroup != null)
            lowOxygenAudioSource.outputAudioMixerGroup = lowOxygenMusicMixerGroup;

        ResetOxygen();
        currentDepletionRate = oxygenDepletionRate.value;
        currentMoveDepletionModifier = movingDepletionChange;

        if (oxygenWarningController != null)
        {
            oxygenWarningController.gameObject.SetActive(false);
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
                hazard.OnHazardCollided.AddListener(OnHazardHit);
            }
            else Debug.LogWarning("No PlayerHazardListener found in the scene.");
        }
    }


    private void OnDisable()
    {
        if (oxygenable != null)
        {
            oxygenable.isMovingEvent -= UpdateBoolIfMoving;
            oxygenable.isSprintingEvent -= ChangeDepletionRateIfSprinting;

            if (hazard != null)
                hazard.OnHazardCollided.RemoveListener(OnHazardHit);
        }

        if (oxygenWarningController != null)
            oxygenWarningController.gameObject.SetActive(false);

        StopLowOxygenMusicImmediate();
        ClearInvincibility();
    }


    private void Update()
    {
        UpdateInvincibility();
        UpdateLowOxygenWarning();
    }


    // Damage

    private void OnHazardHit(Hazard hazardObj)
    {
        if (!GameManager.Instance.inShift)
            return;

        if (isInvincible)
            return;

        if (hazardDamageSound != null)
            AudioManager.Instance.Play(hazardDamageSound, SoundCategory.SFX);

        ChangeOxygenLevel(-hazardObj.damage);
        if (Random.value <= 0.50f)
        {
            GameManager.Instance.LoseSackSpaceAndItems(1, 4);
            if (loseObjectSound != null)    
                AudioManager.Instance.Play(loseObjectSound, SoundCategory.SFX);
        }
        StartInvincibility();
    }


    private void StartInvincibility()
    {
        isInvincible = true;
        invincibleTimer = invincibilityDuration;
        flickerTimer = 0f;
        flickerState = true;

        if (playerSprite != null)
            playerSprite.enabled = true;
    }


    private void ClearInvincibility()
    {
        isInvincible = false;
        invincibleTimer = 0f;
        flickerTimer = 0f;
        flickerState = true;

        if (playerSprite != null)
            playerSprite.enabled = true;
    }


    // Flicker logic
    private void UpdateInvincibility()
    {
        if (!isInvincible) return;

        invincibleTimer -= Time.deltaTime;

        if (invincibleTimer <= 0f)
        {
            ClearInvincibility();
            return;
        }

        flickerTimer += Time.deltaTime;

        if (flickerTimer >= 0.1f)
        {
            flickerTimer = 0f;
            flickerState = !flickerState;

            if (playerSprite != null)
                playerSprite.enabled = flickerState;
        }
    }


    // Oxygen system

    public void ResetOxygen()
    {
        StopAllCoroutines();
        consumingOxygen = false;

        oxygenLevel.value = maxTotalOxygen.value;
        oxygenLvlText.text = oxygenLevel.value.ToString();

        UpdateOxygenBar();
        ClearInvincibility();

        if (oxygenWarningController != null)
        {
            oxygenWarningController.gameObject.SetActive(false);
            oxygenWarningController.SetGradientOffset(normalGradientOffset);
            oxygenWarningController.SetGradientDerivation(normalGradientDerivation);
        }

        StopLowOxygenMusic();
        isLowOxygenWarningActive = false;
        currentPulseValue = 0f;
    }


    public void PauseOxygen()
    {
        StopAllCoroutines();
        consumingOxygen = false;

        UpdateOxygenBar();

        if (oxygenWarningController != null)
            oxygenWarningController.gameObject.SetActive(false);

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
        maxTotalOxygen.value += increment;
        oxygenLvlText.text = oxygenLevel.value.ToString();
    }


    public void ChangeOxygenLevel(float value)
    {
        if (oxygenLevel.value > 0)
        {
            oxygenLevel.value =
                Mathf.Clamp(oxygenLevel.value + value, 0, maxTotalOxygen.value);

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


    private void UpdateOxygenBar()
    {
        oxygenBar.value = oxygenLevel.value / maxTotalOxygen.value;
    }


    // Low oxygen warning

    private void StopLowOxygenMusicImmediate()
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }

        if (lowOxygenAudioSource != null)
        {
            lowOxygenAudioSource.Stop();
            lowOxygenAudioSource.volume = 0f;
        }
    }

    private void UpdateLowOxygenWarning()
    {
        if (!consumingOxygen || oxygenWarningController == null) return;

        float oxygenRatio = oxygenLevel.value / maxTotalOxygen.value;
        bool isLow = oxygenRatio <= lowOxygenThreshold;

        // music
        if (isLow) StartLowOxygenMusic();
        else StopLowOxygenMusic();

        // UI pulse
        if (isLow)
        {
            if (!oxygenWarningController.gameObject.activeInHierarchy)
                oxygenWarningController.gameObject.SetActive(true);

            float severity = 1f - (oxygenRatio / lowOxygenThreshold);
            currentPulseValue = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f * severity;

            float offset = Mathf.Lerp(normalGradientOffset, pulseGradientOffset, currentPulseValue);
            float deriv = Mathf.Lerp(normalGradientDerivation, pulseGradientDerivation, currentPulseValue);

            oxygenWarningController.SetGradientOffset(offset);
            oxygenWarningController.SetGradientDerivation(deriv);

            isLowOxygenWarningActive = true;
        }
        else if (isLowOxygenWarningActive)
        {
            currentPulseValue = Mathf.Lerp(currentPulseValue, 0f, Time.deltaTime * 2f);

            float offset = Mathf.Lerp(normalGradientOffset, pulseGradientOffset, currentPulseValue);
            float deriv = Mathf.Lerp(normalGradientDerivation, pulseGradientDerivation, currentPulseValue);

            oxygenWarningController.SetGradientOffset(offset);
            oxygenWarningController.SetGradientDerivation(deriv);

            if (currentPulseValue < 0.01f)
            {
                oxygenWarningController.gameObject.SetActive(false);
                oxygenWarningController.SetGradientOffset(normalGradientOffset);
                oxygenWarningController.SetGradientDerivation(normalGradientDerivation);

                isLowOxygenWarningActive = false;
                currentPulseValue = 0f;
            }
        }
    }


    // Low Oxygen Music

    private void StartLowOxygenMusic()
    {
        if (lowOxygenMusic == null) return;

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        if (!lowOxygenAudioSource.isPlaying)
            lowOxygenAudioSource.Play();

        musicFadeCoroutine = StartCoroutine(
            FadeMusic(lowOxygenAudioSource, lowOxygenAudioSource.volume, lowOxygenMusicVolume, musicFadeDuration)
        );
    }


    private void StopLowOxygenMusic()
    {
        if (!lowOxygenAudioSource.isPlaying) return;

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        musicFadeCoroutine = StartCoroutine(
            FadeMusic(lowOxygenAudioSource, lowOxygenAudioSource.volume, 0f, musicFadeDuration, true)
        );
    }


    private IEnumerator FadeMusic(AudioSource audioSource, float fromVolume, float toVolume, float duration, bool stopAfter = false)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(fromVolume, toVolume, t / duration);
            yield return null;
        }

        audioSource.volume = toVolume;

        if (stopAfter && toVolume <= 0f)
            audioSource.Stop();
    }


    // Movement

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
        currentMoveDepletionModifier = isSprinting ? sprintingDepletionChange : movingDepletionChange;
        ChangeDepletionRateIfMoving();
    }

    void TemporaryChangeOxygenDepletionRate(float value)
    {
        currentDepletionRate = oxygenDepletionRate.value * value;
    }

    public void ForceStopOxygenCompletely()
    {
        StopAllCoroutines();
        consumingOxygen = false;

        // Turn off visual warning
        if (oxygenWarningController != null)
        {
            oxygenWarningController.gameObject.SetActive(false);
            oxygenWarningController.SetGradientOffset(normalGradientOffset);
            oxygenWarningController.SetGradientDerivation(normalGradientDerivation);
        }

        StopLowOxygenMusic();
        
        // Avoid UpdateLowOxygenWarning from reactivating
        isLowOxygenWarningActive = false;
        currentPulseValue = 0f;
    }
}