using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

[System.Serializable]
public class SpawnData
{
    [Tooltip("El prefab del objeto a instanciar (debe tener Rigidbody2D).")]
    public GameObject prefab;

    [Tooltip("Peso relativo para la selección. Un valor más alto significa que aparecerá más a menudo.")]
    [Range(0, 100)]
    public int weight = 10;
     
    [Header("Condición de Aparición")]
    [Tooltip("Si se establece en falso, este objeto no será incluido en la selección de spawn. Útil para desbloqueos.")]
    public bool canSpawn = true;
}

public class LevelSpawner : MonoBehaviour
{
    [SerializeField] private bool startSpawningInStart = true;
    
    [Header("Configuración de Nivel")]
    [Tooltip("ScriptableObject que define la configuración de este día/nivel.")]
    [SerializeField] private LevelDayConfig currentDayConfig;
    public void SetCurrentDayConfig(LevelDayConfig day) => currentDayConfig = day;

    [Header("Pre-Warm")]
    [Tooltip("Tiempo en segundos que se simulará el spawn antes de que el nivel comience oficialmente.")]
    [SerializeField] private float preWarmSeconds = 5f;

    [Header("Áreas de Spawn (Colisionadores)")]
    [Tooltip("Colisionador 2D que define el área vertical de aparición en el lado IZQUIERDO de la pantalla.")]
    [SerializeField] private Collider2D leftSpawnArea;
     
    [Tooltip("Colisionador 2D que define el área vertical de aparición en el lado DERECHO de la pantalla.")]
    [SerializeField] private Collider2D rightSpawnArea;

    [Header("Spawn Smoothing")]
    [Tooltip("Número mínimo de objetos que deben mantenerse en pantalla")]
    [SerializeField] private int minObjectsOnScreen = 30;

    // Lista de datos filtrada: solo objetos con weight > 0 
    private List<SpawnData> availableSpawnData; 
    private float totalWeight;
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;

    void Start()
    {
        if (startSpawningInStart) StartSpawning();
    }

    /// <summary>
    /// Inicia la rutina de spawn.
    /// </summary>
    [ContextMenu("Start Spawning")]
    public void StartSpawning()
    {
        if (isSpawning)
        {
            Debug.LogWarning("El spawner ya está activo.");
            return;
        }

        if (currentDayConfig == null || leftSpawnArea == null || rightSpawnArea == null)
        {
            Debug.LogError("El Spawner del Nivel no está configurado correctamente. No se puede iniciar el spawn.");
            return;
        }

        RecalculateSpawnWeights();

        if (totalWeight > 0)
        {
            spawnCoroutine = StartCoroutine(SpawnRoutine());
            isSpawning = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Spawner iniciado.");
#endif
        }
        else
        {
            Debug.LogWarning("No hay objetos activos con peso positivo para aparecer. El spawn no se iniciará.");
        }
    }

    /// <summary>
    /// Detiene la aparición de nuevos objetos.
    /// </summary>
    [ContextMenu("Stop Spawning")]
    public void StopSpawning()
    {
        if (isSpawning && spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            isSpawning = false;
            spawnCoroutine = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Spawner detenido.");
#endif
        }
    }

    /// <summary>
    /// Destruye / devuelve al pool todos los objetos con OceanObjectMovement activos en la escena.
    /// </summary>
    [ContextMenu("Clean Up Objects")]
    public void CleanUpObjects()
    {
        OceanObjectMovement[] objectsToClean = FindObjectsByType<OceanObjectMovement>(FindObjectsSortMode.None);

        int count = 0;
        for (int i = 0; i < objectsToClean.Length; i++)
        {
            var obj = objectsToClean[i];
            if (obj == null) continue;

            GameObject go = obj.gameObject;

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.Release(go);
            }
            else
            {
                Destroy(go);
            }

            count++;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Limpieza de nivel completada. {count} objetos destruidos / devueltos al pool.");
#endif
    }

    /// <summary>
    /// Recalcula el peso total y filtra los objetos que pueden aparecer.
    /// </summary>
    [ContextMenu("Recalculate Weights")]
    public void RecalculateSpawnWeights()
    {
        if (currentDayConfig == null) return;
        
        // Usando LINQ como en el original
        availableSpawnData = currentDayConfig.spawnableObjects
            .Where(data => data != null && data.canSpawn && data.weight > 0)
            .ToList();

        totalWeight = availableSpawnData.Sum(data => data.weight);

        if (totalWeight <= 0)
        {
            Debug.LogWarning("No hay objetos activos con peso positivo para aparecer. Deteniendo el spawn.");
            StopSpawning();
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"Pesos de spawn recalculados. Total Weight: {totalWeight}");
#endif
        }
    }

    /// <summary>
    /// Corutina principal para manejar el tiempo de aparición de objetos.
    /// Mantiene la simplicidad del original pero con la optimización de mantener mínimo de objetos.
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        isSpawning = true;

        // 1. Fase de Pre-Warm (igual que el original)
        PreWarm(preWarmSeconds);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Pre-Warm completado. Tiempo simulado: {preWarmSeconds} segundos.");
