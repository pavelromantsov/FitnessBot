using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgChangeLogRepository : IChangeLogRepository
    {
        private readonly Func<PgDataContext> _dataContextFactory;

        public PgChangeLogRepository(Func<PgDataContext> dataContextFactory)
        {
            _dataContextFactory = dataContextFactory;
        }

        public async Task AddAsync(ChangeLog changeLog)
        {
            await using var db = _dataContextFactory();

            var model = new ChangeLogModel
            {
                AdminUserId = changeLog.AdminUserId,
                Timestamp = changeLog.Timestamp,
                ChangeType = changeLog.ChangeType,
                Details = changeLog.Details
            };

            await db.InsertAsync(model);
        }

        public async Task<List<ChangeLog>> GetRecentAsync(int count)
        {
            await using var db = _dataContextFactory();

            var models = await db.GetTable<ChangeLogModel>()
                .OrderByDescending(c => c.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(m => new ChangeLog
            {
                Id = m.Id,
                AdminUserId = m.AdminUserId,
                Timestamp = m.Timestamp,
                ChangeType = m.ChangeType,
                Details = m.Details
            }).ToList();
        }

        public async Task<List<ChangeLog>> GetByAdminIdAsync(long adminUserId, int count)
        {
            await using var db = _dataContextFactory();

            var models = await db.GetTable<ChangeLogModel>()
                .Where(c => c.AdminUserId == adminUserId)
                .OrderByDescending(c => c.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(m => new ChangeLog
            {
                Id = m.Id,
                AdminUserId = m.AdminUserId,
                Timestamp = m.Timestamp,
                ChangeType = m.ChangeType,
                Details = m.Details
            }).ToList();
        }

        public async Task<int> GetCountAsync()
        {
            await using var db = _dataContextFactory();
            return await db.GetTable<ChangeLogModel>().CountAsync();
        }
    }
}
