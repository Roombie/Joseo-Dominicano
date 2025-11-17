using TMPro;
using UnityEngine;

public class HomeStatLine : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text valueText;

    public void SetLabel(string text)
    {
        if (labelText != null)
            labelText.text = text;
        else
            Debug.LogWarning($"{name}: labelText not assigned!");
    }

    public void SetValue(string text)
    {
        if (valueText != null)
            valueText.text = text;
        else
            Debug.LogWarning($"{name}: valueText not assigned!");
    }

    public void Clear()
    {
        if (labelText != null) labelText.text = string.Empty;
        if (valueText != null) valueText.text = string.Empty;
    }
}