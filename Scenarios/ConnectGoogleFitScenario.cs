using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FitnessBot.Scenarios
{
    public class ConnectGoogleFitScenario : IScenario
    {
        private readonly UserService _userService;

        public ConnectGoogleFitScenario(UserService userService)
        {
            _userService = userService;
        }

        public ScenarioType ScenarioType => ScenarioType.ConnectGoogleFit;
        public bool CanHandle(ScenarioType type) => type == ScenarioType.ConnectGoogleFit;

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
                case 0:
                    await bot.SendMessage(
                        chatId,
                        "Вставьте access_token (ya29...).",
                        cancellationToken: ct);
                    context.CurrentStep = 1;
                    return ScenarioResult.InProgress;

                case 1:
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        await bot.SendMessage(
                            chatId,
                            "Токен не должен быть пустым. Вставьте access_token.",
                            cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    context.Data["accessToken"] = text;

                    await bot.SendMessage(
                        chatId,
                        "Теперь вставьте refresh_token (или напишите `нет`, если его нет).",
                        cancellationToken: ct);
                    context.CurrentStep = 2;
                    return ScenarioResult.InProgress;

                case 2:
                    {
                        var accessToken = context.Data["accessToken"]?.ToString() ?? string.Empty;
                        string? refreshToken = null;

                        if (!text.Equals("нет", StringComparison.OrdinalIgnoreCase))
                            refreshToken = text;

                        var expiresAtUtc = DateTime.UtcNow.AddHours(1); 

                        var user = await _userService.GetByIdAsync(context.UserId);
                        if (user == null)
                        {
                            await bot.SendMessage(chatId, "Пользователь не найден.", 
                                cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        user.GoogleFitAccessToken = accessToken;
                        user.GoogleFitRefreshToken = refreshToken;
                        user.GoogleFitTokenExpiresAt = expiresAtUtc;
                                                
                        await _userService.SaveAsync(user);

                        await bot.SendMessage(
                            chatId,
                            "Google Fit подключён. Фоновый синк начнёт подтягивать шаги и калории.",
                            cancellationToken: ct);

                        return ScenarioResult.Completed;
                    }

                default:
                    return ScenarioResult.Completed;
            }
        }
    }

}
