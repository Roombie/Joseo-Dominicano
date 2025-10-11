using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInteract : MonoBehaviour
{
    /// <summary>
    /// Assign to an object you want to give interaction. 
    /// This script will handle recognition and activate interaction with the player.
    /// Should assign specific interaction script to each object
    /// </summary>
    /// 

    /// <summary>
    /// Instructions: Tag player with "Player" tag.
    /// Assign collider to the interaction object, set as trigger.
    /// Assign this script to the interaction object.
    /// Assign interactionScript to the specific interaction script that defines the interaction.
    /// Add an interaction sign (Sprite) if desired and assign to this script
    /// Player must have collider too
    /// Add a player input component to the object with this script, additional to the player's (even if it uses the same input asset)
    /// On action map of the player input remove the Hold interaction from Interact action, or reduce the hold time to achieve desired effects 
    /// </summary>

    [SerializeField] private PlayerMovement player; //To Get the Script that defines the specific interaction of the interaction-object
    [SerializeField] private MonoBehaviour interactionScript; //To Get the Script that defines the specific interaction of the interaction-object
    [SerializeField] private SpriteRenderer interactSign;
    IPlayerInteract interact;

    private bool inRange;
    //-----------------------------------------------------------
    private void Awake() => interact = interactionScript as IPlayerInteract;
    private void OnDisable() => player.onInteractEvent -= Interact;
    private void Start() => interactSign.enabled = false;        
    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.gameObject.CompareTag("Player"))
        {
            inRange = true;
            InteractSign(true); //Activates object interaction sign
            player.onInteractEvent += Interact;
            Debug.Log("inrange " + inRange);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {

        if (collision.gameObject.CompareTag("Player"))
        {
            inRange = false;
            InteractSign(false); //Dectivates player interaction sign
            player.onInteractEvent -= Interact;
            //Debug.Log("inrange " + inRange);
        }
    }
    public void Interact()
    {
        Debug.Log("Interact called)");
        if (inRange) interact.Interact();
        
    }
    public void InteractSign(bool state)
    {
        interactSign.enabled = state;
    }
}
