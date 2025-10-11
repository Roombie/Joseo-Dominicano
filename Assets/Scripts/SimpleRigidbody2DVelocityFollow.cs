using UnityEngine;

public class SimpleRigidbody2DVelocityFollow : MonoBehaviour
{
    [SerializeField] float _maxSpeed = 1;
    [SerializeField, Range(0, 1)] float _time = 1;
    [SerializeField, Range(0, 1)] float _threshold = 0.2f;
    [SerializeField] Rigidbody2D _body;
    Vector3 follow;
    void Update()
    {
        Vector3 center = _body.transform.position;
        Vector3 velocity = _time * _body.linearVelocity;
        velocity = Vector3.ClampMagnitude(velocity, _maxSpeed);
        follow = Vector3.Lerp(follow, center + velocity, (velocity.magnitude / Mathf.Max(0.00000001f, _maxSpeed * _threshold)) * Time.deltaTime );
        transform.position = follow;
    }
}
