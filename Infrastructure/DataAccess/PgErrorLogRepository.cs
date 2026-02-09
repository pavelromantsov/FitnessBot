using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using FitnessBot.Core.DataAccess.Models;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgErrorLogRepository : IErrorLogRepository
    {
        private readonly Func<PgDataContext> _dataContextFactory;

        public PgErrorLogRepository(Func<PgDataContext> dataContextFactory)
        {
            _dataContextFactory = dataContextFactory;
        }

        public async Task AddAsync(ErrorLog errorLog)
        {
            await using var db = _dataContextFactory();

            var model = new ErrorLogModel
            {
                UserId = errorLog.UserId,
                Timestamp = errorLog.Timestamp,
                Level = errorLog.Level,
                Message = errorLog.Message,
                StackTrace = errorLog.StackTrace,
                ContextJson = null
            };

            await db.InsertAsync(model);
        }

        public async Task<List<ErrorLog>> GetRecentAsync(int count)
        {
            await using var db = _dataContextFactory();

            var models = await db.ErrorLogs
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(m => new ErrorLog
            {
                Id = m.Id,
                UserId = m.UserId,
                Timestamp = m.Timestamp,
                Level = m.Level,
                Message = m.Message,
                StackTrace = m.StackTrace
            }).ToList();
        }

        public async Task<List<ErrorLog>> GetByUserIdAsync(long userId, int count)
        {
            await using var db = _dataContextFactory();

            var models = await db.ErrorLogs
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(m => new ErrorLog
            {
                Id = m.Id,
                UserId = m.UserId,
                Timestamp = m.Timestamp,
                Level = m.Level,
                Message = m.Message,
                StackTrace = m.StackTrace
            }).ToList();
        }

        public async Task<int> GetCountAsync()
        {
            await using var db = _dataContextFactory();
            return await db.ErrorLogs.CountAsync();
        }
    }
}
