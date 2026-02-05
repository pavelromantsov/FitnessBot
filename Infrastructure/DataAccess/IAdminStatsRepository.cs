namespace FitnessBot.Infrastructure.DataAccess
{
    public interface IAdminStatsRepository
    {
        Task<int> GetDailyActiveUsersAsync(DateTime dayUtc);
        Task<IDictionary<string, int>> GetAgeDistributionAsync();
        Task<IDictionary<string, int>> GetGeoDistributionAsync();
        Task<long> GetTotalContentVolumeAsync();
    }
}
