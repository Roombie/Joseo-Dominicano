using System.Collections;
using UnityEngine;

public class TestValuable : MonoBehaviour
{
    [SerializeField] GameManager.TestValuableData _properties;
    [SerializeField] float destroyTime = 10;
    private void Start() => StartCoroutine(DestroyOnSeconds());

    IEnumerator DestroyOnSeconds()
    {
        yield return new WaitForSeconds(destroyTime);
        Destroy(gameObject);    
    }
    public GameManager.TestValuableData valuable => _properties;
}
