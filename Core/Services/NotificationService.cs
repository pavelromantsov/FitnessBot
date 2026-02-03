using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;

namespace FitnessBot.Core.Services
{
    public class NotificationService
    {
        private readonly IDailyGoalRepository _goals;
        private readonly IUserRepository _users;

        public NotificationService(IDailyGoalRepository goals, IUserRepository users)
        {
            _goals = goals;
            _users = users;
        }

        // Пример бизнес‑метода: проверить, достиг ли пользователь цели на день
        public async Task<bool> IsDailyGoalCompletedAsync(long userId, DateTime dayUtc,
            double caloriesIn, double caloriesOut, int steps)
        {
            var goal = await _goals.GetByUserAndDateAsync(userId, dayUtc.Date);
            if (goal is null) return false;

            bool okSteps = steps >= goal.TargetSteps;
            bool okCalIn = caloriesIn <= goal.TargetCaloriesIn;
            bool okCalOut = caloriesOut >= goal.TargetCaloriesOut;

            bool completed = okSteps && okCalIn && okCalOut;

            if (completed && !goal.IsCompleted)
            {
                goal.IsCompleted = true;
                goal.CompletedAt = DateTime.UtcNow;
                await _goals.SaveAsync(goal);
            }

            return completed;
        }
    }
}
