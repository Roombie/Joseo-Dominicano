using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro; 

public class DebugDaysManager : MonoBehaviour
{
    [SerializeField] GameManager _gameManager;
    
    [Header("Días y UI")]
    [SerializeField] DebugDaysItem _template;
    [SerializeField] Transform _contentParent;
    [Tooltip("ScriptableObject LevelDayConfig que se usará como plantilla para crear nuevos días.")]
    [SerializeField] LevelDayConfig _dayTemplateSO;

    [Header("Configuración Global")]
    [SerializeField] private FloatVariable maxTotalOxygen;
    [SerializeField] TMP_InputField _oxygenField;
    [SerializeField] TMP_InputField _sackLimitField;
    
    List<DebugDaysItem> _debugDaysItems = new List<DebugDaysItem>();

    private void Start()
    {
        // Asegúrate de que la plantilla de UI esté inactiva al inicio
        if (_template != null)
        {
            _template.gameObject.SetActive(false);
        }
        FetchItems();
    }

// -------------------------------------------------------------------
// LÓGICA DE CARGA Y GESTIÓN DE LA UI
// -------------------------------------------------------------------

    /// <summary>
    /// Crea ítems de UI a partir de la lista de días del GameManager.
    /// Cada ítem de UI recibe una referencia directa a su LevelDayConfig (ScriptableObject).
    /// </summary>
    public void FetchItems()
    {
        // 1. Limpiar ítems existentes en la UI y la lista
        foreach (var item in _debugDaysItems)
        {
            Destroy(item.gameObject);
        }
        _debugDaysItems.Clear();

        // 2. Poblar ítems a partir de la lista days del _gameManager
        // Importante: No se necesita clonar aquí, solo se usa la referencia directa
        for (int i = 0; i < _gameManager.days.Count; i++)
        {
            LevelDayConfig levelConfig = _gameManager.days[i];
            
            DebugDaysItem newItem = Instantiate(_template, _contentParent);

            // Inicializar el ítem de UI. Pasa el LevelDayConfig SO por referencia.
            newItem.Initialize(i, levelConfig, RemoveItemByComponent);

            newItem.gameObject.SetActive(true);
            _debugDaysItems.Add(newItem);
        }

        // Actualiza el estado de los botones después de cargar
        UpdateRemoveButtonStates();

        _sackLimitField.text = _gameManager._playerSackCarrySpaceLimit.ToString();
        _oxygenField.text = maxTotalOxygen.value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Añade un nuevo día. Instancia un nuevo ScriptableObject LevelDayConfig.
    /// </summary>
    public void AddItem()
    {
        if (_template == null || _dayTemplateSO == null)
        {
            Debug.LogError("La plantilla de UI o la plantilla de LevelDayConfig SO están sin asignar.");
            return;
        }

        // 1. Instanciar (Clonar) el LevelDayConfig ScriptableObject
        // Esto crea una copia independiente del SO en memoria.
        LevelDayConfig newDayConfig = Instantiate(_dayTemplateSO);
        
        // Opcional: Asignarle un nombre para el Editor (si se persiste).
        newDayConfig.name = $"Día { _gameManager.days.Count + 1} (Debug)";
        
        // 2. Añadir el nuevo SO al GameManager (la lista de referencias)
        _gameManager.days.Add(newDayConfig); 

        // 3. Crear e inicializar la nueva instancia de UI
        DebugDaysItem newItem = Instantiate(_template, _contentParent);

        int newIndex = _debugDaysItems.Count;
        // Inicializar el ítem de UI con la nueva referencia de SO.
        newItem.Initialize(newIndex, newDayConfig, RemoveItemByComponent);
        
        // 4. Agregar a la lista del Manager
        newItem.gameObject.SetActive(true);
        _debugDaysItems.Add(newItem);

        // Re-enumerar para asegurar que el título sea correcto
        ReIndexItems();
        // Actualiza el estado de los botones después de añadir
        UpdateRemoveButtonStates();
    }

    /// <summary>
    /// Elimina un ítem de la lista por su referencia de componente.
    /// También remueve el LevelDayConfig asociado del GameManager.
    /// </summary>
    public void RemoveItemByComponent(DebugDaysItem itemToRemove)
    {
        // Guardar la referencia al SO antes de remover
        LevelDayConfig configToRemove = itemToRemove.levelConfigReference;

        // 1. Eliminar del array del GameManager (remueve la referencia del SO)
        _gameManager.days.Remove(configToRemove);
        
        // 2. Eliminar de la lista interna del manager
        _debugDaysItems.Remove(itemToRemove);
        
        // 3. Destruir el GameObject de la UI
        Destroy(itemToRemove.gameObject);

        // 4. (Opcional) Destruir la instancia del ScriptableObject que clonamos al añadir
        if (!Application.isEditor) // Si estamos en una build, es seguro destruirlo
        {
            Destroy(configToRemove);
        }
        
        // 5. Re-enumerar los títulos
        ReIndexItems();
        
        // Actualiza el estado de los botones después de eliminar
        UpdateRemoveButtonStates();
    }

    /// <summary>
    /// Re-enumera los títulos de los ítems en la UI para reflejar el orden actual.
    /// </summary>
    private void ReIndexItems()
    {
        for (int i = 0; i < _debugDaysItems.Count; i++)
        {
            _debugDaysItems[i].title.text = $"Día {i + 1}";
        }
    }
    
    /// <summary>
    /// Habilita o deshabilita los botones de eliminar, permitiendo la eliminación solo si hay más de un día.
    /// </summary>
    private void UpdateRemoveButtonStates()
    {
        bool canRemove = _debugDaysItems.Count > 1;

        foreach (var item in _debugDaysItems)
        {
            item.SetRemoveInteractable(canRemove);
        }
    }
    
// -------------------------------------------------------------------
// LÓGICA DE CONFIRMACIÓN Y GUARDADO
// -------------------------------------------------------------------

    /// <summary>
    /// Itera sobre todos los DebugDaysItem y les pide que guarden sus valores de UI
    /// (Quota, Duration) de vuelta a su LevelDayConfig de referencia (SO).
    /// </summary>
    public void ConfirmValues()
    {
        // 1. Guardar los valores de Quota y Duration para cada día
        // Nota: Los valores de SpawnData (weight, canSpawn) ya se guardan en tiempo real.
        foreach (var item in _debugDaysItems)
        {
            item.SaveValuesToConfig();
        }

        // 2. Guardar los valores globales
        // Uso de InvariantCulture para evitar problemas de localización con el punto decimal
        int.TryParse(_sackLimitField.text, out _gameManager._playerSackCarrySpaceLimit);
        float.TryParse(_oxygenField.text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out maxTotalOxygen.value);
        
        Debug.Log($"¡Valores de días confirmados y guardados en GameManager! Total de días: {_gameManager.days.Count}");
    }
}