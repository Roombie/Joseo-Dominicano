using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;
using UnityEngine.Events;

public class PlayerSmoothMovement : OxygenableBehaviour
{
    [SerializeField] InputReader input;
    [SerializeField] private GameObject mobileControls;

    public float diagonalAnimationAdjustmentTime = 0.099f; // SYSTEM: Delay animation time from diagonal: Adjust this value as needed
    private bool isUpdatingLastDirection = false; // System: Prevent multiple coroutines

    Animator animator;
    private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sprite;
    private Vector2 moveInput;
    private Vector2 movement;
    private Quaternion targetRotation;
    private float lastDirection;
    private float currentSpeed;
    [SerializeField] float movementDeadZone = 0.01f;
    public float walkSpeed = 5f;
    public float rotationSpeed = 5f;
    public float runSpeed = 10f;
    [SerializeField] float enabledLinearDamping = 4.5f;
    [SerializeField] float enabledAngularDamping = 9;
    [SerializeField] float disabledLinearDamping = 0.5f;
    [SerializeField] float disabledAngularDamping = 2;
    [Range(0, 1)] public float flipDirectionThreshold = 0.2f;
    public enum MoveToForwardType { FollowInputDirection, FollowPhysicsRotation }
    public MoveToForwardType _forwardTraslationType;
    private bool _isSprinting = false;
    [SerializeField] public UnityEvent _onSprintStart;
    [SerializeField] public UnityEvent _onSprintEnd;

    [Header("Swim Sounds")]
    [SerializeField] private AudioClip[] swimSFXVariations;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;
    private bool wasMoving = false;
    private float swimSoundTimer = 0f;
    private const float SwimSoundInterval = 1.2f;
    [SerializeField] private AudioClip sprintStartSFX;

    //Events
    // public event Action<bool> isMovingEvent;
    // public event Action<bool> isSprintingEvent;
    public event Action onInteractEvent;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();

