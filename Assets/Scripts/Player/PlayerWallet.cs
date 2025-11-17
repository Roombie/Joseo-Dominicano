using UnityEngine;

public class PlayerWallet : MonoBehaviour, IWallet
{
    [SerializeField] private int balance;
    public int Balance => balance;

    public System.Action<int> OnBalanceChanged;

    public void AddMoney(int amount)
    {
        balance += amount;
        OnBalanceChanged?.Invoke(Balance);
    }

    public bool TrySpend(int amount)
    {
        if (balance < amount) return false;
        balance -= amount;
        OnBalanceChanged?.Invoke(Balance);
        return true;
    }
}