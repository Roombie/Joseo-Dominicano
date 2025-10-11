using UnityEngine;
using UnityEngine.Events;

public class DeliverInteraction : MonoBehaviour, IPlayerInteract
{
    public UnityEvent onDepositValuables;
    public void Interact() { Debug.Log("DeliverInteraction worked"); onDepositValuables.Invoke(); }
}
