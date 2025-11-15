using UnityEngine;

/// <summary>
/// Gestiona la traslación constante y el movimiento vertical aleatorio
/// para simular objetos flotando en el agua.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class OceanObjectMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private float horizontalSpeed;
    private float verticalWiggle; // Velocidad vertical constante para el bamboleo

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Método llamado por LevelSpawner para inicializar la velocidad del objeto.
    /// </summary>
    /// <param name="speedX">Velocidad horizontal (positiva o negativa).</param>
    /// <param name="wiggleY">Velocidad vertical constante para el bamboleo.</param>
    public void InitializeMovement(float speedX, float wiggleY)
    {
        horizontalSpeed = speedX;
        verticalWiggle = wiggleY;

        // Establecer la velocidad inicial
        SetVelocity();
    }

    /// <summary>
    /// Aplica la velocidad en FixedUpdate para una física consistente.
    /// </summary>
    void FixedUpdate()
    {
        // Esto asegura que la velocidad se mantenga constante sin importar la fricción o las fuerzas externas leves.
        SetVelocity();
    }

    private void SetVelocity()
    {
        // Mantenemos la velocidad constante en X y Y.
        // Si queremos un bamboleo más complejo (senoidal), lo aplicaríamos aquí.
        rb.linearVelocity = new Vector2(horizontalSpeed, verticalWiggle);
    }
}