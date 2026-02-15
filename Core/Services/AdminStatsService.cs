using FitnessBot.Core.Abstractions;

namespace FitnessBot.Core.Services
{
    public class AdminStatsService
    {
        private readonly IAdminStatsRepository _repo;

        public AdminStatsService(IAdminStatsRepository repo)
        {
            _repo = repo;
        }

        public Task<int> GetDailyActiveUsersAsync(DateTime dayUtc) =>
            _repo.GetDailyActiveUsersAsync(dayUtc);

        public Task<IDictionary<string, int>> GetAgeDistributionAsync() =>
            _repo.GetAgeDistributionAsync();

        public Task<IDictionary<string, int>> GetGeoDistributionAsync() =>
            _repo.GetGeoDistributionAsync();

        public Task<long> GetTotalContentVolumeAsync() =>
            _repo.GetTotalContentVolumeAsync();
    }

}
