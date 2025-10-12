using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public class PlayerSmoothMovement : OxygenableBehaviour
{
    [SerializeField] private GameObject mobileControls;

    public float diagonalAnimationAdjustmentTime = 0.099f; //SYSTEM: Delay animation time from diagonal: Adjust this value as needed
    private bool isUpdatingLastDirection = false; // System: Prevent multiple coroutines

    Animator animator;
    private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sprite;
    private Vector2 moveInput;
    private Vector2 movement;
    private Quaternion targetRotation;
    private float lastDirection;
    private float currentSpeed;
    public float walkSpeed = 5f;
    public float rotationSpeed = 5f;
    public float runSpeed = 10f;
    [Range(0, 1)] public float flipDirectionThreshold = 0.2f;
    public enum MoveToForwardType { FollowInputDirection, FollowPhysicsRotation }
    public MoveToForwardType _forwardTraslationType;

    //Events
    // public event Action<bool> isMovingEvent;
    // public event Action<bool> isSprintingEvent;
    public event Action onInteractEvent;

    void Awake()
    {

#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID
        if (mobileControls != null)
        {
            mobileControls.gameObject.SetActive(true);
        }
#else
        if (mobileControls != null)
        {
            mobileControls.gameObject.SetActive(false);
        }
#endif

    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();

        currentSpeed = walkSpeed; // Set initial speed               
    }
    
    private void FixedUpdate()
    {
        if (rb == null) return;

        // 2. Rotación Progresiva (ApplyTorque)
        if (moveInput.sqrMagnitude > 0.01f) // Solo si hay input
        {
            // Calcular el ángulo de rotación deseado (en radianes, luego a grados)
            float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;

            // Convertir la rotación actual del Rigidbody a un ángulo
            float currentAngle = rb.rotation;

            // Usar Mathf.DeltaAngle para obtener la diferencia angular más corta
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);

            // Calcular el torque necesario para mover la rotación hacia el objetivo
            // Usamos un factor de proporcionalidad simple (por ejemplo, 0.1) multiplicado por la diferencia
            // y la velocidad de rotación. O se puede usar una fórmula de torque más compleja.
            float torque = angleDifference * (rotationSpeed / 100f);

            // Limitar el torque para evitar rotaciones excesivas y temblores
            torque = Mathf.Clamp(torque, -rotationSpeed, rotationSpeed);

            // Aplicar Torque
            rb.AddTorque(torque);
        }

        // 3. Movimiento Suavizado
        if (moveInput.sqrMagnitude > 0.01f)
        {
            // El Rigidbody2D.rotation es el ángulo en grados. Necesitamos la dirección frontal (forward vector)
            // que coincide con la rotación Z del Rigidbody en 2D.
            // Vector2.up (0, 1) rotado por el ángulo (rb.rotation) nos da la dirección frontal.
            Vector2 forwardVector;
            switch (_forwardTraslationType)
            {
                case MoveToForwardType.FollowInputDirection:
                    forwardVector = targetRotation * Vector2.right;
                    break;
                case MoveToForwardType.FollowPhysicsRotation:
                    forwardVector = Quaternion.Euler(0, 0, rb.rotation) * Vector2.right;
                    break;
                default:
                    forwardVector = targetRotation * Vector2.right;
                    break;
            }

            // La velocidad deseada es la dirección frontal multiplicada por la velocidad
            Vector2 desiredVelocity = forwardVector * currentSpeed;

            // Calculamos la diferencia entre la velocidad deseada y la velocidad actual
            Vector2 velocityChange = desiredVelocity - rb.linearVelocity;

            // Aplicamos la fuerza como un cambio de velocidad instantáneo
            rb.AddForce(desiredVelocity);
        }
        // else
        // {
        //     // Si no hay input, reducir gradualmente la velocidad para simular la "fricción del agua"
        //     // Esto se hace aplicando una fuerza contraria a la velocidad actual.
        //     rb.AddForce(-rb.linearVelocity * 0.1f, ForceMode2D.Force); 
        // }
    }

    private void Update()
    {
        // LookForward();

        //Note: Animator should create a blend tree for the 8 directions and set motion values according to values on DefineLastDirection()
        // animator.SetFloat("Blend", lastDirection);
        animator.SetFloat("X", rb.linearVelocity.x / currentSpeed);
        animator.SetFloat("Y", rb.linearVelocity.y / currentSpeed);
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);

        float playerHorizontalSide = Vector3.Dot(transform.right, Vector3.right);
        if (Mathf.Abs(playerHorizontalSide) > flipDirectionThreshold)
        {
            sprite.flipY = playerHorizontalSide < 0;
        }


        if (!isUpdatingLastDirection) // If moving away from diagonal, delay before switching to avoid animation flicker
            {
                StartCoroutine(DelayDirectionChange());
            }

    }

    private IEnumerator DelayDirectionChange()
    {
        isUpdatingLastDirection = true;
        yield return new WaitForSecondsRealtime(diagonalAnimationAdjustmentTime); // Wait before changing direction

        // DefineLastDirection();

        //Debug.Log("lastDirection changed to: " + lastDirection);
        isUpdatingLastDirection = false;
    }
    public void OnInteract() => onInteractEvent?.Invoke();
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();

        if (moveInput != Vector2.zero)
        {
            SetMoveEvent(true); // Tell listeners moving changed
        }
        else
        {
            SetMoveEvent(false); // Tell listeners moving changed
        }
    }

    //NOTE: needs a Press and release interaction on the action map button to work
    public void OnSprint(InputValue value)
    {
        Sprint();
    }

    public void Sprint()
    {
        if (currentSpeed == walkSpeed)
        {
            currentSpeed = runSpeed;
            //Debug.Log("Sprint started");

            SetSprintingEvent(true); // Tell listeners sprinting changed
        }
        else
        {
            currentSpeed = walkSpeed;
            //Debug.Log("Sprint canceled");

            SetSprintingEvent(false); // Tell listeners sprinting changed
        }
    }

}
