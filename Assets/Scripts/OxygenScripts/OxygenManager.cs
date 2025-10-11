using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class OxygenManager : MonoBehaviour
{
    [SerializeField] private FloatVariable oxygenLevel;
    [SerializeField] private FloatVariable oxygenDepletionRate; //Seconds to consume 1%
    [SerializeField] private FloatVariable maxTotalOxygen; //Seconds to consume 1%

    [SerializeField] private float movingDepletionChange = 1.2f;
    [SerializeField] private float sprintingDepletionChange = 1.7f;
    [SerializeField] private float currentMoveDepletionModifier = 1;


    [SerializeField] private Slider oxygenBar;
    [SerializeField] private TextMeshProUGUI oxygenLvlText;

    [SerializeField] private PlayerMovement playerMovement;


    [SerializeField] private bool consumingOxygen = false;
    
    public UnityEvent onOxygenDepleted;

    bool ismoving = false;
    float currentDepletionRate = 1;

    void Start()
    {
        if(FindAnyObjectByType<PlayerMovement>() != null) 
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        ResetOxygen();

        currentDepletionRate = oxygenDepletionRate.value;
        currentMoveDepletionModifier = movingDepletionChange;
    }

    private void OnEnable()
    {
        if(playerMovement != null)
        {
            playerMovement.isMovingEvent += UpdateBoolIfMoving;
            playerMovement.isSprintingEvent += ChangeDepletionRateIfSprinting;
        }
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.isMovingEvent -= UpdateBoolIfMoving;
            playerMovement.isMovingEvent -= ChangeDepletionRateIfSprinting;
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
    public void ConsumeOxygen()
    {
        consumingOxygen = true;
        StartCoroutine(OxygenDepletion());
    }

    private IEnumerator OxygenDepletion()
    {
        while (consumingOxygen)
        {
            yield return new WaitForSecondsRealtime(currentDepletionRate); //Consume 1% oxygen every x seconds defined in the depletion rate
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
            else
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
        if(isSprinting)
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
