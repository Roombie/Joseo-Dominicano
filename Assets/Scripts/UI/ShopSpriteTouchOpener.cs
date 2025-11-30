using UnityEngine;
using UnityEngine.InputSystem;

public class ShopSpriteTouchOpener : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShopInteraction shop;   // objeto que tiene ShopInteraction
    [SerializeField] private Camera worldCamera;     // normalmente la Main Camera

    [Header("Editor")]
    [SerializeField] private bool allowMouseInEditor = true; // para probar con el mouse

    void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    void Update()
    {
        if (shop == null || worldCamera == null)
            return;

        // 1) ¿Hubo tap/click este frame?
        if (!TryGetTapPosition(out Vector2 screenPos))
            return;

        // 2) Pasar a coordenadas de mundo
        Vector3 worldPos = worldCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, worldCamera.nearClipPlane));

        Vector2 worldPoint2D = new Vector2(worldPos.x, worldPos.y);

        // 3) Raycast puntual en 2D
        Collider2D hit = Physics2D.OverlapPoint(worldPoint2D);

        // Si no golpeó nada, o no golpeó ESTE sprite, salir
        if (hit == null)
            return;

        if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            return;

        // 4) Aquí hacemos EXACTAMENTE lo mismo que el teclado:
        Debug.Log("[ShopSpriteTouchOpener] Tap/click sobre la TIENDA → Interact()");
        shop.Interact();   // Interact ya comprueba playerInRange, isOpen, isPaused, etc.
    }

    bool TryGetTapPosition(out Vector2 screenPos)
    {
        // TOUCH (Android / iOS / WebGL móvil / pantallas táctiles)
        if (Touchscreen.current != null)
        {
            var t = Touchscreen.current.primaryTouch;
            if (t.press.wasPressedThisFrame)
            {
                screenPos = t.position.ReadValue();
                return true;
            }
        }

        // MOUSE (PC, editor, WebGL escritorio) para pruebas
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
#if UNITY_EDITOR
            if (!allowMouseInEditor)
            {
                screenPos = default;
                return false;
            }
#endif
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        screenPos = default;
        return false;
    }
}