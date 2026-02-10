using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;

namespace FitnessBot.Infrastructure
{
    public class NotificationService
    {
        private readonly INotificationRepository _notifications;
        private readonly IDailyGoalRepository _goals;

        public NotificationService(
            INotificationRepository notifications,
            IDailyGoalRepository goals)
        {
            _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            _goals = goals ?? throw new ArgumentNullException(nameof(goals));
        }

        /// <summary>
        /// Запланировать отправку уведомления
        /// </summary>
        public Task<long> ScheduleAsync(
            long userId,
            string type,
            string text,
            DateTime scheduledAt,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(text);

            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Text = text,
                ScheduledAt = scheduledAt,
                IsSent = false
            };

            return _notifications.AddAsync(notification);
        }

        /// <summary>
        /// Получить список уведомлений, которые должны быть отправлены
        /// </summary>
        public Task<IReadOnlyList<Notification>> GetDueAsync(DateTime beforeUtc) =>
            _notifications.GetScheduledAsync(beforeUtc);

        /// <summary>
        /// Отметить уведомление как отправленное
        /// </summary>
        public Task MarkSentAsync(long id, DateTime sentAt) =>
            _notifications.MarkSentAsync(id, sentAt);

        /// <summary>
        /// Проверить, достиг ли пользователь ежедневной цели
        /// </summary>
        public async Task<bool> IsDailyGoalCompletedAsync(
            long userId,
            DateTime dayUtc,
            double caloriesIn,
            double caloriesOut,
            int steps)
        {
            var goal = await _goals.GetByUserAndDateAsync(userId, dayUtc.Date);
            if (goal is null)
                return false;

            bool stepsAchieved = steps >= goal.TargetSteps;
            bool caloriesInOk = caloriesIn <= goal.TargetCaloriesIn;
            bool caloriesOutOk = caloriesOut >= goal.TargetCaloriesOut;

            bool completed = stepsAchieved && caloriesInOk && caloriesOutOk;

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
