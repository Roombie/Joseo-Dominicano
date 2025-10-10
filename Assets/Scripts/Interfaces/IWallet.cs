public interface IWallet
{
    int Balance { get; } 
    void AddMoney(int amount);
    bool TrySpend(int amount);
}