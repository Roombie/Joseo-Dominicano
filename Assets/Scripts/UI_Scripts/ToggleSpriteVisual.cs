using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Toggle))]
public class ToggleSpriteVisual : MonoBehaviour, IToggleVisual,
IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image targetImage; // if null, uses toggle.targetGraphic as Image

    [Header("Normal Sprites")]
    [SerializeField] private Sprite onNormal;
    [SerializeField] private Sprite offNormal;

    [Header("Pressed Sprites")]
    [SerializeField] private Sprite onPressed;
    [SerializeField] private Sprite offPressed;

    [Tooltip("If true, pressed shows the target state's pressed; false = current state's pressed.")]
    [SerializeField] private bool previewTargetOnPress = false;

    [Tooltip("Flip mapping quickly if your sprites were accidentally assigned swapped.")]
    [SerializeField] private bool invertMapping = false;

    private void Reset()
    {
        toggle = GetComponent<Toggle>();
        if (!targetImage && toggle && toggle.targetGraphic is Image img) targetImage = img;
    }

    private void Awake()
    {
        if (!toggle) toggle = GetComponent<Toggle>();
        if (!targetImage && toggle && toggle.targetGraphic is Image img) targetImage = img;

        if (toggle && toggle.transition != Selectable.Transition.SpriteSwap)
            Debug.LogWarning($"[{nameof(ToggleSpriteVisual)}] Toggle.Transition should be SpriteSwap for pressed visuals.");

        // refresh everything after the value flips (pointer up)
        if (toggle) toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnDestroy()
    {
        if (toggle) toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    private void OnEnable()  => RefreshNow();
#if UNITY_EDITOR
    private void OnValidate() => RefreshNow();
#endif

    private void OnToggleValueChanged(bool _)
    {
        RefreshNow();
    }

    // IToggleVisual methods
    public void SetOn(bool isOn) { RefreshNow(); }
    public void SetPressed(bool pressed) { /* SpriteSwap handles visual; nothing to do. Just put so the IToggleVisual doesn't throw erros here */ }
    public void RefreshNow()
    {
        if (!toggle) return;
        ApplyNormalSprite();
        UpdatePressedSpriteRoute();
    }

    // ensure first pressed frame uses correct pressed sprite
    public void OnPointerDown(PointerEventData e) { UpdatePressedSpriteRoute(); }
    public void OnPointerUp  (PointerEventData e) { RefreshNow(); }
    public void OnPointerExit(PointerEventData e) { ApplyNormalSprite(); }

    private void ApplyNormalSprite()
    {
        if (!targetImage || !toggle) return;
        targetImage.sprite = toggle.isOn ? onNormal : offNormal;
    }

    private void UpdatePressedSpriteRoute()
    {
        if (!toggle) return;

        // Which pressed should be shown?
        bool useOnPressed = previewTargetOnPress ? !toggle.isOn : toggle.isOn;
        if (invertMapping) useOnPressed = !useOnPressed;

        var ss = toggle.spriteState; // struct → must reassign
        ss.pressedSprite = useOnPressed ? onPressed : offPressed;
        toggle.spriteState = ss;
    }
}