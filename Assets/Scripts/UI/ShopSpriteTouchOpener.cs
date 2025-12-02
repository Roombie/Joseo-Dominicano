using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class ShopSpriteTouchOpener : MonoBehaviour
{
    [Header("Target shop logic")]
    [SerializeField] private ShopInteraction shop;   // Object that has ShopInteraction (collider for PC button logic)

    [Header("Camera")]
    [SerializeField] private Camera worldCamera;     // Camera used for screen → world conversion (usually Main Camera)

    [Header("Editor / Debug")]
    [SerializeField] private bool allowMouseInEditor = true; // Allow mouse clicks in Editor/Standalone
    [SerializeField] private bool logDebug = false;          // Enable debug logs

    // Collider on THIS object (the clickable shop sprite)
    private Collider2D tapCollider;

    void Awake()
    {
        // If no camera is assigned, fall back to the main camera
        if (worldCamera == null)
            worldCamera = Camera.main;

        // This script lives on the clickable sprite, so we grab its 2D collider here
        tapCollider = GetComponent<Collider2D>();

        if (worldCamera == null && logDebug)
            Debug.LogWarning("[ShopSpriteTouchOpener] No worldCamera assigned and Camera.main is null.");

        if (shop == null && logDebug)
            Debug.LogWarning("[ShopSpriteTouchOpener] No ShopInteraction reference assigned.");

        if (tapCollider == null && logDebug)
            Debug.LogWarning("[ShopSpriteTouchOpener] No Collider2D on this GameObject. " +
                             "This script must be on the clickable shop sprite with a 2D collider.");
    }

    void Update()
    {
        if (shop == null || worldCamera == null || tapCollider == null)
            return;

        // Check if there was a pointer press this frame (touch on phone, mouse on PC)
        if (!TryGetPointerDown(out Vector2 screenPos))
            return;

        if (logDebug)
            Debug.Log($"[ShopSpriteTouchOpener] Pointer down at screen position: {screenPos}");

        // Convert screen position to world position
        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        Vector2 world2D = new Vector2(world.x, world.y);

        // Check if that world position is inside THIS collider
        bool hitThisCollider = tapCollider.OverlapPoint(world2D);

        if (!hitThisCollider)
        {
            if (logDebug)
                Debug.Log("[ShopSpriteTouchOpener] Pointer did not hit this shop sprite collider.");
            return;
        }

        if (logDebug)
            Debug.Log("[ShopSpriteTouchOpener] Shop sprite tapped. Calling shop.Interact().");

        // Call the logic on the other object (the one with ShopInteraction)
        shop.Interact();
    }

    /// <summary>
    /// Returns true if the primary pointer (mouse or touch) was pressed this frame,
    /// and outputs its screen position in pixels.
    /// </summary>
    private bool TryGetPointerDown(out Vector2 screenPos)
    {
        screenPos = default;

        // Pointer.current will be:
        // - Mouse on PC / Editor
        // - Touchscreen on mobile
        var pointer = Pointer.current;
        if (pointer == null)
        {
            if (logDebug)
                Debug.Log("[ShopSpriteTouchOpener] Pointer.current is null.");
            return false;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        // Optional: ignore mouse in Editor/Standalone if you only want touch-based opening
        if (!allowMouseInEditor && pointer is Mouse)
            return false;
#endif

        if (pointer.press.wasPressedThisFrame)
        {
            screenPos = pointer.position.ReadValue();
            return true;
        }

        return false;
    }
}