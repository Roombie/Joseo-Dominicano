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

    [Header("Pointer / Touch")]
    [Tooltip("If active, also shoot with tap/click (depending on platform)")]
    public bool allowPointer = true;

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

    void OnEnable()
    {
        if (input != null && input.action != null && !input.action.enabled)
        {
            input.action.Enable();
        }
    }

    void OnDisable()
    {
        
    }

    void Update()
    {
        if (oneShot && _hasFired)
            return;

        if (!allowPointer)
            return;

#if UNITY_EDITOR
        if (PointerPressedThisFrame())
        {
            Fire();
        }
#else
        if (IsMobileUser() && PointerPressedThisFrame())
        {
            Fire();
        }
#endif
    }

    private void OnInputPerformed(InputAction.CallbackContext ctx)
    {
        Fire();
    }

    private void Fire()
    {
        if (oneShot && _hasFired)
            return;

        _hasFired = true;
        OnPressed?.Invoke();
    }

    private bool IsMobileUser()
    {
        if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld)
            return true;

    #if UNITY_WEBGL && !UNITY_EDITOR
        if (Touchscreen.current != null)
            return true;
    #endif

        return false;
    }

    private bool PointerPressedThisFrame()
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
                return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    public void ResetOneShot()
    {
        _hasFired = false;
    }
}