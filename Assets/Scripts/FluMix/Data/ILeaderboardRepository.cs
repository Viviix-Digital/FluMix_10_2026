namespace FluMix.Data
{
    public interface ILeaderboardRepository
    {
        LeaderboardEntry[] GetTop(int n);

        int Submit(string name, int score);
    }
}
