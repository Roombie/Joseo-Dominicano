using System.Collections;
using UnityEngine;

public class TestValuable : MonoBehaviour
{
    [SerializeField] GameManager.TestValuableData _properties;
    [SerializeField] float destroyTime = 10;
    [SerializeField] bool selfDestroy = true;
    private void Start()
    {
        if (selfDestroy) 
            StartCoroutine(DestroyOnSeconds());
    }

    IEnumerator DestroyOnSeconds()
    {
        yield return new WaitForSeconds(destroyTime);
        Destroy(gameObject);    
    }
    public GameManager.TestValuableData valuable => _properties;
}
