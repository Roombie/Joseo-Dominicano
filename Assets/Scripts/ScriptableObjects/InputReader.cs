using UnityEngine;
using UnityEngine.InputSystem; // Importante para usar Input System

// 
// Este ScriptableObject actuará como un canal (channel) o proxy 
// entre el Input System y cualquier clase que quiera escuchar los eventos de entrada.
//

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader")]
public class InputReader : ScriptableObject, GameInput.IPlayerActions, GameInput.IUIActions 
{
    // Las Actions se exponen como C# Actions (events)
    // El '?' indica que puede ser nulo, lo que es útil para eventos.

    // Movimiento (Vector2)
    public event System.Action<Vector2> MoveEvent;

    // Sprint (Button)
    // Un evento para cuando se presiona el botón de Sprint (started/performed)
    public event System.Action SprintStartedEvent;
    // Un evento para cuando se suelta el botón de Sprint (canceled)
    public event System.Action SprintCanceledEvent;

    // Pausa (Button)
    // Un evento para cuando se presiona el botón de Pausa (performed)
    public event System.Action PauseEvent;
    public event System.Action InteractEvent;
    public event System.Action UISubmitEvent;

    // --- Propiedades y Métodos de Inicialización ---

    // La referencia al Action Map generado automáticamente por el Input System
    private GameInput gameInput;

    private void OnEnable()
    {
        // Se llama al habilitar el ScriptableObject.
        // Asegura que las referencias existan y que el sistema esté activado.
        if (gameInput == null)
        {
            gameInput = new GameInput();
            // Esto asigna la implementación de la interfaz a esta clase (InputReader)
            // para que los callbacks del Input System lleguen aquí.
            gameInput.Player.SetCallbacks(this);
            gameInput.UI.SetCallbacks(this);
        }

        // Activamos el Action Map "Gameplay" por defecto.
        
    }

    private void OnDisable()
    {
        // Desactivamos el Action Map al deshabilitar el ScriptableObject.
        
    }

    public void EnablePlayer()
    {
        gameInput.Player.Enable();
    }

    public void DisablePlayer()
    {
        gameInput.Player.Disable();
    }

    public void EnableUI()
    {
        gameInput.UI.Enable();
    }

    public void DisableUI()
    {
        gameInput.UI.Disable();
    }

    // --- Implementación de la Interfaz IGameplayActions ---
    // Estos métodos son los callbacks automáticos que llama el Input System.

    // 1. Manejo del Movimiento (Move)
    public void OnMove(InputAction.CallbackContext context)
    {
        // El tipo de valor (Vector2) está definido en tu Action Map.
        // 'ReadValue<Vector2>()' obtiene el valor actual del control.
        Vector2 moveVector = context.ReadValue<Vector2>();

        // Lanza el evento solo si hay suscriptores (MoveEvent != null)
        MoveEvent?.Invoke(moveVector);
    }

    // 2. Manejo del Sprint (Sprint)
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // performed: El botón se ha presionado con suficiente fuerza (o se ha pulsado).
            SprintStartedEvent?.Invoke();
        }
        else if (context.canceled)
        {
            // canceled: El botón se ha soltado.
            SprintCanceledEvent?.Invoke();
        }
    }

    // 3. Manejo de la Pausa (Pause)
    public void OnPause(InputAction.CallbackContext context)
    {
        // 'performed' es el estado más común para acciones de botón "una sola vez".
        if (context.performed)
        {
            PauseEvent?.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
{
        if (context.performed)
            InteractEvent?.Invoke();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            UISubmitEvent?.Invoke();
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
            PauseEvent?.Invoke(); 
    }

    public void OnNavigate(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnPoint(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnMiddleClick(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnTrackedDevicePosition(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }

    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
    {
        // throw new System.NotImplementedException();
    }
}