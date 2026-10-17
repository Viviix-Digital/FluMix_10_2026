namespace FluMix.Data
{
    public interface IPlayerRepository
    {
        bool TryLogin(string username, string password, out PlayerRecord player);

        void UpsertLocal(PlayerRecord player);
    }
}
