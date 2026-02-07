namespace FitnessBot.Core.Abstractions
{
    public interface IAdminStatsRepository
    {
        Task<int> GetDailyActiveUsersAsync(DateTime dayUtc);
        Task<IDictionary<string, int>> GetAgeDistributionAsync();
        Task<IDictionary<string, int>> GetGeoDistributionAsync();
        Task<long> GetTotalContentVolumeAsync();
    }
}
