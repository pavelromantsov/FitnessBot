using FitnessBot.Core.Abstractions;
using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgErrorLogRepository : IErrorLogRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgErrorLogRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory ?? 
                throw new ArgumentNullException(nameof(connectionFactory));
        }

        private static ErrorLogModel MapToModel(ErrorLog e) => new()
        {
            Id = e.Id,
            UserId = e.UserId,
            Timestamp = e.Timestamp,
            Level = e.Level,
            Message = e.Message,
            StackTrace = e.StackTrace,
            ContextJson = null
        };

        private static ErrorLog MapToEntity(ErrorLogModel m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            Timestamp = m.Timestamp,
            Level = m.Level,
            Message = m.Message,
            StackTrace = m.StackTrace
        };

        public async Task AddAsync(ErrorLog log)
        {
            await using var db = _connectionFactory();
            var model = MapToModel(log);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            log.Id = model.Id;
        }

        public async Task<List<ErrorLog>> GetRecentAsync(int count)
        {
            await using var db = _connectionFactory();
            var models = await db.ErrorLogs
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(MapToEntity).ToList();
        }

        public async Task<List<ErrorLog>> GetByUserIdAsync(long userId, int count)
        {
            await using var db = _connectionFactory();
            var models = await db.ErrorLogs
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(MapToEntity).ToList();
        }

        public async Task<int> GetCountAsync()
        {
            await using var db = _connectionFactory();
            return await db.ErrorLogs.CountAsync();
        }
    }
}
