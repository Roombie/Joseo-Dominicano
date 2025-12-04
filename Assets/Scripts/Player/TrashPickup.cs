using UnityEngine;
using System.Collections;

public class TrashPickup : MonoBehaviour
{
    [SerializeField] private TrashItemSO item;
    [SerializeField] private float destroyTime = 10f;
    [SerializeField] private bool selfDestroy = true;

    public TrashItemSO Item => item;

    private Coroutine autoDestroyRoutine;
    private bool collected;

    // DO NOT assume this is set in Awake – PooledObject may be added later by the pool
    private PooledObject pooled;
    private Collider2D col;
    private Rigidbody2D rb;

    private void Awake()
    {
        // These components are on the prefab, so it's safe to cache them in Awake
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        collected = false;

        // Reactivate collision and physics when the object is reused
        if (col != null) col.enabled = true;
        if (rb != null) rb.simulated = true;

        // Start auto-despawn timer if enabled
        if (selfDestroy)
        {
            autoDestroyRoutine = StartCoroutine(AutoDespawn());
        }
    }

    private void OnDisable()
    {
        // Stop the coroutine if the object is disabled (returned to pool, etc.)
        if (autoDestroyRoutine != null)
        {
            StopCoroutine(autoDestroyRoutine);
            autoDestroyRoutine = null;
        }
    }

    private IEnumerator AutoDespawn()
    {
        yield return new WaitForSeconds(destroyTime);

        if (!collected)
        {
            Despawn();
        }
    }

    /// <summary>
    /// Call this when the player collects this trash item.
    /// </summary>
    public void OnCollected()
    {
        if (collected) return;
        collected = true;

        // Immediately stop collisions/physics to avoid weird interactions
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;

        // TODO: play VFX/SFX here if needed, then despawn
        Despawn();
    }

    /// <summary>
    /// Handles removal of this object: return to pool or destroy.
    /// </summary>
    private void Despawn()
    {
        if (autoDestroyRoutine != null)
        {
            StopCoroutine(autoDestroyRoutine);
            autoDestroyRoutine = null;
        }

        // Lazy-get the pool now, because PooledObject might have been
        // added by ObjectPool AFTER Awake() ran.
        if (pooled == null)
        {
            pooled = GetComponent<PooledObject>();
        }

        if (pooled != null)
        {
            // This instance came from a pool → return it
            pooled.ReturnToPool();
        }
        else
        {
            // Fallback for objects not managed by a pool
            Destroy(gameObject);
        }
    }
}