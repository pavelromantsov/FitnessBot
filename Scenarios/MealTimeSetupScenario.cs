using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace FitnessBot.Scenarios
{
    public class MealTimeSetupScenario : IScenario
    {
        private readonly UserService _userService;

        public MealTimeSetupScenario(UserService userService)
        {
            _userService = userService;
        }

        public ScenarioType ScenarioType => ScenarioType.MealTimeSetup;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            // шаг 0 — завтрак
            if (context.CurrentStep == 0)
            {
                if (!TryParseTime(message.Text, out var breakfast))
                {
                    await bot.SendMessage(
                        message.Chat.Id,
                        "Введите время завтрака в формате HH:mm, например: 08:00",
                        cancellationToken: ct);
                    return ScenarioResult.InProgress;
                }

                context.Data["Breakfast"] = breakfast;
                context.CurrentStep = 1;

                await bot.SendMessage(
                    message.Chat.Id,
                    "Теперь время обеда (HH:mm), например: 13:00",
                    cancellationToken: ct);

                return ScenarioResult.InProgress;
            }

            // шаг 1 — обед
            if (context.CurrentStep == 1)
            {
                if (!TryParseTime(message.Text, out var lunch))
                {
                    await bot.SendMessage(
                        message.Chat.Id,
                        "Введите время обеда в формате HH:mm, например: 13:00",
                        cancellationToken: ct);
                    return ScenarioResult.InProgress;
                }

                context.Data["Lunch"] = lunch;
                context.CurrentStep = 2;

                await bot.SendMessage(
                    message.Chat.Id,
                    "И наконец время ужина (HH:mm), например: 19:30",
                    cancellationToken: ct);

                return ScenarioResult.InProgress;
            }

            // шаг 2 — ужин и сохранение в БД
            if (context.CurrentStep == 2)
            {
                if (!TryParseTime(message.Text, out var dinner))
                {
                    await bot.SendMessage(
                        message.Chat.Id,
                        "Введите время ужина в формате HH:mm, например: 19:30",
                        cancellationToken: ct);
                    return ScenarioResult.InProgress;
                }

                if (!context.Data.TryGetValue("Breakfast", out var bRaw) ||
                    !context.Data.TryGetValue("Lunch", out var lRaw))
                {
                    await bot.SendMessage(
                        message.Chat.Id,
                        "Ошибка сценария, начните заново: /setmeals",
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                var telegramId = message.From.Id;
                var user = await _userService.GetByTelegramIdAsync(telegramId);
                if (user == null)
                {
                    await bot.SendMessage(
                        message.Chat.Id,
                        "Пользователь не найден.",
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                user.BreakfastTime = (TimeSpan)bRaw;
                user.LunchTime = (TimeSpan)lRaw;
                user.DinnerTime = dinner;

                await _userService.SaveAsync(user); 

                await bot.SendMessage(
                    message.Chat.Id,
                    $"Сохранил время:\n" +
                    $"Завтрак: {user.BreakfastTime:hh\\:mm}\n" +
                    $"Обед: {user.LunchTime:hh\\:mm}\n" +
                    $"Ужин: {user.DinnerTime:hh\\:mm}",
                    cancellationToken: ct);

                return ScenarioResult.Completed;
            }

            return ScenarioResult.Completed;
        }

        private static bool TryParseTime(string? text, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(
                text?.Trim(),
                "hh\\:mm",
                null,
                out time);
        }
    }
}
