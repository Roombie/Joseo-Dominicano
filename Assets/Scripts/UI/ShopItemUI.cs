using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;
using System;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TMP_Text priceLabel;
    [SerializeField] Button buyButton;

    [Header("Localization")]
    [SerializeField] LocalizedString purchasedLocalizedString;

    [Header("Audio Feedback")]
    [SerializeField] AudioClip purchaseSuccessSound;
    [SerializeField] AudioClip purchaseFailSound;

    // Events for external systems
    public event Action<ShopItemSO> OnPurchaseAttempt;
    public event Action<ShopItemSO> OnPurchaseSuccess;
    public event Action<ShopItemSO> OnPurchaseFailed;

    ShopItemSO item;
    ShopInteraction shop;
    bool isInitialized = false;

    void Awake()
    {
        // Validate critical components
        if (priceLabel == null)
            Debug.LogError("ShopItemUI: PriceLabel reference is missing!", this);
        
        if (buyButton == null)
            Debug.LogError("ShopItemUI: BuyButton reference is missing!", this);
        
        // Initialize button state
        if (buyButton != null)
            buyButton.interactable = false;
    }

    void OnDestroy() 
    {
        Cleanup();
    }

    void OnEnable()
    {
        // Subscribe to localization changes if needed
        if (purchasedLocalizedString != null)
        {
            purchasedLocalizedString.StringChanged += OnPurchasedStringChanged;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from localization
        if (purchasedLocalizedString != null)
        {
            purchasedLocalizedString.StringChanged -= OnPurchasedStringChanged;
        }
    }

    public void Setup(ShopItemSO item, ShopInteraction shop)
    {
        if (item == null)
        {
            Debug.LogError("ShopItemUI: Cannot setup with null item!", this);
            return;
        }

        if (shop == null)
        {
            Debug.LogError("ShopItemUI: Cannot setup with null shop!", this);
            return;
        }

        // Cleanup previous setup
        Cleanup();

        this.item = item;
        this.shop = shop;
        this.isInitialized = true;

        // Setup button listener
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuy);
        }

        RefreshUI();
    }

    public void Cleanup()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnBuy);
        }
        
        item = null;
        shop = null;
        isInitialized = false;
    }

    public void RefreshUI()
    {
        if (!isInitialized || item == null)
        {
            Debug.LogWarning("ShopItemUI: Cannot refresh - not properly initialized");
            return;
        }

        if (item.isPurchased)
        {
            SetPurchasedState();
        }
        else
        {
            SetAvailableState();
        }
    }

    private void SetPurchasedState()
    {
        if (buyButton != null)
        {
            buyButton.interactable = false;
        }

        if (priceLabel != null)
        {
            if (purchasedLocalizedString != null && !purchasedLocalizedString.IsEmpty)
            {
                priceLabel.text = purchasedLocalizedString.GetLocalizedString();
            }
            else
            {
                priceLabel.text = "PURCHASED";
            }
        }
    }

    private void SetAvailableState()
    {
        if (buyButton != null)
        {
            buyButton.interactable = true;
        }

        if (priceLabel != null && item != null)
        {
            priceLabel.text = $"${item.price}";
        }
    }

    private void OnPurchasedStringChanged(string localizedText)
    {
        // Refresh UI when localization changes
        if (isInitialized && item != null && item.isPurchased)
        {
            if (priceLabel != null)
            {
                priceLabel.text = localizedText;
            }
        }
    }

    private void OnBuy()
    {
        Debug.Log("ShopItemUI: Buy button clicked for item: " + (item != null ? item.name : "null"));

        if (!isInitialized)
        {
            Debug.LogWarning("ShopItemUI: Not initialized, cannot purchase");
            return;
        }

        if (item == null)
        {
            Debug.LogError("ShopItemUI: Item reference is null!");
            return;
        }

        if (shop == null)
        {
            Debug.LogError("ShopItemUI: Shop reference is null!");
            return;
        }

        // Notify listeners about purchase attempt
        OnPurchaseAttempt?.Invoke(item);

        if (item.isPurchased)
        {
            Debug.Log("ShopItemUI: Item already purchased");
            PlayPurchaseSound(purchaseFailSound);
            OnPurchaseFailed?.Invoke(item);
            return;
        }

        int oldBalance = shop.Wallet != null ? shop.Wallet.Balance : 0;

        // Try to purchase through shop system
        shop.TryBuy(item);

        // Check if purchase was successful
        if (item.isPurchased && shop.Wallet != null && shop.Wallet.Balance != oldBalance)
        {
            Debug.Log("ShopItemUI: Purchase successful!");
            PlayPurchaseSound(purchaseSuccessSound);
            RefreshUI();
            OnPurchaseSuccess?.Invoke(item);
        }
        else
        {
            Debug.Log("ShopItemUI: Purchase failed - not enough money or other issue");
            PlayPurchaseSound(purchaseFailSound);
            OnPurchaseFailed?.Invoke(item);
        }
    }

    private void PlayPurchaseSound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioManager.Instance.Play(clip, SoundCategory.SFX);
        }
    }

    // Public method to force UI update (useful for runtime changes)
    public void ForceUpdate()
    {
        RefreshUI();
    }

    // Helper method to check if this UI is currently displaying a valid item
    public bool IsValid()
    {
        return isInitialized && item != null && shop != null;
    }
}