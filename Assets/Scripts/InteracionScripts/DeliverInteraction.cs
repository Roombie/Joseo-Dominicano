using UnityEngine;
using UnityEngine.Events;

public class DeliverInteraction : MonoBehaviour, IPlayerInteract
{
    public UnityEvent onDepositValuables;
    public void Interact() => onDepositValuables.Invoke();
}
