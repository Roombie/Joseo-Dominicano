using UnityEngine;
using UnityEngine.Events;

public class PlayerCollect : MonoBehaviour
{ 
    public UnityEvent<TrashPickup> onCollect;
    private void OnCollisionEnter2D(Collision2D other) 
    {
        if (other.gameObject.TryGetComponent(out TrashPickup pickup))
        {
            onCollect?.Invoke(pickup);
        }
    }
}
