using UnityEngine;

using UnityEngine.Events;

public class PlayerHazardListener : MonoBehaviour
{
    [SerializeField] UnityEvent<Hazard> _onHazardCollided;
    public UnityEvent<Hazard> OnHazardCollided => _onHazardCollided;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            Debug.Log("Hazard collided with Player");
            if (collision.gameObject.TryGetComponent(out Hazard hazard))
            {
                _onHazardCollided?.Invoke(hazard);
            }
        }
    }

}

