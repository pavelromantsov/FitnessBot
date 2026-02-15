using System.Globalization;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FitnessBot.Scenarios
{
    public class SetDailyGoalScenario : IScenario
    {
        private readonly IDailyGoalRepository _dailyGoalRepository;

        public SetDailyGoalScenario(IDailyGoalRepository dailyGoalRepository)
        {
            _dailyGoalRepository = dailyGoalRepository;
        }

        public ScenarioType ScenarioType => ScenarioType.SetDailyGoal;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            var text = message.Text ?? string.Empty;

            switch (context.CurrentStep)
            {
                case 0:
                    await bot.SendMessage(
                        message.Chat.Id,
                        "Установка ежедневной цели 🎯\n\n" +
                        "Шаг 1/3: Введите целевое количество шагов (например: 10000):",
                        cancellationToken: ct);
                    context.CurrentStep = 1;
                    return ScenarioResult.InProgress;

                case 1:
                    if (!int.TryParse(text, out var steps) || steps < 0)
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            "Введите целевое количество шагов числом (например: 10000):",
                            cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    context.Data["steps"] = steps.ToString(CultureInfo.InvariantCulture);

                    await bot.SendMessage(
                        message.Chat.Id,
                        "Шаг 2/3: Введите максимальное потребление калорий (например: 2000):",
                        cancellationToken: ct);
                    context.CurrentStep = 2;
                    return ScenarioResult.InProgress;

                case 2:
                    if (!double.TryParse(text, out var caloriesIn) || caloriesIn < 0)
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            "Введите максимальное потребление калорий числом (например: 2000):",
                            cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    context.Data["caloriesIn"] = caloriesIn.ToString(CultureInfo.InvariantCulture);

                    await bot.SendMessage(
                        message.Chat.Id,
                        "Шаг 3/3: Введите минимальный расход калорий (например: 500):",
                        cancellationToken: ct);
                    context.CurrentStep = 3;
                    return ScenarioResult.InProgress;

                case 3:
                    if (!double.TryParse(text, out var caloriesOut) || caloriesOut < 0)
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            "Введите минимальный расход калорий числом (например: 500):",
                            cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    // Сохраняем цель
                    var stepsGoal = int.Parse(context.Data["steps"]!.ToString()!, 
                        CultureInfo.InvariantCulture);
                    var caloriesInGoal = double.Parse(context.Data["caloriesIn"]!.ToString()!, 
                        CultureInfo.InvariantCulture);

                    var today = DateTime.UtcNow.Date;

                    var goal = new DailyGoal
                    {
                        UserId = context.UserId,
                        Date = today,
                        TargetSteps = stepsGoal,
                        TargetCaloriesIn = caloriesInGoal,
                        TargetCaloriesOut = caloriesOut,
                        IsCompleted = false
                    };

                    await _dailyGoalRepository.SaveAsync(goal);

                    await bot.SendMessage(
                        message.Chat.Id,
                        $"✅ Ежедневная цель установлена!\n\n" +
                        $"🎯 Шаги: {stepsGoal}\n" +
                        $"🍽 Макс. калорий (потребление): {caloriesInGoal:F0}\n" +
                        $"🔥 Мин. калорий (расход): {caloriesOut:F0}\n\n" +
                        $"Бот будет автоматически отслеживать ваш прогресс и " +
                        $"уведомит о достижении цели!",
                        cancellationToken: ct);

                    return ScenarioResult.Completed;

                default:
                    return ScenarioResult.Completed;
            }
        }
    }
}
