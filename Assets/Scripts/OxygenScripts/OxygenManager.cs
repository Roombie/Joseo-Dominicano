using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class OxygenManager : MonoBehaviour
{
    [SerializeField] private FLoatVariable oxygenLevel;
    [SerializeField] private FLoatVariable oxygenDepletionRate; //Seconds to consume 1%
    [SerializeField] private FLoatVariable maxTotalOxygen; //Seconds to consume 1%

    [SerializeField] private Slider oxygenBar;
    [SerializeField] private TextMeshProUGUI oxygenLvlText;

    [SerializeField] bool consumingOxygen = false;

    void Start()
    {
        ResetOxygen();
    }

    public void ResetOxygen()
    {
        consumingOxygen = false;
        oxygenLevel.value = maxTotalOxygen.value;
        oxygenLvlText.text = oxygenLevel.value.ToString();
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
            yield return new WaitForSecondsRealtime(oxygenDepletionRate.value); //Consume 1% oxygen every x seconds defined in the depletion rate
            oxygenLevel.value -= 1;
            oxygenLvlText.text = oxygenLevel.value.ToString();
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
}
