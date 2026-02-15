using FitnessBot.Core.Services;
using FitnessBot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace FitnessBot.BackgroundTasks
{
    public class NotificationSenderBackgroundTask : IBackgroundTask
    {
        private readonly ITelegramBotClient _botClient;
        private readonly NotificationService _notificationService;
        private readonly Core.Services.UserService _userService;

        public NotificationSenderBackgroundTask(
            ITelegramBotClient botClient,
            NotificationService notificationService,
            Core.Services.UserService userService)
        {
            _botClient = botClient;
            _notificationService = notificationService;
            _userService = userService;
        }

        public async Task Start(CancellationToken ct)
        {
            Console.WriteLine("✅ NotificationSenderBackgroundTask запущена");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await SendDueNotifications(ct);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"❌ Ошибка в NotificationSenderBackgroundTask: {ex}");
                }

                // Проверяем каждую минуту
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }

            Console.WriteLine("⛔ NotificationSenderBackgroundTask остановлена");
        }

        private async Task SendDueNotifications(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var notifications = await _notificationService.GetDueAsync(now);

            foreach (var notification in notifications)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    var user = await _userService.GetByIdAsync(notification.UserId);
                    if (user == null)
                        continue;

                    await _botClient.SendMessage(user.TelegramId, notification.Text, cancellationToken: ct);
                    await _notificationService.MarkSentAsync(notification.Id, now);

                    Console.WriteLine($"📨 {user.TelegramId} {notification.Type}");
                }
                catch (ApiRequestException ex) when (ex.Message.Contains("chat not found", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine($"[NotificationSender] chat not found for notification {notification.Id}, userId={notification.UserId}");

                    await _notificationService.MarkSentAsync(notification.Id, now);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[NotificationSender] {notification.Id} {ex.Message}");
                }
            }
        }
    }
}
