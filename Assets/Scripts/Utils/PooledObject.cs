using UnityEngine;

/// <summary>
/// Component that lets an object return to its pool in a generic way.
/// </summary>
public class PooledObject : MonoBehaviour
{
    private ObjectPool _pool;

    /// <summary>
    /// Set by ObjectPool when the instance is created.
    /// </summary>
    internal void SetPool(ObjectPool pool)
    {
        _pool = pool;
    }

    /// <summary>
    /// Returns this object to its pool. If no pool is assigned,
    /// the object is destroyed.
    /// </summary>
    public void ReturnToPool()
    {
        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            // Fallback for objects not created by a pool
            Destroy(gameObject);
        }
    }
}