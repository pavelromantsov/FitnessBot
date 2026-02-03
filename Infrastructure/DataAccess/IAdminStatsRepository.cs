using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
