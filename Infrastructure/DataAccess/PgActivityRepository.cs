using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using LinqToDB.Async;
using LinqToDB;
using LinqToDB.Data;
using FitnessBot.Core.DataAccess.Models;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgActivityRepository : IActivityRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgActivityRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // маппинг Activity <-> ActivityModel
        private static ActivityModel Map(Activity a) => new()
        {
            Id = a.Id,
            UserId = a.UserId,
            Date = a.Date,
            Steps = a.Steps,
            ActiveMinutes = a.ActiveMinutes,
            CaloriesBurned = a.CaloriesBurned,
            Source = a.Source
        };

        private static Activity Map(ActivityModel m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            Date = m.Date,
            Steps = m.Steps,
            ActiveMinutes = m.ActiveMinutes,
            CaloriesBurned = m.CaloriesBurned,
            Source = m.Source
        };

        public async Task AddAsync(Activity activity)
        {
            await using var db = _connectionFactory();
            var model = Map(activity);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            activity.Id = model.Id;
        }

        public async Task<IReadOnlyList<Activity>> GetByUserAndPeriodAsync(long userId, DateTime from, DateTime to)
        {
            await using var db = _connectionFactory();
            var models = await db.Activities
                .Where(a => a.UserId == userId &&
                            a.Date >= from.Date &&
                            a.Date < to.Date)
                .OrderBy(a => a.Date)
                .ToListAsync(); // List<ActivityModel>

            return models.Select(Map).ToList(); // List<Activity> -> IReadOnlyList<Activity>
        }
    }
}
