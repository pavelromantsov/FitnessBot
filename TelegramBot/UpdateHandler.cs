using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using FitnessBot.Infrastructure.DataAccess;
using FitnessBot.Scenarios;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DomainUser = FitnessBot.Core.Entities.User;

namespace FitnessBot.TelegramBot
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly UserService _userService;
        private readonly BmiService _bmiService;
        private readonly ActivityService _activityService;
        private readonly NutritionService _nutritionService;
        private readonly ReportService _reportService;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly IScenario[] _scenarios;
        private readonly PgNutritionRepository _nutritionRepo;
        private readonly PgActivityRepository _activityRepo;
        private readonly AdminStatsService _adminStatsService;
        private readonly ChartService _chartService;
        private readonly ChartDataService _chartDataService;
        private readonly ChartImageService _chartImageService;

        public delegate void MessageEventHandler(string message);
        public event MessageEventHandler? OnHandleUpdateStarted;
        public event MessageEventHandler? OnHandleUpdateCompleted;

        public UpdateHandler(
            ITelegramBotClient botClient,
            UserService userService,
            BmiService bmiService,
            ActivityService activityService,
            NutritionService nutritionService,
            ReportService reportService,
            IScenarioContextRepository contextRepository,
            IScenario[] scenarios,
            PgNutritionRepository nutritionRepo,
            PgActivityRepository activityRepo,
            AdminStatsService adminStatsService,
            ChartService chartService,
            ChartDataService chartDataService,
            ChartImageService chartImageService)
        {
            _botClient = botClient;
            _userService = userService;
            _bmiService = bmiService;
            _activityService = activityService;
            _nutritionService = nutritionService;
            _reportService = reportService;
            _contextRepository = contextRepository;
            _scenarios = scenarios;
            _nutritionRepo = nutritionRepo;
            _activityRepo = activityRepo;
            _adminStatsService = adminStatsService;
            _chartService = chartService;
            _chartDataService = chartDataService;
            _chartImageService = chartImageService; // без дубля
        }

        public async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken)
        {
            var text = update.Message?.Text
                       ?? update.CallbackQuery?.Data
                       ?? update.Type.ToString();

            OnHandleUpdateStarted?.Invoke(text);

            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        if (update.Message != null)
                            await OnMessage(update.Message, cancellationToken);
                        break;

                    case UpdateType.CallbackQuery:
                        if (update.CallbackQuery != null)
                            await OnCallbackQuery(update.CallbackQuery, cancellationToken);
                        break;

                    default:
                        await OnUnknown(update, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки обновления: {ex}");
            }

            OnHandleUpdateCompleted?.Invoke(text);
        }

        public Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            HandleErrorSource source,
            CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error [{apiRequestException.ErrorCode}]: {apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine($"❌ Ошибка polling ({source}): {errorMessage}");
            return Task.CompletedTask;
        }


        private async Task OnMessage(Message message, CancellationToken ct)
        {
            if (message.Text is null)
                return;

            var chatId = message.Chat.Id;
            var telegramId = message.From?.Id ?? 0;
            var firstName = message.From?.FirstName ?? "Unknown";

            if (telegramId == 0)
            {
                await _botClient.SendMessage(
                    chatId,
                    "Не удалось определить твой TelegramId.",
                    cancellationToken: ct);
                return;
            }

            // пользователь
            var user = await _userService.GetByTelegramIdAsync(telegramId);
            if (user == null)
            {
                user = await _userService.RegisterOrUpdateAsync(
                    telegramId,
                    firstName,
                    null,
                    null);

                var regContext = new ScenarioContext
                {
                    UserId = user.Id,
                    CurrentScenario = ScenarioType.Registration,
                    CurrentStep = 0
                };

                await _contextRepository.SetContext(user.Id, regContext, ct);

                var regScenario = GetScenario(ScenarioType.Registration);
                await regScenario.HandleMessageAsync(_botClient, regContext, message, ct);
                return;
            }

            user = await _userService.RegisterOrUpdateAsync(
                telegramId,
                firstName,
                user.Age,
                user.City);

            var scenarioContext = await _contextRepository.GetContext(user.Id, ct);

            if (message.Text.Equals("/cancel", StringComparison.OrdinalIgnoreCase)
                && scenarioContext != null)
            {
                await _contextRepository.ResetContext(user.Id, ct);
                await _botClient.SendMessage(
                    chatId,
                    "Сценарий остановлен.",
                    cancellationToken: ct);
                return;
            }

            if (scenarioContext != null)
            {
                await ProcessScenario(user, scenarioContext, message, ct);
                return;
            }

            // команды
            var parts = message.Text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var command = parts.FirstOrDefault() ?? string.Empty;
            var args = parts.Skip(1).ToArray();

            await HandleCommand(user, chatId, command, args, ct);
        }

        private async Task OnCallbackQuery(CallbackQuery callbackQuery, CancellationToken ct)
        {
            var data = callbackQuery.Data ?? string.Empty;
            var chatId = callbackQuery.Message?.Chat.Id ?? 0;

            if (chatId == 0)
            {
                await _botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "Ошибка: чат не найден.",
                    cancellationToken: ct);
                return;
            }

            var user = await _userService.GetByTelegramIdAsync(callbackQuery.From.Id);
            if (user == null)
            {
                await _botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "Пользователь не найден.",
                    cancellationToken: ct);
                return;
            }

            try
            {
                await HandleCallback(user, chatId, callbackQuery, data, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Callback error: {ex}");
                try
                {
                    await _botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Ошибка при обработке нажатия.",
                        cancellationToken: ct);
                }
                catch
                {
                    // ignore
                }
            }
        }

        private async Task OnUnknown(Update update, CancellationToken ct)
        {
            var chatId = update.Message?.Chat.Id
                         ?? update.CallbackQuery?.Message?.Chat.Id
                         ?? 0;

            if (chatId == 0)
                return;

            await _botClient.SendMessage(
                chatId,
                "Неизвестный тип обновления.",
                cancellationToken: ct);
        }

        private async Task ProcessScenario(
            DomainUser user,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            var scenario = GetScenario(context.CurrentScenario);
            var result = await scenario.HandleMessageAsync(_botClient, context, message, ct);

            if (result == ScenarioResult.Completed)
            {
                await _contextRepository.ResetContext(user.Id, ct);
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Сценарий завершён. Используйте /start для других команд.",
                    cancellationToken: ct);
            }
            else
            {
                await _contextRepository.SetContext(user.Id, context, ct);
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Для выхода из сценария используйте /cancel.",
                    cancellationToken: ct);
            }
        }

        private IScenario GetScenario(ScenarioType type)
        {
            var scenario = _scenarios.FirstOrDefault(s => s.CanHandle(type));
            if (scenario == null)
                throw new InvalidOperationException($"Сценарий {type} не найден");
            return scenario;
        }

        // ---------------- Команды и callback’и ----------------
        // Здесь просто оставляешь твой существующий код:
        // методы HandleCommand, HandleCallback и все приватные хелперы,
        // ровно как они сейчас в оригинальном UpdateHandler.cs,
        // без изменений сигнатур.

        private Task HandleCommand(
            DomainUser user,
            long chatId,
            string command,
            string[] args,
            CancellationToken ct)
        {
            // оставь этот метод как в твоём исходном файле
            throw new NotImplementedException();
        }

        private Task HandleCallback(
            DomainUser user,
            long chatId,
            CallbackQuery callbackQuery,
            string data,
            CancellationToken ct)
        {
            // оставь этот метод как в твоём исходном файле
            throw new NotImplementedException();
        }
    }
}