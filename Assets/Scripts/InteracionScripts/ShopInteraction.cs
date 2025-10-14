using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

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

    GameManager gm;
    bool isOpen;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
        if (shopPanel) shopPanel.SetActive(false);
    }

    public void Interact()
    {
        if (!isOpen) Open(); else Close();
    }

    void Open()
    {
        isOpen = true;

        // Congelar al jugador
        if (playerRB) playerRB.linearVelocity = Vector2.zero;
        if (mover) { mover.ResetMove(); mover.enabled = false; } // cleans input and deactivate movement
        gm?._Gameplay_Pause(); // modal pause, just the timer and oxigen pausa modal

        // UI
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
        gm?._Gameplay_Resume(); // reanuda modal
    }

    // Ejemplo simple de compra
    public void Buy(TrashItemSO item)
    {
        if (!wallet || !item) return;
        if (wallet.Balance >= item.Worth)
        {
            wallet.TrySpend(item.Worth);
            if (moneyLabel) moneyLabel.text = "$" + wallet.Balance;
            // TODO: entregar ítem / mejora
        }
        else
        {
            // TODO: feedback sin saldo
        }
    }

    // Botón "Cerrar" en el UI
    public void OnClickClose() => Close();
}