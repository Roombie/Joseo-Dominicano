using UnityEngine;

/// <summary>
/// Handles constant horizontal translation and a sinusoidal vertical wiggle
/// to simulate floating objects in the water.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class OceanObjectMovement : MonoBehaviour
{
    public static int ActiveCount { get; private set; }

    private Rigidbody2D rb;
    private float horizontalSpeed;
    private float verticalWiggle;
    private float progressOffset;
    private float amplitudeMultiplier;
    private float maxWiggleRate;

    [SerializeField, Range(0, 1)] private float minWiggleMultiplier = 0.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()  => ActiveCount++;
    private void OnDisable() => ActiveCount--;

    /// <summary>
    /// Called by LevelSpawner to initialize the object's movement.
    /// </summary>
    public void InitializeMovement(float speedX, float wiggleY, float wiggleRate)
    {
        horizontalSpeed = speedX;
        verticalWiggle   = wiggleY;
        maxWiggleRate    = wiggleRate;

        // randomize wiggle every time we (re)spawn from the pool
        progressOffset      = Random.Range(0f, Mathf.PI * 2f);
        amplitudeMultiplier = Random.Range(minWiggleMultiplier, 1f);

        SetVelocity();
    }

    private void FixedUpdate()
    {
        SetVelocity();
    }

    private void SetVelocity()
    {
        float y =
            amplitudeMultiplier *
            verticalWiggle *
            Mathf.Sin((Time.time + progressOffset) * maxWiggleRate);

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(horizontalSpeed, y);
#else
        rb.velocity = new Vector2(horizontalSpeed, y);
#endif
    }
}