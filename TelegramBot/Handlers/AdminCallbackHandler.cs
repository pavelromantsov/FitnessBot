using System;
using System.Threading.Tasks;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.TelegramBot.DTO;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class AdminCallbackHandler : ICallbackHandler
    {
        private readonly UserService _userService;

        public AdminCallbackHandler(UserService userService)
        {
            _userService = userService;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string data)
        {
            if (!data.StartsWith("make_admin", StringComparison.OrdinalIgnoreCase))
                return false;

            // Check if caller is admin
            if (context.User.Role != UserRole.Admin)
            {
                await context.Bot.AnswerCallbackQuery(
                    context.CallbackQuery!.Id,
                    "Нет прав для назначения админов.",
                    cancellationToken: default);
                return true;
            }

            // Parse: make_admin|telegramId
            var parts = data.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !long.TryParse(parts[1], out var targetTelegramId))
            {
                await context.Bot.AnswerCallbackQuery(
                    context.CallbackQuery!.Id,
                    "Некорректные данные.",
                    cancellationToken: default);
                return true;
            }

            var ok = await _userService.MakeAdminAsync(targetTelegramId);
            if (!ok)
            {
                await context.Bot.AnswerCallbackQuery(
                    context.CallbackQuery!.Id,
                    "Пользователь не найден.",
                    cancellationToken: default);
                return true;
            }

            await context.Bot.AnswerCallbackQuery(
                context.CallbackQuery!.Id,
                $"Пользователь {targetTelegramId} назначен администратором.",
                cancellationToken: default);

            if (context.CallbackQuery!.Message != null)
            {
                await context.Bot.EditMessageText(
                    context.CallbackQuery.Message.Chat.Id,
                    context.CallbackQuery.Message.MessageId,
                    $"Пользователь {targetTelegramId} назначен администратором.",
                    cancellationToken: default);
            }

            return true;
        }
    }
}
