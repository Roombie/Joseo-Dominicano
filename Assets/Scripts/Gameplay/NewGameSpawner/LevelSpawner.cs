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
    public void SetCurrentDayConfig(LevelDayConfig day) => currentDayConfig = day;

    [Header("Pre-Warm")]
    [Tooltip("Tiempo en segundos que se simulará el spawn antes de que el nivel comience oficialmente.")]
    [SerializeField] private float preWarmSeconds = 5f; // NUEVO: Tiempo de simulación inicial

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
    /// Solo incluye objetos donde data.weight > 0 y data.canSpawn es true.
    /// (Se añadió la condición data.canSpawn para usar el campo de SpawnData).
    /// </summary>
    [ContextMenu("Recalculate Weights")]
    public void RecalculateSpawnWeights()
    {
        if (currentDayConfig == null) return;
        
        // 1. Filtrar solo los objetos que tienen un peso positivo Y pueden aparecer
        availableSpawnData = currentDayConfig.spawnableObjects
            .Where(data => data.weight > 0 && data.canSpawn)
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
    /// Ahora inicia llamando a la fase de PreWarm.
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        isSpawning = true;

        // 1. Fase de Pre-Warm: Simula y genera objetos ya trasladados.
        PreWarm(preWarmSeconds);
        Debug.Log($"Pre-Warm completado. Tiempo simulado: {preWarmSeconds} segundos.");

        // 2. Fase de Spawn en tiempo real: Retoma el ciclo normal.
        while (isSpawning)
        {
            float waitTime = Random.Range(currentDayConfig.minSpawnInterval, currentDayConfig.maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // Solo intentar spawner si hay al menos un objeto activo
            if (totalWeight > 0)
            {
                SpawnObject(0f); // elapsedTime = 0f para spawn en tiempo real
            }
        }
    }

    /// <summary>
    /// Simula la aparición de objetos durante un tiempo dado y los posiciona en su ubicación actual.
    /// </summary>
    private void PreWarm(float duration)
    {
        // Solo si hay duración de pre-warm y objetos disponibles
        if (duration <= 0f || totalWeight <= 0) return;

        float simulatedTime = 0f;
        // Calcula el tiempo hasta la primera aparición simulada
        float nextSpawnTime = Random.Range(currentDayConfig.minSpawnInterval, currentDayConfig.maxSpawnInterval);

        while (simulatedTime < duration)
        {
            if (simulatedTime >= nextSpawnTime)
            {
                // 1. Calcular el tiempo que el objeto lleva en movimiento (si hubiera sido spawn en nextSpawnTime)
                float timeSinceSpawn = duration - nextSpawnTime;

                // 2. Spawnea el objeto en la posición de avance
                SpawnObject(timeSinceSpawn);

                // 3. Calcular el tiempo para la *siguiente* aparición simulada
                float interval = Random.Range(currentDayConfig.minSpawnInterval, currentDayConfig.maxSpawnInterval);
                nextSpawnTime += interval;
            }

            // Avanzar el tiempo simulado al momento del siguiente spawn o al final de la duración
            // Usamos Mathf.Min para avanzar lo justo hasta el siguiente evento o hasta el final.
            float timeToAdvance = Mathf.Min(duration - simulatedTime, nextSpawnTime - simulatedTime);
            
            // Previene bucles si el cálculo de tiempo es muy pequeño o cero.
            if (timeToAdvance <= 0f) 
            {
                // Si estamos en un punto exacto de spawn, aseguramos que el loop continúe si aún queda tiempo
                if (simulatedTime < duration && simulatedTime == nextSpawnTime)
                {
                    // El siguiente objeto ya se ha calculado en el 'if' superior. Salir.
                    break; 
                }
                break;
            }
            
            simulatedTime += timeToAdvance;
        }
    }


    /// <summary>
    /// Selecciona aleatoriamente un objeto, una posición y aplica la velocidad.
    /// </summary>
    /// <param name="elapsedTime">Tiempo transcurrido desde que se "spawnearía" el objeto. Se usa para calcular la posición inicial avanzada durante el Pre-Warm.</param>
    private void SpawnObject(float elapsedTime)
    {
        // 1. Determinar el lado de spawn
        bool spawnFromLeft = Random.value > 0.5f;
        Collider2D spawnArea = spawnFromLeft ? leftSpawnArea : rightSpawnArea;
        
        // Determinar la dirección de movimiento: positiva para derecha, negativa para izquierda
        float baseDirectionX = spawnFromLeft ? currentDayConfig.moveSpeed : -currentDayConfig.moveSpeed;

        Bounds bounds = spawnArea.bounds;

        // 2. Determinar la posición de spawn inicial (en el borde)
        float spawnY = Random.Range(bounds.min.y, bounds.max.y);
        float initialSpawnX = spawnFromLeft ? bounds.min.x : bounds.max.x;
        
        // 2.1. Calcular la posición avanzada si hay tiempo transcurrido (para Pre-Warm)
        float finalSpawnX = initialSpawnX + (baseDirectionX * elapsedTime); 
        
        Vector3 spawnPosition = new Vector3(finalSpawnX, spawnY, 0); // Usamos finalSpawnX

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
        // NOTA: El código original tenía un pequeño bug en RecalculateSpawnWeights, he añadido data.canSpawn en esa lógica.
        OceanObjectMovement movement = newObject.GetComponent<OceanObjectMovement>();

        if (movement != null)
        {
            // Aplicar velocidad horizontal (traslación) y el bamboleo vertical.
            float randomWiggle = Random.Range(-currentDayConfig.maxVerticalWiggle, currentDayConfig.maxVerticalWiggle);
            
            // Inicializamos el movimiento.
            movement.InitializeMovement(baseDirectionX, randomWiggle, currentDayConfig.maxWiggleRate);
            
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
                rb.linearVelocity = new Vector2(baseDirectionX, randomWiggle); 
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