        if (mobileControls != null)
        {
            mobileControls.SetActive(false);
        }
    }
    
    void OnEnable()
    {
        rb.linearDamping = enabledLinearDamping;
        rb.angularDamping = enabledAngularDamping;
        input.MoveEvent += OnMove;
        input.SprintStartedEvent += SprintPressed;
        input.SprintCanceledEvent += SprintReleased;
    }

    void OnDisable()
    {
        rb.linearDamping = disabledLinearDamping;
        rb.angularDamping = disabledAngularDamping;
        input.MoveEvent -= OnMove;
        input.SprintStartedEvent -= SprintPressed;
        input.SprintCanceledEvent -= SprintReleased;
    }

    private void Start()
    {
        currentSpeed = walkSpeed; // Set initial speed               
    }
    
    private void FixedUpdate()
    {
        if (rb == null) return;

        // 2. Rotación Progresiva (ApplyTorque)
        if (moveInput.sqrMagnitude > movementDeadZone) // Solo si hay input
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
        if (moveInput.sqrMagnitude > movementDeadZone)
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
        else if (currentSpeed == runSpeed) // SPRINTING
        {
            SprintReleased();
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

        // Handle swimming sound based on movement
        // Check if we're in active gameplay
        if (GameManager.Instance == null || !GameManager.Instance.inShift || GameManager.Instance.isPaused) 
        {
            swimSoundTimer = 0f;
            return;
        }

        bool isMoving = moveInput.sqrMagnitude > movementDeadZone;
        
        if (isMoving)
        {
            swimSoundTimer += Time.deltaTime;
            
            if (swimSoundTimer >= SwimSoundInterval)
            {
                if (swimSFXVariations != null && swimSFXVariations.Length > 0)
                {
                    AudioClip randomSound = swimSFXVariations[UnityEngine.Random.Range(0, swimSFXVariations.Length)];

                    // Random pitch between pitch for more variety
                    float randomPitch = UnityEngine.Random.Range(minPitch, maxPitch);
                    
                    // Adjust velocity
                    if (currentSpeed == runSpeed)
                    {
                        randomPitch *= 1.2f;
                    }
                    
                    AudioManager.Instance.Play(randomSound, SoundCategory.SFX, 1f, pitch: randomPitch);
                }
                
                swimSoundTimer = 0f;
            }
        }
        else
        {
            swimSoundTimer = 0f;
        }
        
        wasMoving = isMoving;

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
    
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            Debug.Log("INTERACT RECEIVED by PlayerSmoothMovement");
            onInteractEvent?.Invoke();
        }
    }

    public void ResetMove()
    {
        moveInput = Vector2.zero;
        currentSpeed = walkSpeed;
        animator.SetFloat("Speed", 0);
        _isSprinting = false;
        SetMoveEvent(false);
        SetSprintingEvent(false); 
    }

    public void OnMove(Vector2 value)
    {
        moveInput = value;

        if (moveInput != Vector2.zero)
        {
            SetMoveEvent(true); // Tell listeners moving changed
        }
        else
        {
            SetMoveEvent(false); // Tell listeners moving changed
        }
    }

    public void SprintPressed()
    {
        // Solo en gameplay activo y sin pausa
        if (GameManager.Instance == null || 
            !GameManager.Instance.inShift || 
            GameManager.Instance.isPaused)
        {
            return;
        }

        // Ya estoy sprintando → nada
        if (_isSprinting) 
            return;

        // Si no hay input de movimiento AHORA MISMO, no activamos sprint
        if (moveInput.sqrMagnitude <= movementDeadZone)
            return;

        Debug.Log("Sprint pressed (valid)");

        _isSprinting = true;
        currentSpeed = runSpeed;

        // Avisar a OxygenManager que estamos sprintando
        SetSprintingEvent(true);

        _onSprintStart?.Invoke(); // aquí va tu efecto de boost
    }

    public void SprintReleased()
    {
        // Solo hacemos algo si realmente llegamos a estar en sprint
        if (!_isSprinting) 
            return;

        Debug.Log("Sprint released");
        _isSprinting = false;
        currentSpeed = walkSpeed;

        // Avisar que dejamos de sprintar
        SetSprintingEvent(false); 

        _onSprintEnd?.Invoke(); // aquí se apaga el boost
    }

    public void ResetSprintState()
    {
        // Hard reset sprint flags and speed
        _isSprinting = false;

        if (currentSpeed == runSpeed)
            currentSpeed = walkSpeed;

        // Ensure listeners know we are not sprinting anymore
        SetSprintingEvent(false);

        // Make sure VFX / distortion tied to OnSprintEnd are turned off
        _onSprintEnd?.Invoke();
    }

    // Used when the shift ends / game resets
    public void ForceStopSprint()
    {
        if (_isSprinting)
        {
            // Use the normal flow so it logs and calls OnSprintEnd once
            SprintReleased();
        }
        else
        {
            // Even if the flag says "not sprinting", make sure
            // speed and VFX are reset (defensive)
            ResetSprintState();
        }
    }

    [SerializeField]
    bool simulateMobileInEditor = false;

    bool IsMobileUser()
    {
    #if UNITY_EDITOR
        if (simulateMobileInEditor)
            return true;
    #endif

        if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld)
            return true;

        return false;
    }


    /// <summary>
    /// Called by GameManager when entering/exiting gameplay.
    /// Only shows the mobile controls when:
    /// - This is a mobile-like user (including WebGL on phone), AND
    /// - 'enable' is true.
    /// </summary>
    public void SetMobileControlsForGameplay(bool enable)
    {
        if (mobileControls == null)
            return;

        if (!IsMobileUser())
        {
            // Never show mobile controls on desktop / WebGL PC, etc.
            mobileControls.SetActive(false);
            return;
        }

        mobileControls.SetActive(enable);
    }
}