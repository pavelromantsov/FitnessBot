using System.Globalization;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FitnessBot.Scenarios
{
    public class ManualActivityScenario : IScenario
    {
        private readonly ActivityService _activityService;

        public ManualActivityScenario(ActivityService activityService)
        {
            _activityService = activityService;
        }

        public ScenarioType ScenarioType => ScenarioType.ManualActivity;
        public bool CanHandle(ScenarioType type) => type == ScenarioType.ManualActivity;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            var chatId = message.Chat.Id;
            var text = message.Text?.Trim() ?? string.Empty;

            switch (context.CurrentStep)
            {
                // показываем кнопки выбора типа
                case 0:
                    {
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("👣 Ходьба/Бег", "act_type:steps"),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("🏋️ Силовая/Йога", "act_type:time")
                            }
                        });

                        await bot.SendMessage(
                            chatId,
                            "🎯 Выберите тип активности:",
                            replyMarkup: keyboard,
                            cancellationToken: ct);

                        return ScenarioResult.InProgress;
                    }

                // ввод минут (activityType уже сохранён callback-хендлером)
                case 1:
                    {
                        if (!int.TryParse(text, out var minutes) || minutes <= 0)
                        {
                            await bot.SendMessage(
                                chatId,
                                "⏱️ Введите длительность в минутах (например: 45):",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        context.Data["activeMinutes"] = minutes;

                        var actType = context.Data.TryGetValue("activityType", out var tObj)
                            ? tObj?.ToString()
                            : "steps";

                        if (actType == "steps")
                        {
                            await bot.SendMessage(
                                chatId,
                                "👣 Введите количество шагов:",
                                cancellationToken: ct);
                            context.CurrentStep = 2;
                        }
                        else
                        {
                            // Для тренировок: сразу спрашиваем калории
                            await bot.SendMessage(
                                chatId,
                                "🔥 Введите сожжённые калории (или 0):",
                                cancellationToken: ct);
                            context.CurrentStep = 3;
                        }

                        return ScenarioResult.InProgress;
                    }

                // ввод шагов (только для steps-based)
                case 2:
                    {
                        if (!int.TryParse(text, out var stepsValue) || stepsValue < 0)
                        {
                            await bot.SendMessage(
                                chatId,
                                "❌ Введите число шагов (например: 5000):",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        context.Data["steps"] = stepsValue;

                        await bot.SendMessage(
                            chatId,
                            "🔥 Введите сожжённые калории (или 0):",
                            cancellationToken: ct);

                        context.CurrentStep = 3;
                        return ScenarioResult.InProgress;
                    }

                // ввод калорий и сохранение
                case 3:
                    {
                        if (!double.TryParse(
                                text.Replace(",", "."),
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out var calories))
                        {
                            calories = 0;
                        }

                        var activityType = context.Data.TryGetValue("activityType", out var typeObj)
                                           && typeObj?.ToString() == "time"
                            ? ActivityType.TimeBased
                            : ActivityType.StepsBased;

                        var steps = context.Data.TryGetValue("steps", out var sObj) && sObj is int sVal
                            ? sVal
                            : 0;

                        var minutes = context.Data.TryGetValue("activeMinutes", out var mObj) && mObj is int mVal
                            ? mVal
                            : 0;

                        await _activityService.AddAsync(
                            context.UserId,
                            steps,
                            minutes,
                            calories,
                            source: "manual",
                            type: activityType);

                        var icon = activityType == ActivityType.TimeBased ? "🏋️" : "👣";
                        var title = activityType == ActivityType.TimeBased ? "Тренировка" : "Активность";

                        var resultMessage = $"✅ {title} сохранена!\n\n";
                        if (steps > 0)
                            resultMessage += $"👣 Шаги: {steps:N0}\n";
                        resultMessage += $"⏱️ Длительность: {minutes} мин\n";
                        resultMessage += $"🔥 Калории: {calories:F0}";

                        await bot.SendMessage(chatId, resultMessage, cancellationToken: ct);
                        return ScenarioResult.Completed;
                    }

                default:
                    return ScenarioResult.Completed;
            }
        }
    }
}
