using UnityEngine;

[CreateAssetMenu(menuName = "Game/Shop Effects/Increase Carry Capacity")]
public class IncreaseCarryCapacityEffect : ShopItemEffect
{
    public int extraCapacity = 10;

    public override void ApplyEffect(GameObject player)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.IncreasePlayerCarryCapacity(extraCapacity);
        }
    }
}