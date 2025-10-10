using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Linq;
using System.Collections.Generic;



public class Spawner : MonoBehaviour
{
    [Header("Spawner instructions")]
    [TextArea(3, 5)] // Define el �rea de texto con m�nimo 3 y m�ximo 5 l�neas
    public string Instructions = "- Add spawn prefabs as children of spawner. \n" +
                                "Some functionality depends on the player having the script PlayerRoomTracker which detects triggers on the 'RoomsID' layermask, remember to assign a Scriptable Object RoomTracker to PlayerRoomTracker \n" +
                                "- If set to point, position each cildren in desired spawn position in scene \n" +
                                "- If set to area, add a SpawnArea as Spawner child for each spawning area desired. \n" +
                                "  -- Add spawns as children of these areas.\r\n";

    [Header("Pro Tips")]
    [TextArea(3, 5)] // Define el �rea de texto con m�nimo 3 y m�ximo 5 l�neas
    public string proTips = "- Drag prefabs to hierarchy outside of any parents first and then drag them to Spawnareas. \n" +
                            "- Any direct child of Spawner will act as a SpawnArea and their children will spawn normally.";



    [Header("Caution")]
    [TextArea(3, 5)] // Define el �rea de texto con m�nimo 3 y m�ximo 5 l�neas
    public string Caution = "- If child obj is destroyed spawning of this obj stops. \n" +
                            "  Check your code to avoid this error. But you can use it on purpose. \n" +
                            "- Layers must be properly setup to use this component \n" +
                            "- Setting Spawn range too wide on a scene with few available room to spawn may lead to crashes";

    [Tooltip("Only spawns while current level is same as Active Level")]
    public bool isLevelDependant = false;

    [Tooltip("The level at which spawning should happen")]
    public int activeLevel = 1;

    [Tooltip("Modify this field to track current level")]

    public int currentLevel;

    [Tooltip("Deactivate to stop spawning, to reactivate script must be disabled and reenabled")]
    public bool continousSpawn = true;
    public bool launchInStart = true;

    [Tooltip("In seconds")]
    public float spawnFrecuency = 1f;

    public enum SpawnLocationFocus
    {
        player,
        point,
        area
    }

    [Tooltip("player: spawns around player current position at defined minimum distance \n" +
            "Point: spawns always in the same spot if available \n" +
            "Area: spawns at random location in an area defined by a GameObject with 2D Collider placed as child of the spawner and parent of the prefabs to spawn")]
    public SpawnLocationFocus spawnLocationFocus;


    public enum MaxNumberIndicator
    {
        tag,
        childObject
    }

    [Tooltip("Tag: max spawn number depends on all objects with the same tag of the spawn regardless of the amount of child objects of the spawner \n" +
            "Child Object: max spawn number counts for each child prefab of the spawner ")]
    public MaxNumberIndicator maxNumberIndicator;
    public GameObject player;

    public int maxSpawns = 30;


    [Tooltip("When focus in player this is the max distance of spawning from the player \n")]
    public int spawnRange = 20;
    public int closestSpawnFomPlayer = 5;
    public int closestSpawnFromEnemies = 3;


    [Tooltip("Defines the ground, objects only spawn on top of a ground object or tilemap")]
    public LayerMask groundLayer;

    [Tooltip("Defines the objects that the spawner can't collide with or spawn on top of them")]
    public LayerMask obstacleLayer;
    public UnityEvent<GameObject> onSpawn;

    private RoomTracker playerCurrentRoom;

    private bool levelReached = false;
    private bool isSpawning = false;


    // In non-area modes, spawnPrefabs are the direct children of the spawner.
    // In area mode, they come from inside spawn area objects.
    private List<GameObject> spawnPrefabs = new List<GameObject>();

    private Dictionary<GameObject, List<GameObject>> instantiatedPrefabs = new Dictionary<GameObject, List<GameObject>>();

    // -------- For area mode only ----------
    // Mapping each spawnable prefab to the spawn area (its parent) that determines its random bounds.
    private Dictionary<GameObject, Transform> spawnAreaMapping = new Dictionary<GameObject, Transform>();

    // NEW: Dictionary to store the original scale of each prefab as loaded
    private Dictionary<GameObject, Vector3> compensatedScales = new Dictionary<GameObject, Vector3>();
    private Vector2 playerPosition;
    private int spawnCounter;
    private Collider2D roomCollider;

    const int randomMaxAttempts = 50; // Limit attempts to avoid infinite loops

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerPosition = player.transform.position;
        
        if (launchInStart) LaunchSpawner();

