using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.Core.Services.LogMeal;
using FitnessBot.Scenarios;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using DomainUser = FitnessBot.Core.Entities.User;

namespace FitnessBot.TelegramBot
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly UserService _userService;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly System.Collections.Generic.List<IScenario> _scenarios;
        private readonly IErrorLogRepository _errorLogRepo;
        private readonly LogMealClient _logMealClient;

        // Handlers
        private readonly ICommandHandler[] _commandHandlers;
        private readonly ICallbackHandler[] _callbackHandlers;
        private readonly IEnumerable<IPhotoHandler> _photoHandlers;

        public delegate void MessageEventHandler(string message);
        public event MessageEventHandler? OnHandleUpdateStarted;
        public event MessageEventHandler? OnHandleUpdateCompleted;


        public UpdateHandler(
            ITelegramBotClient botClient,
            UserService userService,
            IScenarioContextRepository contextRepository,
            System.Collections.Generic.IEnumerable<IScenario> scenarios,
            ICommandHandler[] commandHandlers,
            ICallbackHandler[] callbackHandlers,
            IErrorLogRepository errorLogRepo,
            LogMealClient logMealClient,
            IEnumerable<IPhotoHandler> photoHandlers
            )
        {
            _botClient = botClient;
            _userService = userService;
            _contextRepository = contextRepository;
            _scenarios = scenarios.ToList();
            _commandHandlers = commandHandlers;
            _callbackHandlers = callbackHandlers;
            _errorLogRepo = errorLogRepo;
            _logMealClient = logMealClient;
            _photoHandlers = photoHandlers.ToList();
        }

        public async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken)
        {
            var text = update.Message?.Text ?? update.CallbackQuery?.Data ?? 
                update.Type.ToString();
            OnHandleUpdateStarted?.Invoke(text);

            try
            {
                await (update switch
                {
                    { Message: { } message } => OnMessage(update, message, cancellationToken),
                    { CallbackQuery: { } callbackQuery } => OnCallbackQuery(update, 
                    callbackQuery, cancellationToken),
                    _ => OnUnknown(update, cancellationToken)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки обновления: {ex}");
                
                await _errorLogRepo.AddAsync(new ErrorLog
                {
                    Level = "Error",
                    Message = $"Update {update.Id}: {ex.Message}",
                    StackTrace = ex.StackTrace,
                    ContextJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        UpdateId = update.Id,
                        UpdateType = update.Type.ToString(),
                        UserId = update.Message?.From?.Id ?? update.CallbackQuery?.From?.Id
                    }),
                    Timestamp = DateTime.UtcNow
                });
            }

            OnHandleUpdateCompleted?.Invoke(text);
        }

        public Task HandlePollingErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"❌ Ошибка polling: {exception}");
            return Task.CompletedTask;
        }

        public Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            HandleErrorSource source,
            CancellationToken cancellationToken)
        {
            string message;

            if (exception is ApiRequestException apiEx)
            {
                message = $"Telegram API Error [{apiEx.ErrorCode}] from {source}: " +
                    $"{apiEx.Message}";
            }
            else
            {
                message = $"Unexpected error from {source}: {exception.Message}";
            }

            Console.WriteLine(message);
            return Task.Delay(1000, cancellationToken);
        }

        private async Task OnMessage(Update update, Message message, CancellationToken ct)
        {
            if (message.From == null)
                return;

            var chatId = message.Chat.Id;
            var telegramId = message.From.Id;
            var firstName = message.From.FirstName ?? "Unknown";

            if (telegramId == 0)
            {
                await _botClient.SendMessage(
                    chatId,
                    "Не удалось определить твой TelegramId.",
                    cancellationToken: ct);
                return;
            }

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

            // Создаём UpdateContext
            var updateContext = new UpdateContext(
                _botClient,
                user,
                chatId,
                message,
                callbackQuery: null,
                cancellationToken: ct);

            // Фото → IPhotoHandler
            if (message.Photo is { Length: > 0 })
            {
                foreach (var photoHandler in _photoHandlers)
                {
                    if (await photoHandler.HandleAsync(updateContext))
                        return;
                }
            }

            if (message.Text is null)
                return;

            var context = await _contextRepository.GetContext(user.Id, ct);

            if (message.Text.Equals("/cancel", StringComparison.OrdinalIgnoreCase) && context != null)
            {
                await _contextRepository.ResetContext(user.Id, ct);
                await _botClient.SendMessage(
                    chatId,
                    "Сценарий остановлен.",
                    cancellationToken: ct);
                return;
            }

            if (context != null)
            {
                await ProcessScenario(context, message, ct);
                return;
            }

            var messageText = message.Text;
            string command;
            string[] args;

            if (messageText.StartsWith('/'))
            {
                var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                command = parts.FirstOrDefault() ?? string.Empty;
                args = parts.Skip(1).ToArray();
            }
            else
            {
                command = messageText;
                args = Array.Empty<string>();
            }

            await HandleCommand(user, chatId, message, command, args, ct);
        }

        private async Task HandleCommand(
            DomainUser user,
            long chatId,
            Message message,
            string command,
            string[] args,
            CancellationToken ct)
        {
            var context = new UpdateContext(
                _botClient,
                user,
                chatId,
                message,
                null,
                ct);

            foreach (var handler in _commandHandlers)
            {
                if (await handler.HandleAsync(context, command, args))
                    return; 
            }

            await _botClient.SendMessage(
                chatId,
                "Неизвестная команда. Используйте /help.",
                cancellationToken: ct);
        }

        private async Task OnCallbackQuery(Update update, CallbackQuery callbackQuery, 
            CancellationToken ct)
        {
            try
            {
                var data = callbackQuery.Data ?? string.Empty;
                Console.WriteLine($"Callback received: {data}");

                var chatId = callbackQuery.Message?.Chat.Id ?? 0;
                var telegramId = callbackQuery.From.Id;

                if (chatId == 0)
                    return;

                var user = await _userService.GetByTelegramIdAsync(telegramId);
                if (user == null)
                {
                    await _botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Пользователь не найден.",
                        cancellationToken: ct);
                    return;
                }

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
                catch { }
            }
        }

        private async Task HandleCallback(
            DomainUser user,
            long chatId,
            CallbackQuery callbackQuery,
            string data,
            CancellationToken ct)
        {
            var context = new UpdateContext(
                _botClient,
                user,
                chatId,
                null,
                callbackQuery,
                ct);

            foreach (var handler in _callbackHandlers)
            {
                if (await handler.HandleAsync(context, data))
                    return; 
            }

            await _botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                "Неизвестное действие.",
                cancellationToken: ct);
        }

        private async Task ProcessScenario(
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            var scenario = GetScenario(context.CurrentScenario);
            var result = await scenario.HandleMessageAsync(_botClient, context, message, ct);

            if (result == ScenarioResult.Completed)
            {
                await _contextRepository.ResetContext(context.UserId, ct);
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Сценарий завершён. Используйте /start для других команд.",
                    cancellationToken: ct);
            }
            else
            {
                await _contextRepository.SetContext(context.UserId, context, ct);
            }
        }

        private IScenario GetScenario(ScenarioType type)
        {
            var scenario = _scenarios.FirstOrDefault(s => s.CanHandle(type));
            if (scenario == null)
                throw new InvalidOperationException($"Сценарий {type} не найден");
            return scenario;
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
    }
}