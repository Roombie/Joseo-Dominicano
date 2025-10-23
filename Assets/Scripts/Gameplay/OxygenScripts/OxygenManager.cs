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
        // Debug.Log("isMovingEvent CALLED " + value);
    }
    public event System.Action<bool> isSprintingEvent;

    protected void SetSprintingEvent(bool value)
    {
        isSprintingEvent?.Invoke(value);
        // Debug.Log("isSprintingEvent CALLED " + value);
    }
}

public class OxygenManager : MonoBehaviour
{
    [SerializeField] private FloatVariable oxygenLevel;
    [SerializeField] private FloatVariable oxygenDepletionRate; //Seconds to consume 1%
    [SerializeField] private FloatVariable maxTotalOxygen; //Seconds to consume 1%

    [SerializeField] private float movingDepletionChange = 1.2f;
    [SerializeField] private float sprintingDepletionChange = 1.7f;
    [SerializeField] private float currentMoveDepletionModifier = 1;

    [Header("Hazards")]
    [SerializeField] float hazardDamage = 10f;


    [SerializeField] private Slider oxygenBar;
    [SerializeField] private TextMeshProUGUI oxygenLvlText;

    //Scripts to call events
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

        ResetOxygen();

        currentDepletionRate = oxygenDepletionRate.value;
        currentMoveDepletionModifier = movingDepletionChange;
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
    }

    public void ResetOxygen()
    {
        StopAllCoroutines();

        consumingOxygen = false;
        oxygenLevel.value = maxTotalOxygen.value;
        oxygenLvlText.text = oxygenLevel.value.ToString();

        UpdateOxygenBar();
    }

    public void PauseOxygen()
    {
        StopAllCoroutines();

        consumingOxygen = false;

        UpdateOxygenBar();
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
            yield return new WaitForSecondsRealtime(currentDepletionRate); // Consume 1% oxygen every x seconds defined in the depletion rate
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
    void UpdateBoolIfMoving(bool movingState)
    {
        ismoving = movingState;
        ChangeDepletionRateIfMoving();
    }
    public void ChangeDepletionRateIfMoving()
    {
        if (ismoving)
        {
            //Debug.Log("isMoving detected");
            TemporaryChangeOxygenDepletionRate(currentMoveDepletionModifier);
        }
        else
        {
            currentDepletionRate = oxygenDepletionRate.value;
            //Debug.Log("stop Moving detected, currentDepletionRate set to: " + currentDepletionRate);
        }
    }
    public void ChangeDepletionRateIfSprinting(bool isSprinting)
    {
        if (isSprinting)
        {
            currentMoveDepletionModifier = sprintingDepletionChange;
            //Debug.Log("isSprinting detected, currentMoveDepletionModifier set to: " + currentMoveDepletionModifier);
        }
        else
        {
            currentMoveDepletionModifier = movingDepletionChange;
            //Debug.Log("stopped Sprinting detected, currentMoveDepletionModifier set to: " + currentMoveDepletionModifier);

        }

        ChangeDepletionRateIfMoving();


    }
    void TemporaryChangeOxygenDepletionRate(float value)
    {
        currentDepletionRate = oxygenDepletionRate.value * value;
        //Debug.Log("currentDepletionRate changed to: " + currentDepletionRate);
    }


}

