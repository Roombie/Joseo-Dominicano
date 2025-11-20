using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TutorialInteractTarget : MonoBehaviour, IPointerClickHandler
{
    [Header("Config")]
    public bool requireInRange = true;

    [Header("Events")]
    public UnityEvent OnValidInteract;   // se dispara cuando toca en rango
    public UnityEvent OnInvalidInteract; // opcional, cuando toca fuera de rango

    private bool _inRange = false;

    /// <summary>
    /// El tutorial (texto JUGADOR) llamara esto
    /// cuando "entre" o "salga" del rango.
    /// </summary>
    public void SetInRange(bool value)
    {
        _inRange = value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Si no hace falta rango, siempre acepta
        if (!requireInRange || _inRange)
        {
            OnValidInteract?.Invoke();
        }
        else
        {
            OnInvalidInteract?.Invoke();
        }
    }
}