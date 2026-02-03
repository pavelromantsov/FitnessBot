using FitnessBot.Core.Entities;
using System.Threading;
using FitnessBot.Core.Services;
using FitnessBot.Scenarios;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using DomainUser = FitnessBot.Core.Entities.User;
using FitnessBot.TelegramBot.DTO;
using FitnessBot.Core.Abstractions;
using static LinqToDB.Common.Configuration;
using System;


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
            private readonly List<IScenario> _scenarios;

            public delegate void MessageEventHandler(string message);
            public event MessageEventHandler? OnHandleUpdateStarted;
            public event MessageEventHandler? OnHandleUpdateCompleted;
            private readonly IMealRepository _mealRepository;
            private readonly IActivityRepository _activityRepository;


        public UpdateHandler(
    ITelegramBotClient botClient,
    UserService userService,
    BmiService bmiService,
    ActivityService activityService,
    NutritionService nutritionService,
    ReportService reportService,
    IScenarioContextRepository contextRepository,
    IEnumerable<IScenario> scenarios,
    IMealRepository mealRepository,
    IActivityRepository activityRepository)
        {
            _botClient = botClient;
            _userService = userService;
            _bmiService = bmiService;
            _activityService = activityService;
            _nutritionService = nutritionService;
            _reportService = reportService;
            _contextRepository = contextRepository;
            _scenarios = scenarios.ToList();
            _mealRepository = mealRepository;
            _activityRepository = activityRepository;
        }

        // ---------------- IUpdateHandler ----------------

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                var text = update.Message?.Text ?? update.CallbackQuery?.Data ?? update.Type.ToString();
                OnHandleUpdateStarted?.Invoke(text);

                try
                {
                    await (update switch
                    {
                        { Message: { } message } => OnMessage(update, message, cancellationToken),
                        { CallbackQuery: { } callbackQuery } => OnCallbackQuery(update, callbackQuery, cancellationToken),
                        _ => OnUnknown(update, cancellationToken)
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка обработки обновления: {ex}");
                }

                OnHandleUpdateCompleted?.Invoke(text);
            }

            public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                Console.WriteLine($"❌ Ошибка polling: {exception}");
                return Task.CompletedTask;
            }

            // ---------------- Поиск сценария ----------------

            private IScenario GetScenario(ScenarioType type)
            {
                var scenario = _scenarios.FirstOrDefault(s => s.CanHandle(type));
                if (scenario == null)
                    throw new InvalidOperationException($"Сценарий {type} не найден");
                return scenario;
            }

            // ---------------- Обработка сообщений ----------------

            private async Task OnMessage(Update update, Message message, CancellationToken ct)
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
                        "Ошибка: не удалось определить пользователя.",
                        cancellationToken: ct);
                    return;
                }

                // регистрация/обновление пользователя
                var user = await _userService.RegisterOrUpdateAsync(
                    telegramId,
                    firstName,
                    null,
                    message.Chat.Username);

                // 1. проверяем активный сценарий
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

            // 2. обычные команды
            var command = message.Text
                         .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                         .FirstOrDefault() ?? string.Empty;

            switch (command.ToLowerInvariant())
            {
                case "/start":
                    await StartCommand(chatId, user, ct);
                    break;

                case "/bmi":
                    await BmiInlineCommand(chatId, user, message.Text, ct);
                    break;

                case "/bmi_scenario":
                    await StartBmiScenario(user, message, ct);
                    break;

                case "/addcalories":
                    await ShowAddCaloriesMenuAsync(chatId, message.From.Id, ct);
                    break;

                case "/today":
                    await TodayCommand(chatId, user, ct); // user: DomainUser
                    break;

                case "/setmeals":
                    await StartMealTimeSetupAsync(chatId, user, ct);
                    break;

                case "/report":
                    await ReportCommand(chatId, user, ct);
                    break;

                case "/help":
                    await HelpCommand(chatId, ct);
                    break;


                default:
                    await _botClient.SendMessage(
                        chatId,
                        "Неизвестная команда. Используйте /help.",
                        cancellationToken: ct);
                    break;
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

        // ---------------- CallbackQuery ----------------

        private async Task OnCallbackQuery(Update update, CallbackQuery callbackQuery, CancellationToken ct)
        {
            try
            {
                var data = callbackQuery.Data ?? string.Empty;
                Console.WriteLine($"Callback received: {data}");

                // 1. Быстрые калории по кнопкам (100/200/300/500)
                if (data.StartsWith("meal_add_calories", StringComparison.OrdinalIgnoreCase))
                {
                    var dto = MealCaloriesCallbackDto.FromString(data);
                    var now = DateTime.UtcNow;

                    var user = await _userService.GetByTelegramIdAsync(dto.TelegramId);
                    if (user == null)
                    {
                        await _botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Пользователь не найден.",
                            cancellationToken: ct);
                        return;
                    }

                    var meal = new Meal
                    {
                        UserId = user.Id,
                        DateTime = now,
                        MealType = "snack",
                        Calories = dto.Calories
                    };

                    await _mealRepository.AddAsync(meal);

                    await _botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        $"Записал {dto.Calories} ккал в {now:HH:mm}",
                        cancellationToken: ct);

                    if (callbackQuery.Message != null)
                    {
                        await _botClient.EditMessageText(
                            callbackQuery.Message.Chat.Id,
                            callbackQuery.Message.MessageId,
                            $"Добавлено {dto.Calories} ккал ({now:dd.MM HH:mm}).",
                            cancellationToken: ct);
                    }

                    return;
                }

                // 2. Кнопка «Другое количество»
                if (data.StartsWith("meal_add_custom", StringComparison.OrdinalIgnoreCase))
                {
                    // data: meal_add_custom|<telegramId>
                    var parts = data.Split('|');
                    if (parts.Length < 2 || !long.TryParse(parts[1], out var telegramId))
                    {
                        await _botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Некорректные данные.",
                            cancellationToken: ct);
                        return;
                    }

                    var user = await _userService.GetByTelegramIdAsync(telegramId);
                    if (user == null)
                    {
                        await _botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Пользователь не найден.",
                            cancellationToken: ct);
                        return;
                    }

                    var context = new ScenarioContext
                    {
                        UserId = user.Id,
                        CurrentScenario = ScenarioType.CustomCalories,
                        CurrentStep = 0
                    };

                    await _contextRepository.SetContext(user.Id, context, ct);

                    await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

                    if (callbackQuery.Message != null)
                    {
                        await _botClient.SendMessage(
                            callbackQuery.Message.Chat.Id,
                            "Введите количество калорий числом, например: 350",
                            cancellationToken: ct);
                    }

                    return;
                }

                // 3. Дефолт для всех остальных callback'ов
                await _botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "Неизвестное действие.",
                    cancellationToken: ct);
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


        // ---------------- Команды ----------------

        private async Task StartCommand(long chatId, DomainUser user, CancellationToken ct)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
        new KeyboardButton[] { "/bmi 80 180" },
        new KeyboardButton[] { "/bmi_scenario" },
        new KeyboardButton[] { "/today" },
        new KeyboardButton[] { "/report" },
        new KeyboardButton[] { "/help" }
    })
            {
                ResizeKeyboard = true
            };

            await _botClient.SendMessage(
                chatId,
                $"Привет, {user.Name}! Я фитнес‑бот «Вес‑контроль».",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }

        private async Task HelpCommand(long chatId, CancellationToken ct)
            {
                await _botClient.SendMessage(
                    chatId,
                    "Доступные команды:\n" +
                    "/start — приветствие и меню\n" +
                    "/bmi вес рост — быстрый расчёт ИМТ (кг, см)\n" +
                    "/bmi_scenario — пошаговый расчёт ИМТ\n" +
                    "/today — калории и БЖУ за сегодня\n" +
                    "/setmeals - установить напоминания\n"+
                    "/report — краткий отчёт за сегодня\n" +
                    "/cancel — прервать текущий сценарий",
                    cancellationToken: ct);
            }

        // /bmi 80 180
        private async Task BmiInlineCommand(long chatId, DomainUser user, string text, CancellationToken ct)
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 ||
                !double.TryParse(parts[1], out var weight) ||
                !double.TryParse(parts[2], out var height))
            {
                await _botClient.SendMessage(
                    chatId,
                    "Формат команды: /bmi <вес_кг> <рост_см>\nНапример: /bmi 80 180",
                    cancellationToken: ct);
                return;
            }

            var record = await _bmiService.SaveMeasurementAsync(user.Id, height, weight);

            await _botClient.SendMessage(
                chatId,
                $"Ваш ИМТ: {record.Bmi:F1}, категория: {record.Category}.\n{record.Recommendation}",
                cancellationToken: ct);
        }

        // сценарий ИМТ (пошаговый)
        private async Task StartBmiScenario(DomainUser user, Message message, CancellationToken ct)
        {
            var context = new ScenarioContext
            {
                UserId = user.Id,
                CurrentScenario = ScenarioType.Bmi,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(user.Id, context, ct);

            var scenario = GetScenario(ScenarioType.Bmi);
            await scenario.HandleMessageAsync(_botClient, context, message, ct);
        }

        private async Task ShowAddCaloriesMenuAsync(long chatId, long userId, CancellationToken ct)
        {
            var buttons = new[]
            {
        new []
        {
            InlineKeyboardButton.WithCallbackData(
                "100 ккал",
                new MealCaloriesCallbackDto("meal_add_calories", userId, 100).ToString()),
            InlineKeyboardButton.WithCallbackData(
                "200 ккал",
                new MealCaloriesCallbackDto("meal_add_calories", userId, 200).ToString()),
        },
        new []
        {
            InlineKeyboardButton.WithCallbackData(
                "300 ккал",
                new MealCaloriesCallbackDto("meal_add_calories", userId, 300).ToString()),
            InlineKeyboardButton.WithCallbackData(
                "500 ккал",
                new MealCaloriesCallbackDto("meal_add_calories", userId, 500).ToString()),
        },
        new []
        {
            InlineKeyboardButton.WithCallbackData(
                "Другое количество",
                $"meal_add_custom|{userId}")
        }
    };

            var keyboard = new InlineKeyboardMarkup(buttons);

            await _botClient.SendMessage(
                chatId: chatId,
                text: "Сколько калорий вы сейчас съели?",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }


        private async Task TodayCommand(long chatId, DomainUser user, CancellationToken ct)
        {
            var userId = user.Id;              // ваш идентификатор пользователя
            var today = DateTime.UtcNow.Date;   // начало дня
            var tomorrow = today.AddDays(1);    // конец интервала

            var meals = await _mealRepository.GetByUserAndPeriodAsync(userId, today, tomorrow);
            var eatenCalories = meals.Sum(m => m.Calories);
            var eatenCount = meals.Count;

            var activities = await _activityRepository.GetByUserAndPeriodAsync(userId, today, tomorrow);
            var burnedCalories = activities.Sum(a => a.CaloriesBurned);
            var steps = activities.Sum(a => a.Steps);

            var netCalories = eatenCalories - burnedCalories;

            var text =
                $"Статистика за сегодня ({today:dd.MM.yyyy}):\n" +
                $"\n" +
                $"Съедено: {eatenCalories:F0} ккал ({eatenCount} приём(а) пищи)\n" +
                $"Потрачено: {burnedCalories:F0} ккал\n" +
                $"Шаги: {steps}\n" +
                $"\n" +
                $"Баланс: {netCalories:F0} ккал";

            await _botClient.SendMessage(
                chatId,
                text,
                cancellationToken: ct);
        }
        private async Task ReportCommand(long chatId, DomainUser user, CancellationToken ct)
        {
            var today = DateTime.UtcNow.Date;
            var summary = await _reportService.BuildDailySummaryAsync(user.Id, today);

            await _botClient.SendMessage(
                chatId,
                summary,
                cancellationToken: ct);
        }
        private async Task StartMealTimeSetupAsync(long chatId, DomainUser user, CancellationToken ct)
        {
            var context = new ScenarioContext
            {
                UserId = user.Id,
                CurrentScenario = ScenarioType.MealTimeSetup,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(user.Id, context, ct);

            await _botClient.SendMessage(
                chatId,
                "Введите время завтрака в формате HH:mm, например: 08:00",
                cancellationToken: ct);
        }

        // ---------------- Обработка сценариев ----------------

        private async Task ProcessScenario(ScenarioContext context, Message message, CancellationToken ct)
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
                    await _botClient.SendMessage(
                        message.Chat.Id,
                        "Для выхода из сценария используйте /cancel.",
                        cancellationToken: ct);
                }
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
                message = $"Telegram API Error [{apiEx.ErrorCode}] from {source}: {apiEx.Message}";
            }
            else
            {
                message = $"Unexpected error from {source}: {exception.Message}";
            }

            // Лог в консоль (минимум)
            Console.WriteLine(message);

            // При желании можно добавить логирование в БД/файл:
            // await _adminAnalyticsService.LogErrorAsync(message, exception.ToString());

            // Небольшая пауза, чтобы не крутить ошибки бесконечно часто при проблемах с сетью
            return Task.Delay(1000, cancellationToken);
        }
    }
}

    
