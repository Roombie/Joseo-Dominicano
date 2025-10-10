using UnityEngine;

public class TestValuable : MonoBehaviour
{
    [SerializeField] GameManager.TestValuableData _properties;
    public GameManager.TestValuableData valuable => _properties;
}
