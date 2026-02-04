using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Services;
using FitnessBot.Infrastructure;

namespace FitnessBot.BackgroundTasks
{
    public class DailyGoalCheckBackgroundTask : IBackgroundTask
    {
        private readonly UserService _userService;
        private readonly IActivityRepository _activityRepository;
        private readonly IMealRepository _mealRepository;
        private readonly IDailyGoalRepository _dailyGoalRepository;
        private readonly NotificationService _notificationService;

        public DailyGoalCheckBackgroundTask(
            UserService userService,
            IActivityRepository activityRepository,
            IMealRepository mealRepository,
            IDailyGoalRepository dailyGoalRepository,
            NotificationService notificationService)
        {
            _userService = userService;
            _activityRepository = activityRepository;
            _mealRepository = mealRepository;
            _dailyGoalRepository = dailyGoalRepository;
            _notificationService = notificationService;
        }

        public async Task Start(CancellationToken ct)
        {
            Console.WriteLine("✅ DailyGoalCheckBackgroundTask запущена");

            // Запускаем бесконечный цикл проверки каждые 10 минут
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await CheckAllUsersGoals(ct);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"❌ Ошибка в DailyGoalCheckBackgroundTask: {ex}");
                }

                // Проверяем каждые 10 минут
                await Task.Delay(TimeSpan.FromMinutes(10), ct);
            }

            Console.WriteLine("⛔ DailyGoalCheckBackgroundTask остановлена");
        }

        private async Task CheckAllUsersGoals(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var tomorrow = today.AddDays(1);

            var users = await _userService.GetAllAsync();

            foreach (var user in users)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    await CheckUserDailyGoal(user.Id, today, tomorrow, now, ct);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"❌ Ошибка проверки цели для пользователя {user.Id}: {ex.Message}");
                }
            }
        }

        private async Task CheckUserDailyGoal(
            long userId,
            DateTime today,
            DateTime tomorrow,
            DateTime now,
            CancellationToken ct)
        {
            // Получаем цель на сегодня
            var goal = await _dailyGoalRepository.GetByUserAndDateAsync(userId, today);

            if (goal == null || goal.IsCompleted)
                return; // Нет цели или уже выполнена

            // Получаем данные за сегодня
            var meals = await _mealRepository.GetByUserAndPeriodAsync(userId, today, tomorrow);
            var activities = await _activityRepository.GetByUserAndPeriodAsync(userId, today, tomorrow);

            var caloriesIn = meals.Sum(m => m.Calories);
            var caloriesOut = activities.Sum(a => a.CaloriesBurned);
            var steps = activities.Sum(a => a.Steps);

            // Проверяем выполнение целей
            bool stepsGoalMet = steps >= goal.TargetSteps;
            bool caloriesInGoalMet = caloriesIn <= goal.TargetCaloriesIn;
            bool caloriesOutGoalMet = caloriesOut >= goal.TargetCaloriesOut;

            bool allGoalsMet = stepsGoalMet && caloriesInGoalMet && caloriesOutGoalMet;

            if (allGoalsMet)
            {
                // Помечаем цель как выполненную
                goal.IsCompleted = true;
                goal.CompletedAt = now;
                await _dailyGoalRepository.SaveAsync(goal);

                // Отправляем уведомление о достижении цели
                var message =
                    $"🎉 Поздравляем! Вы достигли своей ежедневной цели!\n\n" +
                    $"✅ Шаги: {steps} / {goal.TargetSteps}\n" +
                    $"✅ Потреблено калорий: {caloriesIn:F0} / {goal.TargetCaloriesIn:F0}\n" +
                    $"✅ Потрачено калорий: {caloriesOut:F0} / {goal.TargetCaloriesOut:F0}\n\n" +
                    $"Отличная работа! Продолжайте в том же духе! 💪";

                await _notificationService.ScheduleAsync(
                    userId,
                    "DailyGoalAchieved",
                    message,
                    now,
                    ct);

                Console.WriteLine($"✅ Пользователь {userId} достиг ежедневной цели!");
            }
            else
            {
                // Опционально: отправляем промежуточное напоминание вечером (если ещё не выполнена)
                var hour = now.Hour;

                // Например, в 20:00 напоминаем о незавершённой цели
                if (hour == 20)
                {
                    // Проверяем, не отправляли ли уже сегодня напоминание
                    var existingReminder = (await _notificationService.GetDueAsync(tomorrow))
                        .FirstOrDefault(n =>
                            n.UserId == userId &&
                            n.Type == "DailyGoalReminder" &&
                            n.ScheduledAt.Date == today);

                    if (existingReminder == null)
                    {
                        var reminderMessage =
                            $"⏰ Напоминание о ежедневной цели:\n\n" +
                            $"{(stepsGoalMet ? "✅" : "❌")} Шаги: {steps} / {goal.TargetSteps}\n" +
                            $"{(caloriesInGoalMet ? "✅" : "❌")} Потреблено калорий: {caloriesIn:F0} / {goal.TargetCaloriesIn:F0}\n" +
                            $"{(caloriesOutGoalMet ? "✅" : "❌")} Потрачено калорий: {caloriesOut:F0} / {goal.TargetCaloriesOut:F0}\n\n" +
                            $"Ещё есть время достичь цели! 🏃‍♂️";

                        await _notificationService.ScheduleAsync(
                            userId,
                            "DailyGoalReminder",
                            reminderMessage,
                            now,
                            ct);
                    }
                }
            }
        }
    }
}
