using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.Infrastructure;

namespace FitnessBot.BackgroundTasks
{
    public class MealReminderBackgroundTask : IBackgroundTask
    {
        private readonly UserService _userService;
        private readonly NutritionService _nutritionService;
        private readonly NotificationService _notificationService;

        public MealReminderBackgroundTask(
            UserService userService,
            NutritionService nutritionService,
            NotificationService notificationService)
        {
            _userService = userService;
            _nutritionService = nutritionService;
            _notificationService = notificationService;
        }

        public async Task Start(CancellationToken ct)
        {
            var nowUtc = DateTime.UtcNow;
            var today = nowUtc.Date;

            var users = await _userService.GetAllAsync();

            foreach (var user in users)
            {
                ct.ThrowIfCancellationRequested();

                var localNow = nowUtc; 

                await CheckMealReminder(user, "BreakfastReminder", user.BreakfastTime, today, localNow, ct);
                await CheckMealReminder(user, "LunchReminder", user.LunchTime, today, localNow, ct);
                await CheckMealReminder(user, "DinnerReminder", user.DinnerTime, today, localNow, ct);
            }
        }

        private async Task CheckMealReminder(
            User user,
            string type,
            TimeSpan? mealTime,
            DateTime today,
            DateTime localNow,
            CancellationToken ct)
        {
            if (mealTime == null)
                return;

            var scheduled = today + mealTime.Value;
            var diff = (localNow - scheduled).TotalMinutes;
            if (diff < 0 || diff > 10)
                return;

            var from = scheduled.AddMinutes(-60);
            var to = scheduled.AddMinutes(60);

            var meals = await _nutritionService.GetMealsByUserAndPeriodAsync(user.Id, from, to, ct);
            if (meals.Any())
                return;

            var text = type switch
            {
                "BreakfastReminder" => "Время завтрака! Не пропустите приём пищи 🙂",
                "LunchReminder" => "Пора обедать! Сделайте паузу и поешьте.",
                "DinnerReminder" => "Время ужина! Закончите день полноценным приёмом пищи.",
                _ => "Время приёма пищи!"
            };

            await _notificationService.ScheduleAsync(
                user.Id,
                type,
                text,
                localNow,
                ct);
        }
    }
}
