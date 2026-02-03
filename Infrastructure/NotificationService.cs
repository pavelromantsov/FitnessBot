using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using Telegram.Bot.Types;

namespace FitnessBot.Infrastructure
{
    public class NotificationService
    {
        private readonly INotificationRepository _notifications;
        private readonly IDailyGoalRepository _goals;
        private readonly IUserRepository _users;

        public NotificationService(INotificationRepository notifications)
        {
            _notifications = notifications;
        }

        public Task<long> ScheduleAsync(
            long userId,
            string type,
            string text,
            DateTime scheduledAt,
            CancellationToken ct = default)
        {
            var n = new Notification
            {
                UserId = userId,
                Type = type,
                Text = text,
                ScheduledAt = scheduledAt,
                IsSent = false
            };

            return _notifications.AddAsync(n);
        }

        public Task<IReadOnlyList<Notification>> GetDueAsync(DateTime beforeUtc) =>
            _notifications.GetScheduledAsync(beforeUtc);

        public Task MarkSentAsync(long id, DateTime sentAt) =>
            _notifications.MarkSentAsync(id, sentAt);

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
