using FitnessBot.Core.Abstractions;
using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgActivityRepository : IActivityRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgActivityRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

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

        public async Task<IReadOnlyList<Activity>> GetByUserAndPeriodAsync(long userId, 
                DateTime from, DateTime to)
        {
            await using var db = _connectionFactory();
            var models = await db.Activities
                .Where(a => a.UserId == userId &&
                            a.Date >= from.Date &&
                            a.Date < to.Date)
                .OrderBy(a => a.Date)
                .ToListAsync();

            return models.Select(Map).ToList();
        }

        public async Task<Activity?> GetByUserDateAndSourceAsync(long userId, 
                DateTime dateUtc, string source, CancellationToken ct)
        {
            var day = dateUtc.Date;

            await using var db = _connectionFactory();
            var model = await db.Activities
                .Where(a => a.UserId == userId
                            && a.Date == day
                            && a.Source == source)
                .FirstOrDefaultAsync(ct);

            return model is null ? null : Map(model);
        }

        public async Task UpdateAsync(Activity activity, CancellationToken ct)
        {
            await using var db = _connectionFactory();
            var model = Map(activity);
            await db.UpdateAsync(model, token: ct);
        }
    }
}
