using UnityEngine;
using UnityEngine.Events;

public class MenuView : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    [Header("Behavior")]
    [SerializeField] private bool deactivateOnClose = true;

    [Header("Events")]
    public UnityEvent onOpenAnimationFinished;
    public UnityEvent onCloseAnimationFinished;

    int _openTriggerHash;
    int _closeTriggerHash;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        _openTriggerHash = Animator.StringToHash(openTriggerName);
        _closeTriggerHash = Animator.StringToHash(closeTriggerName);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (animator != null)
        {
            animator.ResetTrigger(_closeTriggerHash);
            animator.SetTrigger(_openTriggerHash);
        }
    }

    public void Hide()
    {
        if (animator != null)
        {
            animator.ResetTrigger(_openTriggerHash);
            animator.SetTrigger(_closeTriggerHash);
        }
        else
        {
            onCloseAnimationFinished?.Invoke();
        }
    }

    // Llamar desde Animation Event al final de la animación de abrir
    public void OnOpenAnimationFinished()
    {
        onOpenAnimationFinished?.Invoke();
    }

    // Llamar desde Animation Event al final de la animación de cerrar
    public void OnCloseAnimationFinished()
    {
        onCloseAnimationFinished?.Invoke();
    }
}