#endif

        // 2. Fase de Spawn en tiempo real con mantenimiento de mínimo
        while (isSpawning)
        {
            // Mantener mínimo de objetos en pantalla (nueva optimización)
            while (OceanObjectMovement.ActiveCount < minObjectsOnScreen && totalWeight > 0)
            {
                SpawnObject(0f);
                
                // Pequeño espaciado para evitar columnas
                if (OceanObjectMovement.ActiveCount < minObjectsOnScreen)
                {
                    yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
                }
            }

            float waitTime = Random.Range(currentDayConfig.minSpawnInterval, currentDayConfig.maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // Solo intentar spawnear si hay al menos un objeto activo
            if (totalWeight > 0)
            {
                SpawnObject(0f);
            }
        }
    }

    /// <summary>
    /// Simula la aparición de objetos durante un tiempo dado.
    /// COPIA EXACTA del método original que funciona bien.
    /// </summary>
    private void PreWarm(float duration)
    {
        if (duration <= 0f || totalWeight <= 0) return;

        float simulatedTime = 0f;
        float nextSpawnTime = Random.Range(currentDayConfig.minSpawnInterval, currentDayConfig.maxSpawnInterval);

        while (simulatedTime < duration)
        {
            if (simulatedTime >= nextSpawnTime)
            {
                float timeSinceSpawn = duration - nextSpawnTime;
                SpawnObject(timeSinceSpawn);

                float interval = Random.Range(currentDayConfig.minSpawnInterval, currentDayConfig.maxSpawnInterval);
                nextSpawnTime += interval;
            }

            float timeToAdvance = Mathf.Min(duration - simulatedTime, nextSpawnTime - simulatedTime);
            
            if (timeToAdvance <= 0f) 
            {
                if (simulatedTime < duration && Mathf.Approximately(simulatedTime, nextSpawnTime))
                {
                    break; 
                }
                break;
            }
            
            simulatedTime += timeToAdvance;
        }
    }

    /// <summary>
    /// Spawns an object, reusing from the pool if possible.
    /// Similar al original pero con object pooling.
    /// </summary>
    /// <param name="elapsedTime">
    /// Time since the object would have spawned (used for pre-warm to offset position).
    /// </param>
    private void SpawnObject(float elapsedTime)
    {
        // 1. Determinar el lado de spawn (igual que original)
        bool spawnFromLeft = Random.value > 0.5f;
        Collider2D spawnArea = spawnFromLeft ? leftSpawnArea : rightSpawnArea;
        
        if (spawnArea == null)
        {
            Debug.LogWarning("Spawn area is null, cannot spawn object");
            return;
        }
        
        float baseDirectionX = spawnFromLeft ? currentDayConfig.moveSpeed : -currentDayConfig.moveSpeed;

        Bounds bounds = spawnArea.bounds;

        // 2. Determinar la posición de spawn (igual que original)
        float spawnY = Random.Range(bounds.min.y, bounds.max.y);
        float initialSpawnX = spawnFromLeft ? bounds.min.x : bounds.max.x;
        
        // 2.1. Calcular la posición avanzada si hay tiempo transcurrido
        float finalSpawnX = initialSpawnX + (baseDirectionX * elapsedTime); 
        
        Vector3 spawnPosition = new Vector3(finalSpawnX, spawnY, 0);

        // 3. Seleccionar el prefab mediante selección ponderada
        GameObject prefabToSpawn = GetWeightedRandomPrefab();

        if (prefabToSpawn == null)
        {
            return;
        }

        // 4. Instanciar o obtener del pool
        GameObject newObject = null;
        if (ObjectPoolManager.Instance != null)
        {
            newObject = ObjectPoolManager.Instance.Get(prefabToSpawn, spawnPosition, Quaternion.identity);
        }
        else
        {
            newObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        }

        // 4.1 Configurar movimiento (igual que original)
        OceanObjectMovement movement = newObject.GetComponent<OceanObjectMovement>();

        if (movement != null)
        {
            float randomWiggle = Random.Range(-currentDayConfig.maxVerticalWiggle, currentDayConfig.maxVerticalWiggle);
            
            movement.InitializeMovement(baseDirectionX, randomWiggle, currentDayConfig.maxWiggleRate);
            
            // Rotar si viene de la derecha
            if (!spawnFromLeft)
            {
                newObject.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }
        else
        {
            Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float randomWiggle = Random.Range(-currentDayConfig.maxVerticalWiggle, currentDayConfig.maxVerticalWiggle);
    #if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector2(baseDirectionX, randomWiggle);
    #else
                rb.velocity = new Vector2(baseDirectionX, randomWiggle);
    #endif
            }
            Debug.LogWarning($"El objeto instanciado ({newObject.name}) no tiene el componente 'OceanObjectMovement', no se le pudo aplicar velocidad constante.");
        }
    }

    /// <summary>
    /// Selecciona un GameObject de forma aleatoria basándose en el 'weight'
    /// Similar al original pero usando bucle for en lugar de foreach para mejor performance
    /// </summary>
    private GameObject GetWeightedRandomPrefab()
    {
        if (availableSpawnData == null || availableSpawnData.Count == 0 || totalWeight <= 0f)
            return null;

        float randomNumber = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < availableSpawnData.Count; i++)
        {
            var data = availableSpawnData[i];
            currentWeight += data.weight;
            if (randomNumber <= currentWeight)
            {
                return data.prefab;
            }
        }
        
        // Por seguridad, devolvemos el último
        return availableSpawnData[availableSpawnData.Count - 1].prefab;
    }
}