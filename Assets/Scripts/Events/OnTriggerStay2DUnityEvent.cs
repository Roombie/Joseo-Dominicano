using UnityEngine;
using UnityEngine.Events;

public class OnTriggerStay2DUnityEvent : MonoBehaviour
{
    [SerializeField] string _tag = "Untagged";
    [SerializeField] Collider2DUnityEvent _onTriggerStay;
    void OnTriggerStay2D(Collider2D other)
    {
        if (_tag == "Untagged") _onTriggerStay?.Invoke(other);
        else if (other.CompareTag(_tag))
        {
            _onTriggerStay?.Invoke(other);
        }
        
    }
}