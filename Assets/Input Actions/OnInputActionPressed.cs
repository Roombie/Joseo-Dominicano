using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class OnInputActionPressed : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference input;
    public UnityEvent OnPressed;

    [Header("Behavior")]
    public bool oneShot = true;

    private bool _hasFired = false;
    private System.Action<InputAction.CallbackContext> _handler;

    void Awake()
    {
        if (input)
        {
            _handler = OnInputPerformed;
            input.action.performed += _handler;
            input.action.Enable();
        }
    }

    void OnDestroy()
    {
        if (input && _handler != null)
        {
            input.action.performed -= _handler;
        }
    }

    private void OnInputPerformed(InputAction.CallbackContext ctx)
    {
        // If it's one-shot and has already fired, ignore
        if (oneShot && _hasFired)
            return;

        _hasFired = true;
        OnPressed?.Invoke();

        // disconnect the input so it NEVER fires again
        if (oneShot && input != null && _handler != null)
        {
            input.action.performed -= _handler;
            input.action.Disable();
        }
    }

    public void ResetOneShot()
    {
        _hasFired = false;

        if (input && _handler != null)
        {
            input.action.performed -= _handler;
            input.action.performed += _handler;
            input.action.Enable();
        }
    }
}