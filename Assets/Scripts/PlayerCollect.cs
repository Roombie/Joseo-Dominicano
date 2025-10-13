using UnityEngine;
using UnityEngine.Events;

public class PlayerCollect : MonoBehaviour
{ 
    [SerializeField] UnityEvent<TestValuable> _onCollect;
    public UnityEvent<TestValuable> onCollect => _onCollect;
    private void OnCollisionEnter2D(Collision2D other) 
    {
        if (other.collider.TryGetComponent(out TestValuable valuable))
        {
            _onCollect?.Invoke(valuable);   
        }
    }
}
