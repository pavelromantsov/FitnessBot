using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class AdminCommandsHandler : ICommandHandler
    {
        private readonly UserService _userService;
        private readonly AdminStatsService _adminStatsService;
        private readonly IErrorLogRepository _errorLogRepo;
        private readonly IContentItemRepository _contentRepo;
        private readonly IChangeLogRepository _changeLogRepo;

        public AdminCommandsHandler(
            UserService userService,
            AdminStatsService adminStatsService,
            IErrorLogRepository errorLogRepo,
            IContentItemRepository contentRepo,
            IChangeLogRepository changeLogRepo)
        {
            _userService = userService;
            _adminStatsService = adminStatsService;
            _errorLogRepo = errorLogRepo;
            _contentRepo = contentRepo;
            _changeLogRepo = changeLogRepo;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string command, string[] args)
        {
            if (!IsAdmin(context.User))
            {
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

            Console.WriteLine($"DEBUG AdminCommandsHandler: '{normalizedCommand}'");

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

            // Команды списка пользователей
            if (normalizedCommand.Contains("admin_users") || normalizedCommand == "/admin_users")
            {
                await AdminUsersCommand(context);
                return true;
            }

            // Детали пользователя
            if (normalizedCommand.Contains("admin_user") || normalizedCommand == "/admin_user")
            {
                await AdminUserDetailsCommand(context, args);
                return true;
            }

            // Назначить администратором
            if (normalizedCommand.Contains("make_admin") || normalizedCommand == "/make_admin")
            {
                await MakeAdminCommand(context, args);
                return true;
            }

            // Снять с роли администратора
            if (normalizedCommand.Contains("make_user") || normalizedCommand == "/make_user")
            {
                await MakeUserCommand(context, args);
                return true;
            }

            // Поиск пользователя
            if (normalizedCommand.Contains("admin_find") || normalizedCommand == "/admin_find")
            {
                await AdminFindUserCommand(context, args);
                return true;
            }

            // Активность пользователей
            if (normalizedCommand.Contains("admin_activity") || normalizedCommand == "/admin_activity")
            {
                await AdminActivityCommand(context);
                return true;
            }

            // Статистика
            if (normalizedCommand.Contains("admin_stats") || normalizedCommand == "/admin_stats")
            {
                await AdminStatsCommand(context);
                return true;
            }

            // Логи ошибок
            if (normalizedCommand.Contains("error_logs") || normalizedCommand == "/error_logs")
            {
                await ErrorLogCommand(context);
                return true;
            }

            // Статистика контента
            if (normalizedCommand.Contains("content_stats") || normalizedCommand == "/content_stats")
            {
                await ContentStatsCommand(context);
                return true;
            }

            // История изменений
            if (normalizedCommand.Contains("change_log") || normalizedCommand == "/change_log")
            {
                await ChangeLogCommand(context);
                return true;
            }

            // Тестовая запись в changelog
            if (normalizedCommand.Contains("test_changelog") || normalizedCommand == "/test_changelog")
            {
                await TestChangeLogCommand(context);
                return true;
            }

            // Тестовая ошибка
            if (normalizedCommand.Contains("test_error") || normalizedCommand == "/test_error")
            {
                await TestErrorLogCommand(context);
                return true;
            }

            // Просмотр логов ошибок
            if (normalizedCommand.Contains("errorlog") || normalizedCommand == "/errorlog")
            {
                await ErrorLogCommand(context);
                return true;
            }

            Console.WriteLine($"DEBUG AdminCommandsHandler: Команда не распознана");
            return false;
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

            var targetUser = await _userService.GetByTelegramIdAsync(targetTelegramId);
            if (targetUser == null)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    $"Пользователь с TelegramId={targetTelegramId} не найден.",
                    cancellationToken: default);
                return;
            }

            var ok = await _userService.MakeAdminAsync(targetTelegramId);
            if (!ok)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "Не удалось назначить администратора.",
                    cancellationToken: default);
                return;
            }

            // Логируем действие
            await _changeLogRepo.AddAsync(new ChangeLog
            {
                AdminUserId = ctx.User.Id,
                ChangeType = "PromoteToAdmin",
                Details = $"User {targetUser.Name} (ID: {targetUser.Id}) promoted to Admin",
                Timestamp = DateTime.UtcNow
            });

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                $"Пользователь {targetUser.Name} (TelegramId={targetTelegramId}) " +
                $"назначен администратором.",
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

            var targetUser = await _userService.GetByTelegramIdAsync(targetTelegramId);
            if (targetUser == null)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    $"Пользователь с TelegramId={targetTelegramId} не найден.",
                    cancellationToken: default);
                return;
            }

            var ok = await _userService.MakeUserAsync(targetTelegramId);
            if (!ok)
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "Не удалось изменить роль.",
                    cancellationToken: default);
                return;
            }

            // Логируем действие
            await _changeLogRepo.AddAsync(new ChangeLog
            {
                AdminUserId = ctx.User.Id,
                ChangeType = "DemoteToUser",
                Details = $"User {targetUser.Name} (ID: {targetUser.Id}) demoted to User",
                Timestamp = DateTime.UtcNow
            });

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                $"Пользователь {targetUser.Name} (TelegramId={targetTelegramId}) " +
                $"теперь обычный пользователь.",
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
                        $"  Последняя активность: {u.LastActivityAt:dd.MM.yyyy HH:mm} " +
                        $"UTC ({status})";
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
                    .Select(kv => $"{(string.IsNullOrWhiteSpace(kv.Key) ? "(не указан)" : 
                    kv.Key)}: {kv.Value}"));
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

        private async Task ContentStatsCommand(UpdateContext ctx)
        {
            if (ctx.User.Role != UserRole.Admin)
                return;

            var totalSize = await _contentRepo.GetTotalSizeAsync();
            var sizeMb = totalSize / (1024.0 * 1024.0);

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                $"📊 **Статистика контента:**\n\n" +
                $"Всего загружено: {sizeMb:F2} МБ",
                cancellationToken: default);
        }

        private async Task ChangeLogCommand(UpdateContext ctx)
        {
            if (ctx.User.Role != UserRole.Admin)
                return;

            var changes = await _changeLogRepo.GetRecentAsync(20);

            var text = "📜 **История изменений:**\n\n";

            foreach (var change in changes)
            {
                if (!change.AdminUserId.HasValue)
                    continue;

                var admin = await _userService.GetByIdAsync(change.AdminUserId.Value);

                if (admin != null)
                {
                    text += $"🕐 {change.Timestamp:dd.MM HH:mm}\n";
                    text += $"👤 Администратор: {admin.Name}\n";
                    text += $"📝 {change.ChangeType}: {change.Details}\n\n";
                }
            }

            if (changes.Count == 0)
            {
                text += "История пуста.";
            }

            await ctx.Bot.SendMessage(ctx.ChatId, text, cancellationToken: default);
        }

        private async Task TestChangeLogCommand(UpdateContext ctx)
        {
            if (ctx.User.Role != UserRole.Admin)
                return;

            try
            {
                await _changeLogRepo.AddAsync(new ChangeLog
                {
                    AdminUserId = ctx.User.Id,
                    ChangeType = "TestAction",
                    Details = "Тестовая запись для проверки логирования",
                    Timestamp = DateTime.UtcNow
                });

                Console.WriteLine("✅ Запись добавлена в change_logs");

                // Retry логика для отправки сообщения
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        await ctx.Bot.SendMessage(
                            ctx.ChatId,
                            "✅ Тестовая запись добавлена в change_logs. Проверьте командой /change_log",
                            cancellationToken: default);
                        break; 
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Попытка {i + 1}/3 не удалась: {ex.Message}");
                        if (i < 2)
                            await Task.Delay(1000); 
                        else
                            throw; 
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в TestChangeLogCommand: {ex.Message}");
            }
        }


        private async Task TestErrorLogCommand(UpdateContext ctx)
        {
            if (ctx.User.Role != UserRole.Admin)
                return;

            try
            {
                throw new InvalidOperationException("Тестовая ошибка для проверки логирования");
            }
            catch (Exception ex)
            {
                await _errorLogRepo.AddAsync(new ErrorLog
                {
                    Id = ctx.User.Id,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Timestamp = DateTime.UtcNow,
                    Level = "Error"
                });

                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "✅ Тестовая ошибка залогирована. Проверьте командой /errorlog",
                    cancellationToken: default);
            }
        }

        private async Task ErrorLogCommand(UpdateContext ctx)
        {
            if (ctx.User.Role != UserRole.Admin)
                return;

            var errors = await _errorLogRepo.GetRecentAsync(10);

            var text = "🔴 **Последние ошибки:**\n\n";

            foreach (var error in errors)
            {
                var user = await _userService.GetByIdAsync(error.Id);

                text += $"🕐 {error.Timestamp:dd.MM.yyyy HH:mm}\n";
                text += $"👤 Пользователь: {user?.Name ?? "Unknown"} (ID: {error.Id})\n";
                text += $"❌ Ошибка: {error.Message}\n";
                text += $"Level: {error.Level}\n";
                text += "---\n\n";
            }

            if (errors.Count == 0)
            {
                text += "Ошибок не найдено.";
            }

            await ctx.Bot.SendMessage(ctx.ChatId, text, cancellationToken: default);
        }
    }
}
