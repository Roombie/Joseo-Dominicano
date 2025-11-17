using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using System.Collections;

public class ShopInteraction : MonoBehaviour, IPlayerInteract
{
    [Header("UI")]
    [SerializeField] GameObject shopPanel;

    [Header("Catalog")]
    [SerializeField] ShopItemUI itemUI;
    [SerializeField] ShopItemSO itemSO;

    [Header("Player Control")]
    [SerializeField] Rigidbody2D playerRB;
    [SerializeField] PlayerSmoothMovement mover;

    [Header("Wallet")]
    [SerializeField] PlayerWallet wallet;
    public PlayerWallet Wallet => wallet;

    [Header("Shop Visuals")]
    [SerializeField] LocalizeSpriteEvent shopSpriteLocalizer;
    [SerializeField] LocalizedSprite nearSpriteLocalized;
    [SerializeField] LocalizedSprite farSpriteLocalized;

    [Header("Animator")]
    [SerializeField] Animator shopAnimator;

    [Header("Extra")]
    [SerializeField] bool pauseGameWhenOpen = true;
    [SerializeField] bool closeOnTimerEnd = true; // New option to auto-close when timer ends

    GameManager gm;

    bool isOpen;
    bool playerInRange;
    bool isOpening;
    bool isClosing;
    public bool IsShopOpen => isOpen || isOpening || isClosing;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
        if (shopPanel) shopPanel.SetActive(false);
        UpdateShopSprite();
        
        // Validate critical references
        if (wallet == null)
        {
            Debug.LogError("ShopInteraction: Wallet reference is missing!", this);
        }
        
        if (itemUI == null)
        {
            Debug.LogError("ShopInteraction: ItemUI reference is missing!", this);
        }
        
        if (itemSO == null)
        {
            Debug.LogError("ShopInteraction: ItemSO reference is missing!", this);
        }
    }

    void Update()
    {
        // Auto-close shop when timer ends if enabled
        if (closeOnTimerEnd && isOpen && gm != null && !gm.inShift)
        {
            Debug.Log("ShopInteraction: Auto-closing shop because timer ended");
            Close();
        }
    }

    public void Interact()
    {
        if (isOpening || isClosing)
            return;

        if (!isOpen && playerInRange)
            Open();
        else if (isOpen)
            Close();
    }

    void Open()
    {
        if (isOpening || isClosing)
            return;

        // Don't open if gameplay shift has ended
        if (gm != null && !gm.inShift)
        {
            Debug.Log("ShopInteraction: Cannot open shop - gameplay shift has ended");
            return;
        }

        // Validate we have everything we need
        if (itemUI == null || itemSO == null || wallet == null)
        {
            Debug.LogError("ShopInteraction: Cannot open shop - missing required references");
            return;
        }

        itemUI.Setup(itemSO, this);

        isOpen = true;
        isOpening = true;

        if (playerRB) playerRB.linearVelocity = Vector2.zero;
        if (mover) { mover.ResetMove(); mover.enabled = false; }

        if (pauseGameWhenOpen && gm != null)
        {
            gm.DisallowPause(true);
            gm._Gameplay_SilentPause();
            if (gm.PauseButton != null)
                gm.PauseButton.SetActive(false);
        }

        if (shopPanel) 
        {
            shopPanel.SetActive(true);
            // Clear selection to prevent auto-clicking
            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(null);
        }

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        yield return null;

        if (shopAnimator)
        {
            float length = shopAnimator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSecondsRealtime(length);
        }

        isOpening = false;
    }

    public void Close()
    {
        if (!isOpen || isClosing)
            return;

        isOpen = false;
        isClosing = true;

        if (shopAnimator)
            shopAnimator.SetTrigger("close");

        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        yield return null;

        if (shopAnimator)
        {
            float length = shopAnimator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSecondsRealtime(length);
        }

        if (shopPanel)
            shopPanel.SetActive(false);

        if (mover)
            mover.enabled = true;
        
        if (pauseGameWhenOpen && gm != null)
        {
            gm._Gameplay_SilentResume();
            gm.DisallowPause(false);
            if (gm.PauseButton != null)
                gm.PauseButton.SetActive(true);
        }   

        isClosing = false;
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

        if (isOpen)
            Close();
    }

    void UpdateShopSprite()
    {
        if (shopSpriteLocalizer == null)
            return;

        shopSpriteLocalizer.AssetReference =
            (playerInRange && !isOpen)
            ? nearSpriteLocalized
            : farSpriteLocalized;
    }

    public void TryBuy(ShopItemSO item)
    {
        if (item == null)
        {
            Debug.LogWarning("ShopInteraction: TryBuy called with null item");
            return;
        }

        if (item.isPurchased)
        {
            Debug.Log("ShopInteraction: Item already purchased");
            return;
        }

        if (wallet == null)
        {
            Debug.LogError("ShopInteraction: Cannot buy - wallet reference is null");
            return;
        }

        if (wallet.Balance < item.price)
        {
            Debug.Log($"ShopInteraction: Not enough money. Have: {wallet.Balance}, Need: {item.price}");
            return;
        }

        bool spent = wallet.TrySpend(item.price);

        if (spent)
        {
            if (item.effect != null)
            {
                if (mover != null)
                    item.effect.ApplyEffect(mover.gameObject);
                else
                    Debug.LogWarning("ShopInteraction: Cannot apply effect - mover reference is null");
            }

            // Force carry-space UI update immediately
            GameManager.Instance.RefreshCarrySpaceUI();

            // Update money UI immediately
            GameManager.Instance.UpdateTotalCoinsUI();

            item.isPurchased = true;

            Debug.Log($"ShopInteraction: Successfully purchased {item.name} for ${item.price}");
        }
        else
        {
            Debug.LogError("ShopInteraction: TrySpend failed unexpectedly");
        }
    }


    public void OnClickClose()
    {
        Close();
    }

    // manually refresh the shop if needed
    public void RefreshShop()
    {
        if (itemUI != null && itemSO != null)
        {
            itemUI.Setup(itemSO, this);
        }
    }

    // check if shop can be opened (useful for UI)
    public bool CanOpenShop()
    {
        return playerInRange && !isOpen && !isOpening && !isClosing && 
               (gm == null || gm.inShift); // Only allow opening during active gameplay
    }
}