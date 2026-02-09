using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.TelegramBot.DTO;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class AdminCommandsHandler : ICommandHandler
    {
        private readonly UserService _userService;
        private readonly AdminStatsService _adminStatsService;

        public AdminCommandsHandler(
            UserService userService,
            AdminStatsService adminStatsService)
        {
            _userService = userService;
            _adminStatsService = adminStatsService;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string command, string[] args)
        {
            // Only handle if user is admin
            if (!IsAdmin(context.User))
            {
                // For admin commands, return false so they get "unknown command" message
                // Or you can send custom message here and return true
                if (command.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) ||
                    command.StartsWith("/make_", StringComparison.OrdinalIgnoreCase))
                {
                    await context.Bot.SendMessage(
                        context.ChatId,
                        "Эта команда доступна только администратору.",
                        cancellationToken: default);
                    return true;
                }
                return false;
            }

            var normalizedCommand = command.Trim().ToLowerInvariant();

            // Обработка кнопок с эмодзи и русским текстом
            if (normalizedCommand.Contains("админ") && normalizedCommand.Contains("пользовател"))
            {
                await AdminUsersCommand(context);
                return true;
            }

            if (normalizedCommand.Contains("админ") && normalizedCommand.Contains("статистика"))
            {
                await AdminStatsCommand(context);
                return true;
            }

            // Обработка обычных команд
            switch (normalizedCommand)
            {
                case "/admin_users":
                    await AdminUsersCommand(context);
                    return true;

                case "/admin_user":
                    await AdminUserDetailsCommand(context, args);
                    return true;

                case "/make_admin":
                    await MakeAdminCommand(context, args);
                    return true;

                case "/make_user":
                    await MakeUserCommand(context, args);
                    return true;

                case "/admin_find":
                    await AdminFindUserCommand(context, args);
                    return true;

                case "/admin_activity":
                    await AdminActivityCommand(context);
                    return true;

                case "/admin_stats":
                    await AdminStatsCommand(context);
                    return true;

                default:
                    return false;
            }
        }


        private static bool IsAdmin(User user) => user.Role == UserRole.Admin;

        private async Task AdminUsersCommand(UpdateContext ctx)
        {
            var users = await _userService.GetAllAsync();

            if (users.Count == 0)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "Пользователей пока нет.",
                    cancellationToken: default);
                return;
            }

            var lines = users
                .OrderByDescending(u => u.LastActivityAt)
                .Select(u =>
                    $"Id={u.Id}, Tg={u.TelegramId}, " +
                    $"Имя={u.Name}, Роль={u.Role}, " +
                    $"Город={u.City ?? "-"}, Возраст={u.Age?.ToString() ?? "-"}, " +
                    $"Последняя активность={u.LastActivityAt:dd.MM HH:mm}");

            var text = "Список пользователей:\n" + string.Join("\n", lines);
            await ctx.Bot.SendMessage(
                ctx.ChatId,
                text,
                cancellationToken: default);
        }

        private async Task AdminUserDetailsCommand(UpdateContext ctx, string[] args)
        {
            if (args.Length != 1 || !long.TryParse(args[0], out var telegramId))
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "Формат: /admin_user <telegramId>",
                    cancellationToken: default);
                return;
            }

            var user = await _userService.GetByTelegramIdAsync(telegramId);
            if (user == null)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    $"Пользователь с TelegramId={telegramId} не найден.",
                    cancellationToken: default);
                return;
            }

            var text =
                $"Пользователь:\n" +
                $"Id: {user.Id}\n" +
                $"TelegramId: {user.TelegramId}\n" +
                $"Имя: {user.Name}\n" +
                $"Роль: {user.Role}\n" +
                $"Возраст: {user.Age?.ToString() ?? "-"}\n" +
                $"Город: {user.City ?? "-"}\n" +
                $"Создан: {user.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                $"Последняя активность: {user.LastActivityAt:dd.MM.yyyy HH:mm}\n" +
                $"Завтрак: {user.BreakfastTime?.ToString(@"hh\:mm") ?? "-"}\n" +
                $"Обед: {user.LunchTime?.ToString(@"hh\:mm") ?? "-"}\n" +
                $"Ужин: {user.DinnerTime?.ToString(@"hh\:mm") ?? "-"}";

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                text,
                cancellationToken: default);
        }

        private async Task MakeAdminCommand(UpdateContext ctx, string[] args)
        {
            if (args.Length != 1 || !long.TryParse(args[0], out var targetTelegramId))
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "Формат: /make_admin <telegramId>",
                    cancellationToken: default);
                return;
            }

            var ok = await _userService.MakeAdminAsync(targetTelegramId);
            if (!ok)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    $"Пользователь с TelegramId={targetTelegramId} не найден.",
                    cancellationToken: default);
                return;
            }

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                $"Пользователь с TelegramId={targetTelegramId} назначен администратором.",
                cancellationToken: default);
        }

        private async Task MakeUserCommand(UpdateContext ctx, string[] args)
        {
            if (args.Length != 1 || !long.TryParse(args[0], out var targetTelegramId))
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "Формат: /make_user <telegramId>",
                    cancellationToken: default);
                return;
            }

            var ok = await _userService.MakeUserAsync(targetTelegramId);
            if (!ok)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    $"Пользователь с TelegramId={targetTelegramId} не найден.",
                    cancellationToken: default);
                return;
            }

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                $"Пользователь с TelegramId={targetTelegramId} теперь обычный пользователь.",
                cancellationToken: default);
        }

        private async Task AdminFindUserCommand(UpdateContext ctx, string[] args)
        {
            if (args.Length == 0)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "Формат: /admin_find <часть имени>",
                    cancellationToken: default);
                return;
            }

            var namePart = string.Join(" ", args);

            var users = await _userService.FindByNameAsync(namePart);
            if (users.Count == 0)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    $"Пользователи с именем, содержащим \"{namePart}\", не найдены.",
                    cancellationToken: default);
                return;
            }

            var rows = users.Select(u =>
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"{u.Name} (TgId={u.TelegramId}, роль={u.Role})",
                        $"make_admin|{u.TelegramId}")
                }
            ).ToArray();

            var keyboard = new InlineKeyboardMarkup(rows);

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                "Выберите пользователя, которого хотите сделать администратором:",
                replyMarkup: keyboard,
                cancellationToken: default);
        }

        private async Task AdminActivityCommand(UpdateContext ctx)
        {
            var users = await _userService.GetAllAsync();
            if (users.Count == 0)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "Пользователей пока нет.",
                    cancellationToken: default);
                return;
            }

            var now = DateTime.UtcNow;

            var lines = users
                .OrderByDescending(u => u.LastActivityAt)
                .Select(u =>
                {
                    var sinceMinutes = (now - u.LastActivityAt).TotalMinutes;
                    string status =
                        sinceMinutes < 5 ? "онлайн (<5 мин назад)" :
                        sinceMinutes < 60 ? "недавно (до часа назад)" :
                        sinceMinutes < 24 * 60 ? "сегодня" :
                        "давно";

                    return
                        $"Id={u.Id}, Tg={u.TelegramId}, Имя={u.Name},\n" +
                        $"  Последняя активность: {u.LastActivityAt:dd.MM.yyyy HH:mm} UTC ({status})";
                });

            var text = "Активность пользователей:\n\n" + string.Join("\n\n", lines);

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                text,
                cancellationToken: default);
        }

        private async Task AdminStatsCommand(UpdateContext ctx)
        {
            var today = DateTime.UtcNow.Date;

            var dailyActive = await _adminStatsService.GetDailyActiveUsersAsync(today);
            var ageDist = await _adminStatsService.GetAgeDistributionAsync();
            var geoDist = await _adminStatsService.GetGeoDistributionAsync();
            var totalContent = await _adminStatsService.GetTotalContentVolumeAsync();

            string FormatAge()
            {
                if (ageDist.Count == 0) return "нет данных";
                return string.Join(", ", ageDist
                    .OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}: {kv.Value}"));
            }

            string FormatGeo()
            {
                if (geoDist.Count == 0) return "нет данных";
                return string.Join(", ", geoDist
                    .OrderByDescending(kv => kv.Value)
                    .Select(kv => $"{(string.IsNullOrWhiteSpace(kv.Key) ? "(не указан)" : kv.Key)}: {kv.Value}"));
            }

            var text =
                $"Админ-статистика:\n" +
                $"\n" +
                $"Активные пользователей за сегодня ({today:dd.MM.yyyy}): {dailyActive}\n" +
                $"Общий объём контента: {totalContent} байт\n" +
                $"\n" +
                $"Возрастная структура (возраст: количество):\n" +
                $"{FormatAge()}\n" +
                $"\n" +
                $"География (город: количество):\n" +
                $"{FormatGeo()}";

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                text,
                cancellationToken: default);
        }
    }
}
