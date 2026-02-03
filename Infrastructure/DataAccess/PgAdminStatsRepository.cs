using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Async;
using LinqToDB.Data;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgAdminStatsRepository : IAdminStatsRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgAdminStatsRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> GetDailyActiveUsersAsync(DateTime dayUtc)
        {
            var from = dayUtc.Date;
            var to = from.AddDays(1);

            await using var db = _connectionFactory();
            return await db.Users
                .Where(u => u.LastActivityAt >= from && u.LastActivityAt < to)
                .CountAsync();
        }

        public async Task<IDictionary<string, int>> GetAgeDistributionAsync()
        {
            await using var db = _connectionFactory();

            var query =
                from u in db.Users
                let bucket =
                    u.Age == null ? "unknown" :
                    u.Age < 18 ? "<18" :
                    u.Age < 30 ? "18-29" :
                    u.Age < 45 ? "30-44" :
                    u.Age < 60 ? "45-59" :
                    "60+"
                group u by bucket
                into g
                select new { Bucket = g.Key, Count = g.Count() };

            var list = await query.ToListAsync();
            return list.ToDictionary(x => x.Bucket, x => x.Count);
        }

        public async Task<IDictionary<string, int>> GetGeoDistributionAsync()
        {
            await using var db = _connectionFactory();

            var query =
                from u in db.Users
                let city = u.City ?? "unknown"
                group u by city
                into g
                select new { City = g.Key, Count = g.Count() };

            var list = await query.ToListAsync();
            return list.ToDictionary(x => x.City, x => x.Count);
        }

        public async Task<long> GetTotalContentVolumeAsync()
        {
            await using var db = _connectionFactory();
            return await db.ContentItems.SumAsync(ci => (long?)ci.SizeBytes) ?? 0L;
        }
    }
}
