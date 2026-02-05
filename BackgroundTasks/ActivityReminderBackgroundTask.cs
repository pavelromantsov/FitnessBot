using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Services;
using FitnessBot.Infrastructure;

namespace FitnessBot.BackgroundTasks
{
    public class ActivityReminderBackgroundTask : IBackgroundTask
    {
        private readonly UserService _userService;
        private readonly IActivityRepository _activityRepository;
        private readonly IDailyGoalRepository _dailyGoalRepository;
        private readonly NotificationService _notificationService;

        public ActivityReminderBackgroundTask(
            UserService userService,
            IActivityRepository activityRepository,
            IDailyGoalRepository dailyGoalRepository,
            NotificationService notificationService)
        {
            _userService = userService;
            _activityRepository = activityRepository;
            _dailyGoalRepository = dailyGoalRepository;
            _notificationService = notificationService;
        }

        public async Task Start(CancellationToken ct)
        {
            Console.WriteLine("✅ ActivityReminderBackgroundTask запущена");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await CheckActivityReminders(ct);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"❌ Ошибка в ActivityReminderBackgroundTask: {ex}");
                }

                // Проверяем каждые 5 минут
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }

            Console.WriteLine("⛔ ActivityReminderBackgroundTask остановлена");
        }

        private async Task CheckActivityReminders(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var tomorrow = today.AddDays(1);
            var currentHour = now.Hour;
            var currentMinute = now.Minute;

            // Напоминания отправляем только в рабочее время (8:00 - 22:00)
            if (currentHour < 8 || currentHour >= 22)
                return;

            var users = await _userService.GetAllAsync();

            foreach (var user in users)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    // 1. Утреннее напоминание о начале активности (9:00)
                    if (currentHour == 9 && currentMinute < 5)
                    {
                        await SendMorningActivityReminder(user.Id, today, tomorrow, now, ct);
                    }

                    // 2. Обеденное напоминание о шагах (13:00)
                    if (currentHour == 13 && currentMinute < 5)
                    {
                        await SendLunchTimeActivityReminder(user.Id, today, tomorrow, now, ct);
                    }

                    // 3. Дневное напоминание о разминке (16:00)
                    if (currentHour == 16 && currentMinute < 5)
                    {
                        await SendAfternoonStretchReminder(user.Id, today, tomorrow, now, ct);
                    }

                    // 4. Вечернее напоминание о достижении цели (19:00)
                    if (currentHour == 19 && currentMinute < 5)
                    {
                        await SendEveningGoalReminder(user.Id, today, tomorrow, now, ct);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"❌ Ошибка обработки напоминаний для пользователя {user.Id}: {ex.Message}");
                }
            }
        }

        // 1. Утреннее напоминание (9:00)
        private async Task SendMorningActivityReminder(
            long userId,
            DateTime today,
            DateTime tomorrow,
            DateTime now,
            CancellationToken ct)
        {
            // ← ВОТ ЗДЕСЬ ДОБАВЛЕНА ПРОВЕРКА НАСТРОЕК
            var user = await _userService.GetByIdAsync(userId);

            // Проверяем настройки пользователя
            if (user == null || !user.ActivityRemindersEnabled || !user.MorningReminderEnabled)
                return;

            // Проверяем, не отправляли ли уже сегодня
            if (await WasReminderSentToday(userId, "MorningActivity", today, tomorrow))
                return;

            var goal = await _dailyGoalRepository.GetByUserAndDateAsync(userId, today);

            string message;
            if (goal != null)
            {
                message =
                    $"☀️ Доброе утро! Начинаем новый день!\n\n" +
                    $"Ваша цель на сегодня:\n" +
                    $"🚶 {goal.TargetSteps} шагов\n" +
                    $"🔥 {goal.TargetCaloriesOut:F0} ккал на активность\n\n" +
                    $"Начните день с утренней прогулки или зарядки! 💪";
            }
            else
            {
                message =
                    $"☀️ Доброе утро! Начинаем новый день!\n\n" +
                    $"Рекомендуем установить ежедневную цель: /setgoal\n\n" +
                    $"Начните день с утренней прогулки или зарядки! 💪";
            }

            await _notificationService.ScheduleAsync(
                userId,
                "MorningActivity",
                message,
                now,
                ct);

            Console.WriteLine($"✅ Отправлено утреннее напоминание пользователю {userId}");
        }

        // 2. Обеденное напоминание о шагах (13:00)
        private async Task SendLunchTimeActivityReminder(
            long userId,
            DateTime today,
            DateTime tomorrow,
            DateTime now,
            CancellationToken ct)
        {
            // ← ВОТ ЗДЕСЬ ДОБАВЛЕНА ПРОВЕРКА НАСТРОЕК
            var user = await _userService.GetByIdAsync(userId);

            // Проверяем настройки пользователя
            if (user == null || !user.ActivityRemindersEnabled || !user.LunchReminderEnabled)
                return;

            // Проверяем, не отправляли ли уже сегодня
            if (await WasReminderSentToday(userId, "LunchTimeActivity", today, tomorrow))
                return;

            var activities = await _activityRepository.GetByUserAndPeriodAsync(userId, today, tomorrow);
            var currentSteps = activities.Sum(a => a.Steps);

            var goal = await _dailyGoalRepository.GetByUserAndDateAsync(userId, today);
            var targetSteps = goal?.TargetSteps ?? 10000;

            // Ожидаемый прогресс к 13:00 (примерно 40% дня)
            var expectedSteps = (int)(targetSteps * 0.4);

            string message;
            if (currentSteps < expectedSteps)
            {
                var deficit = expectedSteps - currentSteps;
                message =
                    $"🍽 Обеденный перерыв - время для активности!\n\n" +
                    $"Текущий прогресс: {currentSteps} / {targetSteps} шагов\n" +
                    $"Рекомендуемый прогресс к этому времени: {expectedSteps} шагов\n\n" +
                    $"💡 Совет: Пройдитесь во время обеда (~{Math.Min(deficit, 1000)} шагов).\n" +
                    $"Прогулка поможет пищеварению и взбодрит! 🚶‍♂️";
            }
            else
            {
                message =
                    $"🍽 Обеденный перерыв!\n\n" +
                    $"Отличный прогресс: {currentSteps} / {targetSteps} шагов ✅\n\n" +
                    $"Продолжайте в том же духе! 💪";
            }

            await _notificationService.ScheduleAsync(
                userId,
                "LunchTimeActivity",
                message,
                now,
                ct);

            Console.WriteLine($"✅ Отправлено обеденное напоминание пользователю {userId}");
        }

        // 3. Дневное напоминание о разминке (16:00)
        private async Task SendAfternoonStretchReminder(
            long userId,
            DateTime today,
            DateTime tomorrow,
            DateTime now,
            CancellationToken ct)
        {
            // ← ВОТ ЗДЕСЬ ДОБАВЛЕНА ПРОВЕРКА НАСТРОЕК
            var user = await _userService.GetByIdAsync(userId);

            // Проверяем настройки пользователя
            if (user == null || !user.ActivityRemindersEnabled || !user.AfternoonReminderEnabled)
                return;

            // Проверяем, не отправляли ли уже сегодня
            if (await WasReminderSentToday(userId, "AfternoonStretch", today, tomorrow))
                return;

            var activities = await _activityRepository.GetByUserAndPeriodAsync(userId, today, tomorrow);
            var activeMinutes = activities.Sum(a => a.ActiveMinutes);

            string message =
                $"🧘‍♂️ Время для разминки!\n\n" +
                $"Вы уже активны сегодня: {activeMinutes} минут\n\n" +
                $"💡 Рекомендации:\n" +
                $"• Встаньте и пройдитесь 5-10 минут\n" +
                $"• Сделайте растяжку шеи и плеч\n" +
                $"• Поднимитесь по лестнице вместо лифта\n\n" +
                $"Короткие перерывы повышают продуктивность! 🚀";

            await _notificationService.ScheduleAsync(
                userId,
                "AfternoonStretch",
                message,
                now,
                ct);

            Console.WriteLine($"✅ Отправлено дневное напоминание пользователю {userId}");
        }

        // 4. Вечернее напоминание о достижении цели (19:00)
        private async Task SendEveningGoalReminder(
            long userId,
            DateTime today,
            DateTime tomorrow,
            DateTime now,
            CancellationToken ct)
        {
            // ← ВОТ ЗДЕСЬ ДОБАВЛЕНА ПРОВЕРКА НАСТРОЕК
            var user = await _userService.GetByIdAsync(userId);

            // Проверяем настройки пользователя
            if (user == null || !user.ActivityRemindersEnabled || !user.EveningReminderEnabled)
                return;

            // Проверяем, не отправляли ли уже сегодня
            if (await WasReminderSentToday(userId, "EveningGoal", today, tomorrow))
                return;

            var goal = await _dailyGoalRepository.GetByUserAndDateAsync(userId, today);

            // Если нет цели или цель уже выполнена, не отправляем
            if (goal == null || goal.IsCompleted)
                return;

            var activities = await _activityRepository.GetByUserAndPeriodAsync(userId, today, tomorrow);
            var currentSteps = activities.Sum(a => a.Steps);
            var currentCaloriesBurned = activities.Sum(a => a.CaloriesBurned);

            var stepsRemaining = Math.Max(0, goal.TargetSteps - currentSteps);
            var caloriesRemaining = Math.Max(0, goal.TargetCaloriesOut - currentCaloriesBurned);

            // Если осталось мало до выполнения цели
            if (stepsRemaining > 0 || caloriesRemaining > 0)
            {
                string message =
                    $"🌆 Вечерняя проверка целей!\n\n" +
                    $"Осталось до выполнения цели:\n" +
                    $"🚶 Шаги: {stepsRemaining} из {goal.TargetSteps}\n" +
                    $"🔥 Калории: {caloriesRemaining:F0} из {goal.TargetCaloriesOut:F0}\n\n";

                if (stepsRemaining <= 2000 && stepsRemaining > 0)
                {
                    var walkMinutes = (int)(stepsRemaining / 100); // примерно 100 шагов в минуту
                    message += $"💡 Совет: Вечерняя прогулка {walkMinutes}-{walkMinutes + 5} минут закроет цель!\n\n";
                }
                else if (stepsRemaining > 2000)
                {
                    message += $"💡 Совет: Даже небольшая активность - это прогресс. Не переживайте, завтра новый день! 😊\n\n";
                }

                message += $"Ещё есть время! Вы можете это сделать! 💪";

                await _notificationService.ScheduleAsync(
                    userId,
                    "EveningGoal",
                    message,
                    now,
                    ct);

                Console.WriteLine($"✅ Отправлено вечернее напоминание пользователю {userId}");
            }
        }

        // Вспомогательный метод: проверка, отправляли ли уже напоминание сегодня
        private async Task<bool> WasReminderSentToday(
            long userId,
            string reminderType,
            DateTime today,
            DateTime tomorrow)
        {
            var existingNotifications = await _notificationService.GetDueAsync(tomorrow);

            return existingNotifications.Any(n =>
                n.UserId == userId &&
                n.Type == reminderType &&
                n.ScheduledAt.Date == today &&
                (n.IsSent || n.ScheduledAt <= DateTime.UtcNow));
        }
    }
}
