using System.Collections;
using UnityEngine;


public class Hazard : MonoBehaviour
{
    [SerializeField] public float damage;
    [SerializeField] public float coolDownPeriod;
    [SerializeField] private float destroyTime = 10f;
    [SerializeField] private bool selfDestroy = true;

    float normalDamage;
    bool isCooling;

    private void Start()
    {
        normalDamage = damage;
        if (selfDestroy) StartCoroutine(AutoDestroy());
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isCooling)
        {
            damage = 0;
            StartCoroutine(Cooldown());
        }
    }

    IEnumerator Cooldown()
    {
        isCooling = true;
        yield return new WaitForSeconds(coolDownPeriod);
        damage = normalDamage;
        isCooling = false;
    }

    private IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(destroyTime);
        if (this != null) Destroy(gameObject);
    }

}

