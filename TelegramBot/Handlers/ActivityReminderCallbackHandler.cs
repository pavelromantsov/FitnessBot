using System;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class ActivityReminderCallbackHandler : ICallbackHandler
    {
        private readonly UserService _userService;
        private ITelegramBotClient? _botClient;

        public ActivityReminderCallbackHandler(UserService userService)
        {
            _userService = userService;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string data)
        {
            _botClient = context.Bot; // Сохраняем ссылку на бота из контекста

            if (!data.StartsWith("activity_reminders_", StringComparison.OrdinalIgnoreCase))
                return false;

            var user = context.User;

            switch (data)
            {
                case "activity_reminders_all_on":
                    user.ActivityRemindersEnabled = true;
                    user.MorningReminderEnabled = true;
                    user.LunchReminderEnabled = true;
                    user.AfternoonReminderEnabled = true;
                    user.EveningReminderEnabled = true;
                    await _userService.SaveAsync(user);

                    await context.Bot.AnswerCallbackQuery(
                        context.CallbackQuery!.Id,
                        "✅ Все напоминания включены!",
                        cancellationToken: default);

                    if (context.CallbackQuery!.Message != null)
                    {
                        await context.Bot.EditMessageText(
                            context.CallbackQuery.Message.Chat.Id,
                            context.CallbackQuery.Message.MessageId,
                            "✅ Настройки сохранены!\n\n" +
                            "Все напоминания об активности включены:\n" +
                            "☀️ Утренние (9:00) - включены\n" +
                            "🍽 Обеденные (13:00) - включены\n" +
                            "🧘‍♂️ Дневные (16:00) - включены\n" +
                            "🌆 Вечерние (19:00) - включены",
                            cancellationToken: default);
                    }
                    break;

                case "activity_reminders_all_off":
                    user.ActivityRemindersEnabled = false;
                    user.MorningReminderEnabled = false;
                    user.LunchReminderEnabled = false;
                    user.AfternoonReminderEnabled = false;
                    user.EveningReminderEnabled = false;
                    await _userService.SaveAsync(user);

                    await context.Bot.AnswerCallbackQuery(
                        context.CallbackQuery!.Id,
                        "❌ Все напоминания отключены!",
                        cancellationToken: default);

                    if (context.CallbackQuery!.Message != null)
                    {
                        await context.Bot.EditMessageText(
                            context.CallbackQuery.Message.Chat.Id,
                            context.CallbackQuery.Message.MessageId,
                            "❌ Настройки сохранены!\n\n" +
                            "Все напоминания об активности отключены.\n" +
                            "Вы можете включить их снова командой /activity_reminders",
                            cancellationToken: default);
                    }
                    break;

                case "activity_reminders_morning":
                    user.MorningReminderEnabled = !user.MorningReminderEnabled;
                    await _userService.SaveAsync(user);

                    await context.Bot.AnswerCallbackQuery(
                        context.CallbackQuery!.Id,
                        user.MorningReminderEnabled
                            ? "✅ Утренние напоминания включены!"
                            : "❌ Утренние напоминания отключены!",
                        cancellationToken: default);

                    if (context.CallbackQuery!.Message != null)
                    {
                        await UpdateActivityReminderMenu(
                            context.CallbackQuery.Message.Chat.Id,
                            context.CallbackQuery.Message.MessageId,
                            user);
                    }
                    break;

                case "activity_reminders_lunch":
                    user.LunchReminderEnabled = !user.LunchReminderEnabled;
                    await _userService.SaveAsync(user);

                    await context.Bot.AnswerCallbackQuery(
                        context.CallbackQuery!.Id,
                        user.LunchReminderEnabled
                            ? "✅ Обеденные напоминания включены!"
                            : "❌ Обеденные напоминания отключены!",
                        cancellationToken: default);

                    if (context.CallbackQuery!.Message != null)
                    {
                        await UpdateActivityReminderMenu(
                            context.CallbackQuery.Message.Chat.Id,
                            context.CallbackQuery.Message.MessageId,
                            user);
                    }
                    break;

                case "activity_reminders_afternoon":
                    user.AfternoonReminderEnabled = !user.AfternoonReminderEnabled;
                    await _userService.SaveAsync(user);

                    await context.Bot.AnswerCallbackQuery(
                        context.CallbackQuery!.Id,
                        user.AfternoonReminderEnabled
                            ? "✅ Дневные напоминания включены!"
                            : "❌ Дневные напоминания отключены!",
                        cancellationToken: default);

                    if (context.CallbackQuery!.Message != null)
                    {
                        await UpdateActivityReminderMenu(
                            context.CallbackQuery.Message.Chat.Id,
                            context.CallbackQuery.Message.MessageId,
                            user);
                    }
                    break;

                case "activity_reminders_evening":
                    user.EveningReminderEnabled = !user.EveningReminderEnabled;
                    await _userService.SaveAsync(user);

                    await context.Bot.AnswerCallbackQuery(
                        context.CallbackQuery!.Id,
                        user.EveningReminderEnabled
                            ? "✅ Вечерние напоминания включены!"
                            : "❌ Вечерние напоминания отключены!",
                        cancellationToken: default);

                    if (context.CallbackQuery!.Message != null)
                    {
                        await UpdateActivityReminderMenu(
                            context.CallbackQuery.Message.Chat.Id,
                            context.CallbackQuery.Message.MessageId,
                            user);
                    }
                    break;

                default:
                    return false;
            }

            return true;
        }

        private async Task UpdateActivityReminderMenu(
            long chatId,
            int messageId,
            FitnessBot.Core.Entities.User user)
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
                    InlineKeyboardButton.WithCallbackData(
                        user.MorningReminderEnabled ? "✅ Утренние (9:00)" : "☐ Утренние (9:00)",
                        "activity_reminders_morning"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        user.LunchReminderEnabled ? "✅ Обеденные (13:00)" : "☐ Обеденные (13:00)",
                        "activity_reminders_lunch"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        user.AfternoonReminderEnabled ? "✅ Дневные (16:00)" : "☐ Дневные (16:00)",
                        "activity_reminders_afternoon"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        user.EveningReminderEnabled ? "✅ Вечерние (19:00)" : "☐ Вечерние (19:00)",
                        "activity_reminders_evening"),
                }
            });

            await _botClient!.EditMessageText(
                chatId,
                messageId,
                "⚙️ Настройка напоминаний об активности\n\n" +
                "Выберите, какие напоминания вы хотите получать:\n\n" +
                "☀️ Утренние (9:00) - мотивация на начало дня\n" +
                "🍽 Обеденные (13:00) - напоминание пройтись\n" +
                "🧘‍♂️ Дневные (16:00) - разминка и растяжка\n" +
                "🌆 Вечерние (19:00) - проверка выполнения целей\n\n" +
                $"Глобальный статус: {(user.ActivityRemindersEnabled ? "включены ✅" : "отключены ❌")}",
                replyMarkup: keyboard,
                cancellationToken: default);
        }
    }
}
