using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    /// <summary>
    /// Assign to an object you want to give interaction. 
    /// This script will handle recognition and activate interaction with the player.
    /// Should assign specific interaction script to each object
    /// </summary>

    [Header("Interaction Components")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerSmoothMovement playerSmoothMovement;
    [SerializeField] private MonoBehaviour interactionScript;

    [Header("Unity Events")]
    [SerializeField] private UnityEvent onPlayerEnter;
    [SerializeField] private UnityEvent onPlayerExit;
    [SerializeField] private UnityEvent onInteract;

    private IPlayerInteract interact;
    private bool inRange;

    private void Awake() => interact = interactionScript as IPlayerInteract;
    
    private void OnDisable() 
    {
        if(playerMovement != null) 
            playerMovement.onInteractEvent -= Interact;

        if (playerSmoothMovement != null) 
            playerSmoothMovement.onInteractEvent -= Interact;
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            inRange = true;
            
            // Subscribe to interact events
            if (playerMovement != null)
                playerMovement.onInteractEvent += Interact;
            if (playerSmoothMovement != null)
                playerSmoothMovement.onInteractEvent += Interact;
            
            // Invoke enter event
            onPlayerEnter?.Invoke();
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            inRange = false;
            
            // Unsubscribe from interact events
            if (playerMovement != null)
                playerMovement.onInteractEvent -= Interact;
            if (playerSmoothMovement != null)
                playerSmoothMovement.onInteractEvent -= Interact;
            
            // Invoke exit event
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