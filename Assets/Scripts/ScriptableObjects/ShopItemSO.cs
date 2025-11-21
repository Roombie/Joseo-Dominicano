using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "Game/Shop Item")]
public class ShopItemSO : ScriptableObject
{
    [Header("Single-level")]
    public int price;
    public ShopItemEffect effect;
    public bool isPurchased;

    [Header("Multi-level config")]
    public bool useLevels = false;

    [System.Serializable]
    public class ShopItemLevel
    {
        public int price;
        public ShopItemEffect effect;
    }

    public ShopItemLevel[] levels;
    public int currentLevel = 0; // 0 = not purchased, it buys levels[0] first

    public bool IsMaxLevel
    {
        get
        {
            return useLevels && levels != null && levels.Length > 0 &&
                   currentLevel >= levels.Length;
        }
    }

    public bool CanPurchase
    {
        get
        {
            if (!useLevels)
            {
                return !isPurchased;
            }

            return !IsMaxLevel;
        }
    }

    public int CurrentPrice
    {
        get
        {
            if (!useLevels)
                return price;

            if (IsMaxLevel || levels == null || levels.Length == 0)
                return 0;

            return levels[currentLevel].price;
        }
    }

    public ShopItemEffect CurrentEffect
    {
        get
        {
            if (!useLevels)
                return effect;

            if (IsMaxLevel || levels == null || levels.Length == 0)
                return null;

            return levels[currentLevel].effect;
        }
    }

    /// <summary>
    /// Apply the effect of the current level and advance to the next
    /// Returns true if something was applied
    /// </summary>
    public bool ApplyPurchase(GameObject player)
    {
        if (!CanPurchase)
            return false;

        if (!useLevels)
        {
            effect?.ApplyEffect(player);
            isPurchased = true;
            return true;
        }

        // Multi-nivel
        var levelData = levels[currentLevel];
        levelData.effect?.ApplyEffect(player);
        currentLevel++;

        // if we reached the max level after this purchase
        if (IsMaxLevel)
        {
            isPurchased = true;
        }

        return true;
    }
}