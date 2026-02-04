using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace FitnessBot.Scenarios
{
    public class ActivityReminderSettingsScenario : IScenario
    {
        private readonly UserService _userService;

        public ActivityReminderSettingsScenario(UserService userService)
        {
            _userService = userService;
        }

        public ScenarioType ScenarioType => ScenarioType.ActivityReminderSettings;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            if (context.CurrentStep == 0)
            {
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Включить все", "activity_reminders_all_on"),
                        InlineKeyboardButton.WithCallbackData("❌ Отключить все", "activity_reminders_all_off")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("☀️ Утренние (9:00)", "activity_reminders_morning"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🍽 Обеденные (13:00)", "activity_reminders_lunch"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🧘‍♂️ Дневные (16:00)", "activity_reminders_afternoon"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🌆 Вечерние (19:00)", "activity_reminders_evening"),
                    }
                });

                await bot.SendMessage(
                    message.Chat.Id,
                    "⚙️ Настройка напоминаний об активности\n\n" +
                    "Выберите, какие напоминания вы хотите получать:\n\n" +
                    "☀️ Утренние (9:00) - мотивация на начало дня\n" +
                    "🍽 Обеденные (13:00) - напоминание пройтись\n" +
                    "🧘‍♂️ Дневные (16:00) - разминка и растяжка\n" +
                    "🌆 Вечерние (19:00) - проверка выполнения целей\n\n" +
                    "Текущие настройки будут обновлены автоматически.",
                    replyMarkup: keyboard,
                    cancellationToken: ct);

                return ScenarioResult.Completed;
            }

            return ScenarioResult.Completed;
        }
    }
}
