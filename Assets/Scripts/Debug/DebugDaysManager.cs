using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro; // Necesario para la conversión de la lista a array

public class DebugDaysManager : MonoBehaviour
{
    [SerializeField] GameManager _gameManager;
    [SerializeField] DebugDaysItem _template;
    // Debes asignar el Transform que es el padre de todos los ítems de la lista (por ejemplo, el Content de un ScrollView)
    [SerializeField] Transform _contentParent;
    [SerializeField] private FloatVariable maxTotalOxygen;
    [SerializeField] TMP_InputField _oxygenField;
    [SerializeField] TMP_InputField _sackLimitField;
    List<DebugDaysItem> _debugDaysItems = new List<DebugDaysItem>();

    private void Start()
    {
        // Asegúrate de que la plantilla esté inactiva al inicio
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
    /// Crea ítems de UI a partir del array de días del GameManager.
    /// </summary>
    public void FetchItems()
    {
        // 1. Limpiar ítems existentes en la UI y la lista
        foreach (var item in _debugDaysItems)
        {
            Destroy(item.gameObject);
        }
        _debugDaysItems.Clear();

        // 2. Poblar ítems a partir del array days del _gameManager
        for (int i = 0; i < _gameManager.days.Length; i++)
        {
            GameManager.Level level = _gameManager.days[i];
            
            DebugDaysItem newItem = Instantiate(_template, _contentParent);

            // 3. Inicializar cada ítem con los valores del día y el título
            newItem.title.text = $"Día {i + 1}";
            newItem.quotaField.text = level.dayQuota.ToString();
            newItem.timerField.text = level.dayDuration.ToString();

            // 4. Configurar el botón de eliminar usando el método por componente.
            newItem.removeItem.onClick.AddListener(() => RemoveItemByComponent(newItem));

            newItem.gameObject.SetActive(true);
            _debugDaysItems.Add(newItem);
        }

        ReIndexItems();
        // 🌟 NUEVO: Actualiza el estado de los botones después de cargar
        UpdateRemoveButtonStates();

        _sackLimitField.text = _gameManager._playerSackCarrySpaceLimit.ToString();
        _oxygenField.text = maxTotalOxygen.value.ToString();
    }

    /// <summary>
    /// Añade un nuevo ítem de día con valores por defecto.
    /// </summary>
    public void AddItem()
    {
        if (_template == null) return;

        // 1. Crear una nueva instancia
        DebugDaysItem newItem = Instantiate(_template, _contentParent);

        // 2. Establecer valores iniciales
        int newIndex = _debugDaysItems.Count;
        newItem.title.text = $"Día {newIndex + 1}";
        newItem.quotaField.text = "10"; 
        newItem.timerField.text = "60"; 
        
        // 3. Configurar el botón de eliminar.
        newItem.removeItem.onClick.AddListener(() => RemoveItemByComponent(newItem));

        // 4. Asegurar que el nuevo ítem esté activo y agregarlo a la lista
        newItem.gameObject.SetActive(true);
        _debugDaysItems.Add(newItem);

        // 🌟 NUEVO: Actualiza el estado de los botones después de añadir
        UpdateRemoveButtonStates();
    }

    /// <summary>
    /// Elimina un ítem de la lista por su referencia de componente.
    /// </summary>
    public void RemoveItemByComponent(DebugDaysItem itemToRemove)
    {
        // 1. Eliminar de la lista interna
        _debugDaysItems.Remove(itemToRemove);
        
        // 2. Destruir el GameObject de la UI
        Destroy(itemToRemove.gameObject);

        // 3. Re-enumerar los títulos
        ReIndexItems();
        
        // 🌟 NUEVO: Actualiza el estado de los botones después de eliminar
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
    /// 🌟 FUNCIÓN CLAVE: Habilita o deshabilita los botones de eliminar.
    /// Si solo hay 1 ítem, deshabilita su botón. Si hay más, habilita todos.
    /// </summary>
    private void UpdateRemoveButtonStates()
    {
        // Si hay más de un día, todos los días pueden ser eliminados (true).
        // Si solo hay un día (Count == 1), no pueden ser eliminados (false).
        bool canRemove = _debugDaysItems.Count > 1;

        foreach (var item in _debugDaysItems)
        {
            item.removeItem.interactable = canRemove;
        }
    }
    
// -------------------------------------------------------------------
// LÓGICA DE CONFIRMACIÓN Y GUARDADO
// -------------------------------------------------------------------

    /// <summary>
    /// Actualiza el array de días del GameManager con los valores actuales de los ítems de la UI.
    /// </summary>
    public void ConfirmValues()
    {
        List<GameManager.Level> newDays = new List<GameManager.Level>();

        foreach (var item in _debugDaysItems)
        {
            int quota;
            float duration;

            // Intentar parsear el día Quota.
            if (!int.TryParse(item.quotaField.text, out quota))
            {
                Debug.LogError($"Error al parsear la cuota del día {item.title.text}. Asegúrate de ingresar un número entero.");
                quota = 0; 
            }

            // Intentar parsear la Duración del día.
            if (!float.TryParse(item.timerField.text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out duration))
            {
                // Uso de InvariantCulture para manejar decimales con punto (más común en código)
                Debug.LogError($"Error al parsear la duración del día {item.title.text}. Asegúrate de ingresar un número.");
                duration = 0.0f;
            }

            // Crear el nuevo struct Level y añadirlo a la lista
            GameManager.Level newLevel = new GameManager.Level
            {
                dayQuota = quota,
                dayDuration = duration
            };
            newDays.Add(newLevel);
        }

        // Convertir la lista a un array y asignarla al GameManager
        _gameManager.days = newDays.ToArray();

        int.TryParse(_sackLimitField.text, out _gameManager._playerSackCarrySpaceLimit);
        float.TryParse(_oxygenField.text, out maxTotalOxygen.value);
        
        Debug.Log($"¡Valores de días confirmados y guardados en GameManager! Total de días: {_gameManager.days.Length}");
    }
}