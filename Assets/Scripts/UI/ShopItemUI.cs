using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;
using System;
using System.Collections;
using UnityEngine.EventSystems;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TMP_Text priceLabel;
    [SerializeField] Button buyButton;

    [Header("Localization")]
    [SerializeField] LocalizedString purchasedLocalizedString;

    [Header("Price Colors")]
    [SerializeField] private Color affordableNormal = Color.white;
    [SerializeField] private Color affordablePressed = Color.white;

    [SerializeField] private Color notAffordableNormal = Color.red;
    [SerializeField] private Color notAffordablePressed = new Color(0.8f, 0f, 0f);

    [Tooltip("Used when the item is MAX (button not interactable).")]
    [SerializeField] private Color disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Audio Feedback")]
    [SerializeField] AudioClip purchaseSuccessSound;
    [SerializeField] AudioClip purchaseFailSound;

    [Header("Input")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private float submitPressedVisualSeconds = 0.08f;

    // External events
    public event Action<ShopItemSO> OnPurchaseAttempt;
    public event Action<ShopItemSO> OnPurchaseSuccess;
    public event Action<ShopItemSO> OnPurchaseFailed;

    private ShopItemSO item;
    private ShopInteraction shop;
    private bool isInitialized = false;

    private bool canAfford;
    private bool isPointerDown;

    private Coroutine submitPressCoroutine;

    // Layout/TMP stability: avoid forcing text/layout updates every frame.
    private int _lastPrice = int.MinValue;
    private int _lastBalance = int.MinValue;
    private bool _lastCanAfford;
    private bool _lastTreatAsPressed;
    private bool _lastDisabled;
    private bool _hasCachedVisual;

    private void Awake()
    {
        if (priceLabel == null)
            Debug.LogError("ShopItemUI: PriceLabel reference is missing!", this);

        if (buyButton == null)
            Debug.LogError("ShopItemUI: BuyButton reference is missing!", this);

        if (buyButton != null)
            buyButton.interactable = false; // enabled on Setup / RefreshUI
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnEnable()
    {
        if (purchasedLocalizedString != null)
            purchasedLocalizedString.StringChanged += OnPurchasedStringChanged;

        if (inputReader != null)
            inputReader.UISubmitEvent += OnUISubmit;
    }

    private void OnDisable()
    {
        if (purchasedLocalizedString != null)
            purchasedLocalizedString.StringChanged -= OnPurchasedStringChanged;

        if (inputReader != null)
            inputReader.UISubmitEvent -= OnUISubmit;
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

        Cleanup();

        this.item = item;
        this.shop = shop;
        isInitialized = true;

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuy);

        RefreshUI();
    }

    public void Cleanup()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuy);

        item = null;
        shop = null;
        isInitialized = false;
        isPointerDown = false;

        _hasCachedVisual = false;
        _lastPrice = int.MinValue;
        _lastBalance = int.MinValue;
        _lastCanAfford = false;
        _lastTreatAsPressed = false;
        _lastDisabled = false;
    }

    public Button GetBuyButton() => buyButton;

    // Hook this from EventTrigger, then submit on the button
    public void OnSubmitEventTrigger(BaseEventData _)
    {
        if (!isInitialized || buyButton == null || !buyButton.interactable)
            return;

        if (submitPressCoroutine != null)
            StopCoroutine(submitPressCoroutine);

        submitPressCoroutine = StartCoroutine(SubmitPressVisualAndBuy());
    }

    // Optional path (if InputReader is wired for UI submit)
    private void OnUISubmit()
    {
        if (!isInitialized || item == null || shop == null)
            return;

        if (buyButton == null || !buyButton.interactable)
            return;

        if (EventSystem.current == null)
            return;

        var current = EventSystem.current.currentSelectedGameObject;
        if (current == null)
            return;

        bool isOnThisButton =
            current == buyButton.gameObject ||
            current.transform.IsChildOf(buyButton.transform);

        if (!isOnThisButton)
            return;

        if (submitPressCoroutine != null)
            StopCoroutine(submitPressCoroutine);

        submitPressCoroutine = StartCoroutine(SubmitPressVisualAndBuy());
    }

    private IEnumerator SubmitPressVisualAndBuy()
    {
        SetPressedState(true);
        UpdatePriceState(force: true);

        float t = 0f;
        while (t < submitPressedVisualSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        buyButton.onClick.Invoke();

        SetPressedState(false);
        UpdatePriceState(force: true);
        submitPressCoroutine = null;
    }

    public void SetPressedState(bool pressed)
    {
        isPointerDown = pressed;

        if (!isInitialized || item == null || shop == null)
            return;

        UpdatePriceState();
    }

    // Called from EventTrigger (Pointer Down)
    public void OnPriceButtonPressed() => SetPressedState(true);

    // Called from EventTrigger (Pointer Up)
    public void OnPriceButtonReleased() => SetPressedState(false);

    private void SetPurchasedState()
    {
        if (buyButton != null)
            buyButton.interactable = false;

        if (priceLabel != null)
        {
            if (purchasedLocalizedString != null && !purchasedLocalizedString.IsEmpty)
                priceLabel.text = purchasedLocalizedString.GetLocalizedString();
            else
                priceLabel.text = "MAX";
        }

        UpdatePriceState(force: true);
    }

    private void SetAvailableState()
    {
        if (buyButton != null)
            buyButton.interactable = true;

        UpdatePriceState(force: true);
    }

    public void RefreshUI()
    {
        if (!isInitialized || item == null)
        {
            Debug.LogWarning("ShopItemUI: Cannot refresh - not properly initialized");
            return;
        }

        if (!item.CanPurchase)
            SetPurchasedState();
        else
            SetAvailableState();
    }

    private void UpdatePriceState(bool force = false)
    {
        if (shop == null || item == null || priceLabel == null)
            return;

        int price = item.CurrentPrice;
        int balance = shop.Wallet != null ? shop.Wallet.Balance : 0;

        bool afford = balance >= price;
        bool disabled = (buyButton != null && !buyButton.interactable);

        // Pressed only when actually pressed, and only if not disabled.
        bool treatAsPressed = (!disabled) && isPointerDown;

        if (!force && _hasCachedVisual &&
            price == _lastPrice &&
            balance == _lastBalance &&
            afford == _lastCanAfford &&
            treatAsPressed == _lastTreatAsPressed &&
            disabled == _lastDisabled)
        {
            return;
        }

        _hasCachedVisual = true;
        _lastPrice = price;
        _lastBalance = balance;
        _lastCanAfford = afford;
        _lastTreatAsPressed = treatAsPressed;
        _lastDisabled = disabled;

        canAfford = afford;

        if (item.CanPurchase)
            priceLabel.text = $"${price}";

        ApplyPriceColor(afford, treatAsPressed, disabled);

        priceLabel.ForceMeshUpdate();
    }

    private void ApplyPriceColor(bool afford, bool pressed, bool disabled)
    {
        if (priceLabel == null) return;

        if (disabled)
        {
            priceLabel.color = disabledColor;
            return;
        }

        Color chosenColor;
        if (!afford)
            chosenColor = pressed ? notAffordablePressed : notAffordableNormal;
        else
            chosenColor = pressed ? affordablePressed : affordableNormal;

        priceLabel.color = chosenColor;
    }

    private void OnPurchasedStringChanged(string localizedText)
    {
        if (isInitialized && item != null && !item.CanPurchase && priceLabel != null)
        {
            priceLabel.text = localizedText;
            UpdatePriceState(force: true);
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

        OnPurchaseAttempt?.Invoke(item);

        if (!item.CanPurchase)
        {
            Debug.Log("ShopItemUI: Item cannot be purchased (already purchased or at max level)");
            PlayPurchaseSound(purchaseFailSound);
            OnPurchaseFailed?.Invoke(item);
            return;
        }

        int oldBalance = shop.Wallet != null ? shop.Wallet.Balance : 0;

        shop.TryBuy(item);

        if (shop.Wallet != null && shop.Wallet.Balance != oldBalance)
        {
            Debug.Log("ShopItemUI: Purchase/upgrade successful!");
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
            AudioManager.Instance.Play(clip, SoundCategory.SFX);
    }

    public void ForceUpdate() => RefreshUI();

    public bool IsValid() => isInitialized && item != null && shop != null;

    private void LateUpdate()
    {
        if (!isInitialized || item == null || shop == null || priceLabel == null)
            return;

        UpdatePriceState();
    }
}