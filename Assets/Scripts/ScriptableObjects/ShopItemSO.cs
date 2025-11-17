using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "Game/Shop Item")]
public class ShopItemSO : ScriptableObject
{
    public int price;
    public ShopItemEffect effect;
    public bool isPurchased;
}