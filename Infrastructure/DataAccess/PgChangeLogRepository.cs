using FitnessBot.Core.Abstractions;
using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgChangeLogRepository : IChangeLogRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgChangeLogRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory ?? 
                throw new ArgumentNullException(nameof(connectionFactory));
        }

        private static ChangeLogModel MapToModel(ChangeLog c) => new()
        {
            Id = c.Id,
            AdminUserId = c.AdminUserId,
            Timestamp = c.Timestamp,
            ChangeType = c.ChangeType,
            Details = c.Details
        };

        private static ChangeLog MapToEntity(ChangeLogModel m) => new()
        {
            Id = m.Id,
            AdminUserId = m.AdminUserId,
            Timestamp = m.Timestamp,
            ChangeType = m.ChangeType,
            Details = m.Details
        };

        public async Task AddAsync(ChangeLog log)
        {
            await using var db = _connectionFactory();
            var model = MapToModel(log);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            log.Id = model.Id;
        }

        public async Task<List<ChangeLog>> GetRecentAsync(int count)
        {
            await using var db = _connectionFactory();
            var models = await db.ChangeLogs
                .OrderByDescending(c => c.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(MapToEntity).ToList();
        }

        public async Task<List<ChangeLog>> GetByAdminIdAsync(long adminUserId, int count)
        {
            await using var db = _connectionFactory();
            var models = await db.ChangeLogs
                .Where(c => c.AdminUserId == adminUserId)
                .OrderByDescending(c => c.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(MapToEntity).ToList();
        }

        public async Task<int> GetCountAsync()
        {
            await using var db = _connectionFactory();
            return await db.ChangeLogs.CountAsync();
        }
    }
}
