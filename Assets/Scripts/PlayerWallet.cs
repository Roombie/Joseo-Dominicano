using UnityEngine;

public class PlayerWallet : MonoBehaviour, IWallet
{
    [SerializeField] private int balance;
    public int Balance => balance;

    public System.Action<int> OnBalanceChanged;

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        balance += amount;
        OnBalanceChanged?.Invoke(balance);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || amount > balance) return false;
        balance -= amount;
        OnBalanceChanged?.Invoke(balance);
        return true;
    }
}