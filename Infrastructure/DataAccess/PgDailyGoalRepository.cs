using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using LinqToDB.Async;
using LinqToDB;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgDailyGoalRepository : IDailyGoalRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgDailyGoalRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        private static DailyGoalModel MapToModel(DailyGoal g) => new()
        {
            Id = g.Id,
            UserId = g.UserId,
            Date = g.Date,
            TargetSteps = g.TargetSteps,
            TargetCaloriesIn = g.TargetCaloriesIn,
            TargetCaloriesOut = g.TargetCaloriesOut,
            IsCompleted = g.IsCompleted,
            CompletedAt = g.CompletedAt
        };

        private static DailyGoal MapToEntity(DailyGoalModel m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            Date = m.Date,
            TargetSteps = m.TargetSteps,
            TargetCaloriesIn = m.TargetCaloriesIn,
            TargetCaloriesOut = m.TargetCaloriesOut,
            IsCompleted = m.IsCompleted,
            CompletedAt = m.CompletedAt
        };

        public async Task<DailyGoal?> GetByUserAndDateAsync(long userId, DateTime date)
        {
            await using var db = _connectionFactory();
            var model = await db.DailyGoals
                .Where(g => g.UserId == userId && g.Date == date.Date)
                .FirstOrDefaultAsync();

            return model == null ? null : MapToEntity(model);
        }

        public async Task SaveAsync(DailyGoal goal)
        {
            await using var db = _connectionFactory();
            var model = MapToModel(goal);

            var existingModel = await db.DailyGoals
                .Where(g => g.UserId == model.UserId && g.Date == model.Date)
                .FirstOrDefaultAsync();

            if (existingModel != null)
            {
                model.Id = existingModel.Id;
                await db.UpdateAsync(model);
                goal.Id = model.Id;
            }
            else
            {
                model.Id = await db.InsertWithInt64IdentityAsync(model);
                goal.Id = model.Id;
            }
        }
    }
}
