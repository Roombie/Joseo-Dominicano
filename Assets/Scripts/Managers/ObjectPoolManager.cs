using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool for a specific prefab. Managed through ObjectPoolManager.
/// </summary>
public class ObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform parent;
    private readonly Queue<PooledObject> available = new Queue<PooledObject>();

    public ObjectPool(GameObject prefab, int initialSize, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            var po = CreateInstance();
            po.gameObject.SetActive(false);
            available.Enqueue(po);
        }
    }

    /// <summary>
    /// Creates a new instance and hooks it to this pool.
    /// </summary>
    private PooledObject CreateInstance()
    {
        GameObject go = GameObject.Instantiate(prefab, parent);
        PooledObject pooled = go.GetComponent<PooledObject>();
        if (pooled == null)
        {
            pooled = go.AddComponent<PooledObject>();
        }

        pooled.SetPool(this);
        return pooled;
    }

    /// <summary>
    /// Gets an instance from the pool (or creates one) and places it.
    /// </summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        PooledObject pooled = available.Count > 0 ? available.Dequeue() : CreateInstance();
        GameObject go = pooled.gameObject;

        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);

        return go;
    }

    /// <summary>
    /// Returns an instance back to the pool.
    /// </summary>
    public void Release(PooledObject pooled)
    {
        if (pooled == null) return;

        GameObject go = pooled.gameObject;
        go.SetActive(false);
        available.Enqueue(pooled);
    }

    /// <summary>
    /// Destroys all currently available (inactive) instances in this pool.
    /// </summary>
    public void Clear()
    {
        while (available.Count > 0)
        {
            var pooled = available.Dequeue();
            if (pooled != null)
            {
                GameObject.Destroy(pooled.gameObject);
            }
        }
    }
}

/// <summary>
/// Scene-level singleton that manages all prefab pools.
/// Does NOT persist across scenes.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public class PoolConfig
    {
        public GameObject prefab;
        public int initialSize = 10;
    }

    [Header("Initial pools (optional)")]
    [SerializeField] private List<PoolConfig> initialPools = new List<PoolConfig>();

    private readonly Dictionary<GameObject, ObjectPool> prefabToPool =
        new Dictionary<GameObject, ObjectPool>();

    private void Awake()
    {
        // Scene-local singleton (no DontDestroyOnLoad)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Create initial pools defined in the inspector
        for (int i = 0; i < initialPools.Count; i++)
        {
            var cfg = initialPools[i];
            if (cfg.prefab == null) continue;
            if (prefabToPool.ContainsKey(cfg.prefab)) continue;

            int size = Mathf.Max(0, cfg.initialSize);
            prefabToPool[cfg.prefab] = new ObjectPool(cfg.prefab, size, transform);
        }
    }

    /// <summary>
    /// Gets an instance from the pool for the given prefab.
    /// If the pool does not exist yet, it is created lazily.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("ObjectPoolManager.Get received a null prefab.");
            return null;
        }

        if (!prefabToPool.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool(prefab, 0, transform);
            prefabToPool.Add(prefab, pool);
        }

        return pool.Get(position, rotation);
    }

    /// <summary>
    /// Returns an instance to its pool (if it has PooledObject).
    /// Otherwise, destroys the object.
    /// </summary>
    public void Release(GameObject instance)
    {
        if (instance == null) return;

        PooledObject pooled = instance.GetComponent<PooledObject>();
        if (pooled != null)
        {
            pooled.ReturnToPool();
        }
        else
        {
            Destroy(instance);
        }
    }

    /// <summary>
    /// Clears all registered pools (only affects inactive objects).
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var kvp in prefabToPool)
        {
            kvp.Value.Clear();
        }
        prefabToPool.Clear();
    }
}