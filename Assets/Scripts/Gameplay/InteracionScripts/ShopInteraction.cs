using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class ShopInteraction : MonoBehaviour, IPlayerInteract
{
    [Header("UI")]
    [SerializeField] GameObject shopPanel;
    [SerializeField] GameObject firstSelected;
    [SerializeField] TMP_Text moneyLabel;

    [Header("Player Control")]
    [SerializeField] Rigidbody2D playerRB;
    [SerializeField] PlayerSmoothMovement mover;

    [Header("Wallet")]
    [SerializeField] PlayerWallet wallet;

    [Header("Shop Visuals")]
    [SerializeField] LocalizeSpriteEvent shopSpriteLocalizer;
    [SerializeField] LocalizedSprite nearSpriteLocalized;
    [SerializeField] LocalizedSprite farSpriteLocalized;

    GameManager gm;
    bool isOpen;
    bool playerInRange;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
        if (shopPanel) shopPanel.SetActive(false);
        UpdateShopSprite();
    }

    public void Interact()
    {
        if (!isOpen && playerInRange) Open(); 
        else if (isOpen) Close();
    }

    void Open()
    {
        isOpen = true;
        if (playerRB) playerRB.linearVelocity = Vector2.zero;
        if (mover) { mover.ResetMove(); mover.enabled = false; }
        gm?._Gameplay_Pause();

        if (shopPanel) shopPanel.SetActive(true);
        if (wallet && moneyLabel) moneyLabel.text = "$" + wallet.Balance;

        if (firstSelected && EventSystem.current)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void Close()
    {
        isOpen = false;
        if (shopPanel) shopPanel.SetActive(false);
        if (mover) mover.enabled = true;
        gm?._Gameplay_Resume();
    }

    public void OnPlayerEnter()
    {
        playerInRange = true;
        UpdateShopSprite();
    }

    public void OnPlayerExit()
    {
        playerInRange = false;
        UpdateShopSprite();
        if (isOpen) Close();
    }

    void UpdateShopSprite()
    {
        if (shopSpriteLocalizer == null) return;

        // This automatically handles language changes and sprite loading
        shopSpriteLocalizer.AssetReference = (playerInRange && !isOpen) 
            ? nearSpriteLocalized 
            : farSpriteLocalized;
        
        // No need to call RefreshSprite() - it happens automatically when AssetReference changes
    }

    public void Buy(TrashItemSO item)
    {
        if (!wallet || !item) return;
        if (wallet.Balance >= item.Worth)
        {
            wallet.TrySpend(item.Worth);
            if (moneyLabel) moneyLabel.text = "$" + wallet.Balance;
        }
        else
        {
            // TODO: feedback sin saldo
        }
    }

    public void OnClickClose() => Close();
}