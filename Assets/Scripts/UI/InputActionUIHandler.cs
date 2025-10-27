using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

public class InputActionUIHandler : MonoBehaviour
{
    public enum DirectionFilter
    {
        None,
        Up,
        Down,
        Left,
        Right,
        PositiveX,
        NegativeX,
        PositiveY,
        NegativeY
    }

    [Serializable]
    public class ActionUIBinding
    {
        [Header("Action")]
        public InputActionReference inputActionReference;

        [Header("Direction Filter (for Vector2/composites)")]
        public DirectionFilter directionFilter = DirectionFilter.None;
        [Range(0f, 1f)]
        public float directionThreshold = 0.5f;
        public bool treatFilteredAsButton = true;

        [Header("UI")]
        public UIElement uiElement;

        [NonSerialized] internal Action<InputAction.CallbackContext> onPerformed;
        [NonSerialized] internal Action<InputAction.CallbackContext> onCanceled;
    }

    [Serializable]
    public class UIElement
    {
        public Image image;
        public Sprite idleSprite;
        public Sprite activeSprite;

        public UnityEvent<Vector2> onVector2Input;
        public UnityEvent<bool> onBooleanInput;
    }

    public List<ActionUIBinding> actionBindings = new List<ActionUIBinding>();

    private void OnEnable()
    {
        foreach (var binding in actionBindings)
        {
            if (binding?.inputActionReference == null) continue;

            var action = binding.inputActionReference.action;
            if (action == null) continue;

            binding.onPerformed = ctx => OnActionValue(binding, ctx);
            binding.onCanceled  = ctx => OnActionValue(binding, ctx);

            action.performed += binding.onPerformed;
            action.canceled  += binding.onCanceled;

            if (!action.enabled) action.Enable();
        }
    }

    private void OnDisable()
    {
        foreach (var binding in actionBindings)
        {
            if (binding?.inputActionReference == null) continue;

            var action = binding.inputActionReference.action;
            if (action == null) continue;

            if (binding.onPerformed != null) action.performed -= binding.onPerformed;
            if (binding.onCanceled  != null) action.canceled -= binding.onCanceled;

            binding.onPerformed = null;
            binding.onCanceled  = null;
        }
    }

    private void OnActionValue(ActionUIBinding binding, InputAction.CallbackContext ctx)
    {
        if (binding == null || binding.uiElement == null) return;

        Vector2 v2 = Vector2.zero;
        bool isVector2 = false;

        try
        {
            v2 = ctx.ReadValue<Vector2>();
            isVector2 = true;
        }
        catch
        {
            float f = 0f;
            try { f = ctx.ReadValue<float>(); } catch { }
            v2 = new Vector2(f, 0f);
        }

        bool hasFilter = binding.directionFilter != DirectionFilter.None;
        bool filteredActive = hasFilter && DirectionIsActive(v2, binding.directionFilter, binding.directionThreshold);

        binding.uiElement.onVector2Input?.Invoke(v2);

        bool boolState = hasFilter && binding.treatFilteredAsButton
            ? filteredActive
            : (isVector2 ? v2 != Vector2.zero : v2.x > 0f);

        binding.uiElement.onBooleanInput?.Invoke(boolState);

        if (binding.uiElement.image != null)
        {
            bool spriteActive = hasFilter && binding.treatFilteredAsButton
                ? filteredActive
                : (isVector2 ? v2 != Vector2.zero : v2.x > 0f);

            binding.uiElement.image.sprite = spriteActive
                ? binding.uiElement.activeSprite
                : binding.uiElement.idleSprite;
        }
    }

    private static bool DirectionIsActive(Vector2 v, DirectionFilter filter, float threshold)
    {
        switch (filter)
        {
            case DirectionFilter.Up:        return v.y >=  threshold;
            case DirectionFilter.Down:      return v.y <= -threshold;
            case DirectionFilter.Left:      return v.x <= -threshold;
            case DirectionFilter.Right:     return v.x >=  threshold;
            case DirectionFilter.PositiveX: return v.x >=  threshold;
            case DirectionFilter.NegativeX: return v.x <= -threshold;
            case DirectionFilter.PositiveY: return v.y >=  threshold;
            case DirectionFilter.NegativeY: return v.y <= -threshold;
            default:                        return v.sqrMagnitude >= threshold * threshold;
        }
    }
}
