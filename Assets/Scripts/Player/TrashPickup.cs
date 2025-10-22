using UnityEngine;
using System.Collections;

public class TrashPickup : MonoBehaviour
{
    [SerializeField] private TrashItemSO item;
    [SerializeField] private float destroyTime = 10f;
    [SerializeField] private bool selfDestroy = true;

    public TrashItemSO Item => item;

    void Start()
    {
        if (selfDestroy) StartCoroutine(AutoDestroy());
    }

    private IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(destroyTime);
        if (this != null) Destroy(gameObject);
    }
}