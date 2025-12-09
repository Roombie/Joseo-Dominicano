using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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

    /// <summary>
    /// Returns true if there was a pointer press this frame AND it was NOT over UI.
    /// </summary>
    private bool PointerPressedThisFrame()
    {
        // Touch (Input System)
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                if (!IsPointerOverUI())
                    return true;
            }
        }

        // Mouse
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!IsPointerOverUI())
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the current pointer (mouse or first touch) is over a UI element.
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        // Touch UI check (usamos Input solo para la detección de UI)
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        }
#endif

        // Mouse / default pointer
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void ResetOneShot()
    {
        _hasFired = false;
    }
}