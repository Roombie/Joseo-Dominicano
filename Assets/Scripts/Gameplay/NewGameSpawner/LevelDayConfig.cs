using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DayConfig", menuName = "Juego de Nado/Configuración del Día", order = 1)]
public class LevelDayConfig : ScriptableObject
{
    public int quota;
    
    public float duration;

    [Header("Configuración de Aparición (Spawn)")]
    [Tooltip("Lista de objetos que pueden aparecer en este día, junto con sus pesos relativos.")]
    public List<SpawnData> spawnableObjects = new List<SpawnData>();

    [Tooltip("Tiempo mínimo y máximo entre la aparición de dos objetos.")]
    [Range(0.5f, 5f)]
    public float minSpawnInterval = 1.0f;
    [Range(0.5f, 5f)]
    public float maxSpawnInterval = 2.5f;

    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad horizontal a la que se moverán los objetos.")]
    public float moveSpeed = 3f;

    [Tooltip("Un factor de velocidad vertical aleatorio para dar la sensación de movimiento en el agua.")]
    [Range(0f, 1f)]
    public float maxVerticalWiggle = 0.5f;
}