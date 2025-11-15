using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic; // Added for List<SpawnData>

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
    [SerializeField] private bool startSpawningInStart;
    [Header("Configuración de Nivel")]
    [Tooltip("Arrastra aquí el ScriptableObject que define la configuración de este día/nivel.")]
    [SerializeField] private LevelDayConfig currentDayConfig;

    [Header("Áreas de Spawn (Colisionadores)")]
    [Tooltip("Colisionador 2D que define el área vertical de aparición en el lado IZQUIERDO de la pantalla.")]
    [SerializeField] private Collider2D leftSpawnArea;
    
    [Tooltip("Colisionador 2D que define el área vertical de aparición en el lado DERECHO de la pantalla.")]
    [SerializeField] private Collider2D rightSpawnArea;

    // Lista de datos filtrada: solo objetos con weight > 0 
    private List<SpawnData> availableSpawnData; 
    private float totalWeight;
    private bool isSpawning = false;
    private Coroutine spawnCoroutine; // Referencia para poder detener la corrutina específicamente.

    void Start()
    {
        // El método Start() ahora llama a StartSpawning() para inicializar la lógica.
        if (startSpawningInStart) StartSpawning();
    }

    /// <summary>
    /// Intenta iniciar la rutina de aparición de objetos.
    /// Se debe llamar para empezar un nivel o reiniciar el spawn.
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

        // Recalcular los pesos antes de iniciar, en caso de que hayan cambiado
        RecalculateSpawnWeights();

        if (totalWeight > 0)
        {
            // Almacenamos la referencia para poder detenerla con StopCoroutine
            spawnCoroutine = StartCoroutine(SpawnRoutine());
            isSpawning = true;
            Debug.Log("Spawner iniciado.");
        }
        else
        {
            Debug.LogWarning("No hay objetos activos con peso positivo para aparecer. El spawn no se iniciará.");
        }
    }

    /// <summary>
    /// Detiene la aparición de nuevos objetos.
    /// Se debe llamar al finalizar un nivel o al pausar el juego.
    /// </summary>
    [ContextMenu("Stop Spawning")]
    public void StopSpawning()
    {
        if (isSpawning && spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            isSpawning = false;
            spawnCoroutine = null; // Limpiamos la referencia
            Debug.Log("Spawner detenido.");
        }
    }

    /// <summary>
    /// Destruye todos los objetos que fueron instanciados y tienen el componente OceanObjectMovement.
    /// </summary>
    [ContextMenu("Clean Up Objects")]
    public void CleanUpObjects()
    {
        // Encontramos todos los componentes de movimiento en la escena.
        OceanObjectMovement[] objectsToDestroy = FindObjectsOfType<OceanObjectMovement>();

        int count = 0;
        foreach (OceanObjectMovement obj in objectsToDestroy)
        {
            Destroy(obj.gameObject);
            count++;
        }
        Debug.Log($"Limpieza de nivel completada. {count} objetos destruidos.");
    }

    /// <summary>
    /// Recalcula el peso total y filtra los objetos que pueden aparecer. 
    /// Solo incluye objetos donde data.weight > 0.
    /// </summary>
    [ContextMenu("Recalculate Weights")]
    public void RecalculateSpawnWeights()
    {
        if (currentDayConfig == null) return;
        
        // 1. Filtrar solo los objetos que tienen un peso positivo
        availableSpawnData = currentDayConfig.spawnableObjects
            .Where(data => data.weight > 0)
            .ToList();

        // 2. Calcular el peso total para la selección ponderada, solo con los objetos filtrados
        totalWeight = availableSpawnData.Sum(data => data.weight);

        if (totalWeight <= 0)
        {
            Debug.LogWarning("No hay objetos activos con peso positivo para aparecer. Deteniendo el spawn.");
            StopSpawning(); // Detiene el spawn si ya no quedan objetos disponibles
        }
        else
        {
            Debug.Log($"Pesos de spawn recalculados. Total Weight: {totalWeight}");
        }
    }

    /// <summary>
    /// Corutina principal para manejar el tiempo de aparición de objetos.
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        isSpawning = true;
        while (isSpawning)
        {
            float waitTime = Random.Range(currentDayConfig.minSpawnInterval, currentDayConfig.maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // Solo intentar spawner si hay al menos un objeto activo
            if (totalWeight > 0)
            {
                SpawnObject();
            }
        }
    }

    /// <summary>
    /// Selecciona aleatoriamente un objeto, una posición y aplica la velocidad.
    /// </summary>
    private void SpawnObject()
    {
        // 1. Determinar el lado de spawn
        bool spawnFromLeft = Random.value > 0.5f;
        Collider2D spawnArea = spawnFromLeft ? leftSpawnArea : rightSpawnArea;
        
        // Determinar la dirección de movimiento: positiva para derecha, negativa para izquierda
        float directionX = spawnFromLeft ? currentDayConfig.moveSpeed : -currentDayConfig.moveSpeed;

        Bounds bounds = spawnArea.bounds;

        // 2. Determinar la posición de spawn
        float spawnY = Random.Range(bounds.min.y, bounds.max.y);
        float spawnX = spawnFromLeft ? bounds.min.x : bounds.max.x;
        
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0);

        // 3. Seleccionar el prefab mediante selección ponderada
        GameObject prefabToSpawn = GetWeightedRandomPrefab();

        if (prefabToSpawn == null)
        {
            // Esto solo pasa si GetWeightedRandomPrefab falla. 
            return;
        }

        // 4. Instanciar y configurar el movimiento
        GameObject newObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        
        // 4.1 Obtener el componente de movimiento
        OceanObjectMovement movement = newObject.GetComponent<OceanObjectMovement>();

        if (movement != null)
        {
            // Aplicar velocidad horizontal (traslación) y el bamboleo vertical.
            float randomWiggle = Random.Range(-currentDayConfig.maxVerticalWiggle, currentDayConfig.maxVerticalWiggle);
            
            // Inicializamos el movimiento.
            movement.InitializeMovement(directionX, randomWiggle, currentDayConfig.maxWiggleRate);
            
            // Rotar si viene de la derecha
            if (!spawnFromLeft)
            {
                newObject.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }
        else
        {
            // Si falta el componente, intentamos ponerle la velocidad inicial directamente al Rigidbody2D.
            Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float randomWiggle = Random.Range(-currentDayConfig.maxVerticalWiggle, currentDayConfig.maxVerticalWiggle);
                rb.linearVelocity = new Vector2(directionX, randomWiggle); 
            }
            Debug.LogWarning($"El objeto instanciado ({newObject.name}) no tiene el componente 'OceanObjectMovement', no se le pudo aplicar velocidad constante.");
        }
    }

    /// <summary>
    /// Selecciona un GameObject de forma aleatoria basándose en el 'weight' de los objetos DISPONIBLES.
    /// </summary>
    /// <returns>El prefab seleccionado.</returns>
    private GameObject GetWeightedRandomPrefab()
    {
        // Usamos la lista ya filtrada (availableSpawnData) y el peso total (totalWeight)
        float randomNumber = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var data in availableSpawnData)
        {
            currentWeight += data.weight;
            if (randomNumber <= currentWeight)
            {
                return data.prefab;
            }
        }
        return null; 
    }
}