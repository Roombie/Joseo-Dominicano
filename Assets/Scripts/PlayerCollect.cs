using UnityEngine;
using UnityEngine.Events;

public class PlayerCollect : MonoBehaviour
{ 
    [SerializeField] UnityEvent<TestValuable> _onCollect;
    public UnityEvent<TestValuable> onCollect => _onCollect;
    private void OnTriggerEnter2D(Collider2D other) 
    {
        if (other.TryGetComponent(out TestValuable valuable))
        {
            _onCollect?.Invoke(valuable);
        }
    }
}
