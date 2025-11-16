using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.Events;

// Requiere que el LevelDayConfig sea un ScriptableObject
public class DebugDaysItem : MonoBehaviour
{
    [Header("Day Config UI")]
    public TMP_Text title;
    public TMP_InputField quotaField;
    public TMP_InputField timerField;
    public Button removeItem;

    [Header("Spawn Config UI")]
    [Tooltip("Botón para alternar la visibilidad del panel de spawn.")]
    public Button toggleSpawnPanelButton;
    [Tooltip("Panel que contiene la lista de DebugSpawnItem.")]
    public GameObject spawnItemsPanel;

    [Header("Spawn Item Templates")]
    [Tooltip("Plantilla para los objetos spawnables (debe asignarse el prefab de DebugSpawnItem).")]
    public DebugSpawnItem spawnItemTemplate;
    [Tooltip("Parent transform donde se instanciarán los DebugSpawnItem.")]
    public Transform spawnItemContentParent;

    // Referencia al LevelDayConfig real (¡ahora un ScriptableObject!)
    [HideInInspector] public LevelDayConfig levelConfigReference;

    private List<DebugSpawnItem> _spawnItems = new List<DebugSpawnItem>();

    private void Awake()
    {
        // Asegura que las plantillas y el panel de spawn estén inactivos al inicio.
        if (spawnItemTemplate != null)
        {
            spawnItemTemplate.gameObject.SetActive(false);
        }
        if (spawnItemsPanel != null)
        {
            spawnItemsPanel.SetActive(false);
        }

        // Configurar el listener del botón para desplegar/contraer el panel
        if (toggleSpawnPanelButton != null && spawnItemsPanel != null)
        {
            toggleSpawnPanelButton.onClick.AddListener(ToggleSpawnPanel);
        }
    }
    
    /// <summary>
    /// Alterna la visibilidad del panel de ítems de aparición.
    /// </summary>
    public void ToggleSpawnPanel()
    {
        if (spawnItemsPanel != null)
        {
            spawnItemsPanel.SetActive(!spawnItemsPanel.activeSelf);

            if (spawnItemsPanel.activeSelf)
            {
                for (int i = 0; i < spawnItemContentParent.transform.childCount; i++)
                {
                    spawnItemContentParent.transform.GetChild(i).gameObject.SetActive(false);
                }

                for (int i = 0; i < _spawnItems.Count; i++)
                {
                    _spawnItems[i].gameObject.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// Inicializa el ítem de día completo con la configuración y el callback de remoción.
    /// </summary>
    public void Initialize(int dayIndex, LevelDayConfig config, Action<DebugDaysItem> onRemoveCallback)
    {
        levelConfigReference = config;
        title.text = $"Día {dayIndex + 1}";
        
        // Configuración de día (cuota, duración)
        quotaField.text = config.quota.ToString();
        timerField.text = config.duration.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Configurar el botón de eliminar.
        removeItem.onClick.RemoveAllListeners();
        removeItem.onClick.AddListener(() => onRemoveCallback(this));

        // Generar la lista de objetos de aparición.
        GenerateSpawnItems();
    }

    /// <summary>
    /// Genera la lista de UI para cada SpawnData dentro del LevelDayConfig.
    /// </summary>
    private void GenerateSpawnItems()
    {
        // Limpiar ítems de spawn viejos
        foreach (var item in _spawnItems)
        {
            Destroy(item.gameObject);
        }
        _spawnItems.Clear();

        if (levelConfigReference?.spawnableObjects == null)
        {
            Debug.LogError($"LevelDayConfig.spawnableObjects es nulo para {title.text}. No se puede mostrar la configuración de aparición.");
            return;
        }

        // Crear nuevos ítems de UI para cada SpawnData
        foreach (var spawnData in levelConfigReference.spawnableObjects)
        {
            DebugSpawnItem newItem = Instantiate(spawnItemTemplate, spawnItemContentParent);
            newItem.Initialize(spawnData);
            newItem.gameObject.SetActive(true);
            _spawnItems.Add(newItem);
        }
    }

    /// <summary>
    /// Guarda los valores de Quota y Duration de la UI de vuelta al LevelDayConfig (ScriptableObject).
    /// Las propiedades de SpawnData ya se actualizan en tiempo real desde DebugSpawnItem.
    /// </summary>
    public void SaveValuesToConfig()
    {
        if (levelConfigReference == null) return;

        // 1. Guardar Quota
        if (int.TryParse(quotaField.text, out int quota))
        {
            levelConfigReference.quota = quota;
        }
        else
        {
            Debug.LogError($"Entrada de Quota inválida para {title.text}. Quota no guardada.");
        }
        
        // 2. Guardar Duration
        if (float.TryParse(timerField.text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float duration))
        {
            levelConfigReference.duration = duration;
        }
        else
        {
            Debug.LogError($"Entrada de Duration inválida para {title.text}. Duration no guardada.");
        }
        
        // El resto de la configuración de SpawnData se actualiza inmediatamente por DebugSpawnItem.
        
        // Opcional: Marcar el SO como sucio para guardado si estamos en el Editor
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(levelConfigReference);
        #endif
    }

    // Permite al manager habilitar/deshabilitar el botón de remover
    public void SetRemoveInteractable(bool interactable)
    {
        removeItem.interactable = interactable;
    }
}