        //Use to locate the current room/area the player is in:
        playerCurrentRoom = player.GetComponent<PlayerRoomTracker>().playerCurrentRoom;
    }

    void Update()
    {
        if (!levelReached && currentLevel == activeLevel)
        {
            levelReached = true;
            LaunchSpawner();
        }

        playerPosition = player.transform.position;
        if (!continousSpawn || currentLevel > activeLevel && isLevelDependant)
        {
            StopAllCoroutines();
        }
    }

    public void LaunchSpawner()
    {        
        //Loads all children gameobjects as prefabs to spawn
        //Different for area mode
       //Debug.Log("Launch called");
        if (!levelReached)
        {
       //Debug.Log("Level not reached");
            if (spawnLocationFocus == SpawnLocationFocus.area)
            {
                PopulateSpawnListFromAreas();
            }
            else
            {
                PopulateSpawnList(); // None area mode: direct children of spawner
            }
        }
        if (levelReached || !isLevelDependant)
        {
       //Debug.Log("Level reached");

            if (!isSpawning)
            {
                if (spawnLocationFocus == SpawnLocationFocus.area)
                {
                    PopulateSpawnListFromAreas();
                }
                else
                {
                    PopulateSpawnList(); // None area mode: direct children of spawner
                }
            }

            foreach (var spawnPrefab in spawnPrefabs)
            {
                if (maxNumberIndicator == MaxNumberIndicator.childObject)
                {
                    instantiatedPrefabs[spawnPrefab] = new List<GameObject>(); // Initialize a dictionary of listsfor each prefab
                }
                else
                {
                    CountObjectsOnScene(spawnPrefab);
                }

                if (continousSpawn)
                {

                    switch (spawnLocationFocus)
                    {
                        case SpawnLocationFocus.player:
                           //Debug.Log("Start corroutine started");
                            StartCoroutine(SpawnCoroutine(spawnPrefab));
                            break;

                        case SpawnLocationFocus.point:
                            StartCoroutine(SpawnCoroutine(spawnPrefab, spawnPrefab.transform.position));
                            break;

                        case SpawnLocationFocus.area:
                            // For area mode, we use the mapped parent spawn area
                            if (spawnAreaMapping.ContainsKey(spawnPrefab))
                            {
                                Transform spawnArea = spawnAreaMapping[spawnPrefab];

                                // Retrieve the compensated scale from compensatedScale dictionary.
                                Vector3 compensatedScale = compensatedScales[spawnPrefab];

                                StartCoroutine(SpawnCoroutine(spawnPrefab, spawnArea, compensatedScale));
                            }


                            break;
                    }
                }

            }
        }

        isSpawning = true;
    }

    void PopulateSpawnList()
    {
       //Debug.Log("Populate list called");
        spawnPrefabs.Clear();

        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf) // Only store active children
            {
                spawnPrefabs.Add(child.gameObject); // Store child as prefab reference
            }
            child.gameObject.SetActive(false); //Inactive object to avoid changes on the reference for spawning
        }

        //Debug.Log("Spawn list populated with " + spawnPrefabs.Count + " prefabs.");
    }

    // -------------------------------------------------------------------------
    // For area mode: the spawner�s children are the spawn areas.
    // Loop through each spawn area and then add its children (the spawnable prefabs).
    // Also map each prefab to its parent spawn area.
    void PopulateSpawnListFromAreas()
    {
        spawnPrefabs.Clear();
        spawnAreaMapping.Clear();

        // Each child of this spawner is assumed to be a spawn area, therefore it doesn't check by name.
        foreach (Transform spawnArea in transform)
        {
            spawnArea.GetComponent<SpriteRenderer>().enabled = false;
            // Optionally, you can check the spawn area's name if necessary:
            // if (spawnArea.name != "SomeIdentifier") continue;
            foreach (Transform spawnable in spawnArea)
            {
                // add the spawnable prefab from inside the spawn area
                spawnPrefabs.Add(spawnable.gameObject);
                spawnAreaMapping[spawnable.gameObject] = spawnArea;

                // Get the spawn area's scale.
                Vector3 spawnAreaScale = spawnArea.transform.localScale;

                // Compensate the spawnable's scale by dividing by the spawn area's scale.
                // This calculates what the object's scale would be if it were not being shrunk by its parent.
                compensatedScales[spawnable.gameObject] = new Vector3(
                    spawnable.localScale.x * spawnAreaScale.x,
                    spawnable.localScale.y * spawnAreaScale.y,
                    spawnable.localScale.z * spawnAreaScale.z);

                // Disable the object that serves as model for spawns to avoid it being destroyed or modify.
                //Any changes to this object would change the spawns
                spawnable.gameObject.SetActive(false);
            }
            // Optionally, leave the spawn area active if you need to see its area in the scene.
        }

        //Debug.Log("Area mode: Spawn list populated with " + spawnPrefabs.Count + " prefabs from spawn areas.");
    }

    void CountObjectsOnScene(GameObject spawnPrefab)
    {
        switch (maxNumberIndicator)
        {
            case MaxNumberIndicator.tag:

                if (!spawnPrefab.CompareTag("Untagged"))
                {
                    //Finds all objects in scene with same tag to keep count
                    spawnCounter = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Count(obj => obj.CompareTag(spawnPrefab.tag));
                }
                else
                {
                    Debug.LogError("Spawn Prefab" + spawnPrefab.name + ", is missing tag");
                }
                break;

            case MaxNumberIndicator.childObject:
                RemoveDestroyedObjects();

                //Count the number of clones listed (List) stored in dictionary for this prefab gameobject (key).
                if (instantiatedPrefabs.ContainsKey(spawnPrefab))
                    spawnCounter = instantiatedPrefabs[spawnPrefab].Count;
                else
                    spawnCounter = 0;
                break;
        }


    }

    IEnumerator SpawnCoroutine(GameObject spawnPrefab)
    {
        while (spawnPrefab != null)
        {
            SpawnObject(spawnPrefab, GetRandomLocation());

            yield return new WaitForSeconds(spawnFrecuency);
        }

    }
    IEnumerator SpawnCoroutine(GameObject spawnPrefab, Vector2 prefabPosition)
    {
        while (spawnPrefab != null)
        {
            SpawnObject(spawnPrefab, prefabPosition);

            yield return new WaitForSeconds(spawnFrecuency);
        }
    }
    IEnumerator SpawnCoroutine(GameObject spawnPrefab, Transform spawnAreaTransform, Vector3 compensatedScale)
    {
        while (spawnPrefab != null)
        {
            SpawnObject(spawnPrefab, GetRandomLocation(spawnAreaTransform), compensatedScale);

            yield return new WaitForSeconds(spawnFrecuency);
        }
    }

    public void SpawnObject(GameObject spawnPrefab, Vector2 position)
    {


        //Counts all spawn prefabs of the same type to keep number within range
        CountObjectsOnScene(spawnPrefab);
        if (spawnCounter < maxSpawns) //Prevents spawning beyond max spawn limit
        {
            if (position != new Vector2(-500, -500)) //Manages random position failures avoiding crashes, uses a vector to ignore
            {
                if (!CheckValidPosition(position))
                    return; // Avoids spawning more than one item on same spot when using focus on point spawning

                ////Only one object with Item component can exist in the same position at a time
                //if(spawnPrefab.GetComponent<Item>() != null)
                //{
                //    if(spawnCounter > 0)
                //    {
                //        return; // Avoids spawning more than one item
                //    }
                //}

                GameObject spawn = Instantiate(spawnPrefab, position, Quaternion.identity, null);
                spawn.transform.localScale = Vector3.one;

                //Set prefab to active
                spawn.gameObject.SetActive(true);
                onSpawn?.Invoke(spawn);

                if (instantiatedPrefabs.ContainsKey(spawnPrefab))
                {
                    instantiatedPrefabs[spawnPrefab].Add(spawn); // Store in dictionary
                }
            }
        }
    }

    // NEW: Overload that accepts the original scale.
    public void SpawnObject(GameObject spawnPrefab, Vector2 position, Vector3 compensatedScale)
    {
        CountObjectsOnScene(spawnPrefab);
        if (spawnCounter < maxSpawns)
        {
            if (position != new Vector2(-500, -500))
            {
                GameObject spawn = Instantiate(spawnPrefab, position, Quaternion.identity);

                // Set the spawn object's scale to the original (as stored earlier).
                spawn.transform.localScale = new Vector3(compensatedScale.x, compensatedScale.y, compensatedScale.z);
                spawn.SetActive(true);
                onSpawn?.Invoke(spawn);

                if (instantiatedPrefabs.ContainsKey(spawnPrefab))
                {
                    instantiatedPrefabs[spawnPrefab].Add(spawn);
                }
            }
        }
    }
    
    Vector2 GetRandomLocation()
    {
        Vector2 randomPosition;

        //Calculates intersection between spawnRange and current room bounds

        if (playerCurrentRoom.currentRoom != null && roomCollider == null)
        {
            roomCollider = playerCurrentRoom.currentRoom.GetComponent<Collider2D>();
        }
        //Debug.Log("Spawner calling randomLocation, playerCurrentRoom.currentRoom not null, name: " + playerCurrentRoom.currentRoom.name);

        // Get the room's collider and its bounds

        //Debug.Log("Room collider found: " + roomCollider);


        if (roomCollider == null)
        {

            Debug.LogError("The playerCurrentRoom GameObject does not have a Collider2D");
            return new Vector2(-500, -500);
        }
        Bounds roomBounds = roomCollider.bounds;
        //Debug.Log("Room bounds: " + roomBounds);

        // Define the spawn area relative to the player's position:
        float spawnMinX = playerPosition.x - spawnRange;
        float spawnMaxX = playerPosition.x + spawnRange;
        float spawnMinY = playerPosition.y - spawnRange;
        float spawnMaxY = playerPosition.y + spawnRange;

        // Now, calculate the intersection between the room bounds
        float intersectMinX = Mathf.Max(roomBounds.min.x, spawnMinX);
        float intersectMaxX = Mathf.Min(roomBounds.max.x, spawnMaxX);
        float intersectMinY = Mathf.Max(roomBounds.min.y, spawnMinY);
        float intersectMaxY = Mathf.Min(roomBounds.max.y, spawnMaxY);

        //Debug.Log("Intersection bounds: " + intersectMinX + ", " + intersectMaxX + ", " + intersectMinY + ", " + intersectMaxY);

        if (intersectMinX > intersectMaxX || intersectMinY > intersectMaxY)
        {
            Debug.LogWarning("The spawn range and the room bounds do not overlap.");
            return new Vector2(-500, -500);
        }

        //Debug.Log("Spawn range and room bounds overlap. Attempting Random location");
        for (int attempt = 0; attempt < randomMaxAttempts; attempt++)
        {
            float x = Random.Range(intersectMinX, intersectMaxX);
            float y = Random.Range(intersectMinY, intersectMaxY);

            randomPosition = new Vector2(x, y);
            //Debug.Log("Random position generated: " + randomPosition);

            if (CheckValidPosition(randomPosition))
            {
                return randomPosition; // Return a valid position immediately
            }
        }


        Debug.LogWarning("No valid spawn position found.");
        return new Vector2(-500, -500); // Sends a default error vector to be ignored by SpawnEnemy();
    }


    //OVERLOAD to generate random on a spawnArea
    Vector2 GetRandomLocation(Transform spawnAreaTransform)
    {
        Vector2 randomPosition;
        Vector2 center = spawnAreaTransform.position;
        Vector2 size = spawnAreaTransform.localScale / 2; // Assumes scale represents area size

        for (int attempt = 0; attempt < randomMaxAttempts; attempt++)
        {
            float x = Random.Range(center.x - size.x, center.x + size.x);
            float y = Random.Range(center.y - size.y, center.y + size.y);

            randomPosition = new Vector2(x, y);

            if (CheckValidPosition(randomPosition))
            {
                return randomPosition; // Return a valid position immediately
            }
        }
        Debug.LogWarning("No valid spawn position found.");
        return new Vector2(-500, -500); // Sends a default error vector to be ignored by SpawnEnemy();

    }

    bool CheckValidPosition(Vector2 position)
    {
        //Debug.Log("Posiion on ground: " + IsPositionOnGround(position));
        //Debug.Log("Position colliding: " + IsPositionColliding(position));
        //Debug.Log("Too close: " + (Vector2.Distance(playerPosition, position) < closestSpawnFromEnemies));
        if (!IsPositionOnGround(position) || IsPositionColliding(position) || Vector2.Distance(playerPosition, position) < closestSpawnFromEnemies)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    bool IsPositionOnGround(Vector2 position)
    {
        //Debug.Log("Ground: " + Physics2D.OverlapPoint(position, groundLayer));
        return Physics2D.OverlapPoint(position, groundLayer) != null;
    }
    bool IsPositionColliding(Vector2 position)
    {
        //Debug.Log("Obstacles: " + Physics2D.OverlapCircle(position, closestSpawnFromEnemies, obstacleLayer));
        return Physics2D.OverlapCircle(position, closestSpawnFromEnemies, obstacleLayer) != null;
    }

    void RemoveDestroyedObjects()
    {
        foreach (var key in instantiatedPrefabs.Keys.ToList())  // Use ToList() to iterate safely.
        {
            instantiatedPrefabs[key].RemoveAll(obj => obj == null);
            // Ensure the key remains even if the list is empty.
        }

    }

    public void StopSpawning()
    {
        levelReached = false;
        StopAllCoroutines();
        isSpawning = false;
    }
}
