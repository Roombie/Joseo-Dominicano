using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class OnInputActionPressed : MonoBehaviour
{
    public InputActionReference input;
    public UnityEvent OnPressed;
    void Awake()
    {
        if (input)
        {
            input.action.performed += (ctx) => OnPressed?.Invoke();
            input.action.Enable();
        }
    }

    void OnDestroy()
    {
        if (input) input.action.performed -= (ctx) => OnPressed?.Invoke();
    }
}
