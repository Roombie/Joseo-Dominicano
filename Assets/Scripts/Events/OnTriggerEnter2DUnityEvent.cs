using UnityEngine;
using UnityEngine.Events;

public class OnTriggerEnter2DUnityEvent : MonoBehaviour
{
    [SerializeField] string _tag = "Untagged";
    [SerializeField] Collider2DUnityEvent _onTriggerEnter;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (_tag == "Untagged") _onTriggerEnter?.Invoke(other);
        else if (other.CompareTag(_tag))
        {
            _onTriggerEnter?.Invoke(other);
        }
        
    }
}

[System.Serializable]
public class Collider2DUnityEvent : UnityEvent<Collider2D> {}