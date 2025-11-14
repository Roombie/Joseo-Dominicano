using UnityEngine;

public abstract class ShopItemEffect : ScriptableObject
{
    public abstract void ApplyEffect(GameObject player);
}