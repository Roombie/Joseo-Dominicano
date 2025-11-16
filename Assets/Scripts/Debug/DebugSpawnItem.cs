using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Este componente representa la configuración de un SpawnData dentro de un día específico para propósitos de depuración.
public class DebugSpawnItem : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Muestra el nombre del prefab a configurar.")]
    public TMP_Text title; 
    
    [Header("Weight Control")]
    [Tooltip("Slider para modificar el 'weight' del objeto.")]
    public Slider weightSlider;
    [Tooltip("Campo de texto para introducir el 'weight' del objeto manualmente.")]
    public TMP_InputField weightInputField;
    
    [Tooltip("Toggle para el booleano 'canSpawn'.")]
    public Toggle canSpawnToggle;
    
    [Tooltip("Referencia al GameObject del botón de remover (opcional, se usa para mostrar/ocultar).")]
    public GameObject removeItemUI; 

    // Referencia al objeto de datos original (SpawnData)
    [HideInInspector] public SpawnData dataReference; 

    // Constantes para el rango del slider
    private const float MIN_WEIGHT = 0f;
    private const float MAX_WEIGHT = 100f;

    /// <summary>
    /// Inicializa el ítem de UI con la referencia de SpawnData.
    /// </summary>
    public void Initialize(SpawnData data)
    {
        dataReference = data;
        
        // Usamos el nombre del prefab como título
        title.text = data.prefab != null ? data.prefab.name : "MISSING PREFAB";

        // --- Configuración del Slider ---
        weightSlider.minValue = MIN_WEIGHT;
        weightSlider.maxValue = MAX_WEIGHT; 
        weightSlider.wholeNumbers = true; 
        
        // --- Configuración Inicial de Valores ---
        // Establecer el valor inicial en ambos controles
        SetWeightValue(data.weight);

        // --- Configuración de Listeners ---
        
        // Listener del Slider
        weightSlider.onValueChanged.RemoveAllListeners();
        weightSlider.onValueChanged.AddListener(OnSliderWeightChanged);
        
        // Listener del InputField (al finalizar la edición)
        weightInputField.onEndEdit.RemoveAllListeners();
        weightInputField.onEndEdit.AddListener(OnInputWeightChanged);
        
        // Listener del Toggle
        canSpawnToggle.isOn = data.canSpawn;
        
        // Sincronizar apariencia inicial con el Toggle
        UpdateVisuals(data.canSpawn);

        canSpawnToggle.onValueChanged.RemoveAllListeners();
        canSpawnToggle.onValueChanged.AddListener(OnCanSpawnChanged);

        // Deshabilitar cualquier UI de remoción.
        // El estado de removeItemUI se gestiona en UpdateVisuals.
    }

    /// <summary>
    /// Establece el valor de peso y actualiza tanto el Slider como el InputField.
    /// </summary>
    private void SetWeightValue(int weight)
    {
        // 1. Aplicar el valor al Slider
        weightSlider.value = weight;
        
        // 2. Aplicar el valor al InputField
        weightInputField.text = weight.ToString();
        
        // 3. Actualiza la referencia de datos
        dataReference.weight = weight;
    }

    /// <summary>
    /// Se llama cuando el valor del Slider de peso cambia (arrastrando o haciendo clic).
    /// </summary>
    private void OnSliderWeightChanged(float newValue)
    {
        int weight = Mathf.RoundToInt(newValue);
        
        // Actualiza solo el InputField (y, por lo tanto, el SpawnData) si el valor es diferente
        // para evitar un bucle de listeners, aunque en este caso SetWeightValue
        // es seguro ya que el listener del Slider se disparó primero.
        if (dataReference.weight != weight)
        {
            // Usamos el InputField para mostrar el valor del Slider en tiempo real.
            // Nota: Aquí solo actualizamos el texto del InputField para que se refleje 
            // en la UI inmediatamente. El InputField no actualiza el SpawnData hasta que termina la edición.
            weightInputField.text = weight.ToString();
            dataReference.weight = weight; // Actualiza el dato inmediatamente al arrastrar
        }
    }

    /// <summary>
    /// Se llama cuando se termina de editar el InputField (presionar Enter o perder el foco).
    /// </summary>
    private void OnInputWeightChanged(string newValue)
    {
        // Intenta parsear el valor a un entero.
        if (int.TryParse(newValue, out int newWeight))
        {
            // Clampea el valor al rango del Slider para mantener la coherencia.
            newWeight = Mathf.Clamp(newWeight, (int)MIN_WEIGHT, (int)MAX_WEIGHT);
            
            // Si el valor es válido, actualiza el valor.
            SetWeightValue(newWeight);
        }
        else
        {
            // Si el valor no es válido, revierte el texto al valor actual conocido.
            weightInputField.text = dataReference.weight.ToString();
        }
    }
    
    /// <summary>
    /// Se llama cuando el valor del Toggle de aparición cambia.
    /// </summary>
    private void OnCanSpawnChanged(bool newValue)
    {
        // Actualiza la referencia de datos inmediatamente
        dataReference.canSpawn = newValue;
        UpdateVisuals(newValue);
    }

    /// <summary>
    /// Actualiza la opacidad del título y el estado del botón de remover.
    /// </summary>
    private void UpdateVisuals(bool canSpawn)
    {
        // Cambia la opacidad del título para indicar si está activo o no
        title.alpha = canSpawn ? 1f : 0.5f;

        // Muestra/Oculta la UI de remover.
        if(removeItemUI != null)
        {
            removeItemUI.SetActive(canSpawn);
        }
    }
}