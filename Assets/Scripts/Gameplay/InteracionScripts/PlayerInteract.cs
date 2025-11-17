using System;
using UnityEngine;
using UnityEngine.Events;

public class PlayerInteract : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private MonoBehaviour interactionScript;

    [Header("Unity Events")]
    [SerializeField] private UnityEvent onPlayerEnter;
    [SerializeField] private UnityEvent onPlayerExit;
    [SerializeField] private UnityEvent onInteract;

    private IPlayerInteract interact;
    private bool inRange;

    private void Awake()
    {
        interact = interactionScript as IPlayerInteract;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            inRange = true;

            inputReader.InteractEvent += Interact;

            onPlayerEnter?.Invoke();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            inRange = false;
            
            inputReader.InteractEvent -= Interact;

            onPlayerExit?.Invoke();
        }
    }

    public void Interact()
    {
        if (inRange)
        {
            interact?.Interact();
            onInteract?.Invoke();
        }
    }
}