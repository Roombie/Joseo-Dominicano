using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

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

    private float currentDepletionRate = 1;

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
            playerMovement.isMovingEvent += ChangeDepletionRateIfMoving;
            playerMovement.isMovingEvent += ChangeDepletionRateIfSprinting;
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
            oxygenLevel.value -= 1;
            oxygenLvlText.text = oxygenLevel.value.ToString();

            UpdateOxygenBar();
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
        oxygenLevel.value += value;
        
        
        oxygenLvlText.text = oxygenLevel.value.ToString();
    }

    void UpdateOxygenBar()
    {
        oxygenBar.value = oxygenLevel.value / maxTotalOxygen.value;
    }

    void TemporaryChangeOxygenDepletionRate(float value)
    {
        currentDepletionRate = oxygenDepletionRate.value * value;
    }

    public void ChangeDepletionRateIfMoving(bool ismoving)
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
        if(isSprinting)
        {
            currentMoveDepletionModifier = sprintingDepletionChange;
        }
        else
        {
            currentMoveDepletionModifier = movingDepletionChange;

        }

            TemporaryChangeOxygenDepletionRate(currentMoveDepletionModifier);
    }
}
