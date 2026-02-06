using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.Scenarios;
using FitnessBot.TelegramBot.DTO;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
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
            private readonly List<IScenario> _scenarios;

            public delegate void MessageEventHandler(string message);
            public event MessageEventHandler? OnHandleUpdateStarted;
            public event MessageEventHandler? OnHandleUpdateCompleted;
            private readonly IMealRepository _mealRepository;
            private readonly IActivityRepository _activityRepository;
            private readonly AdminStatsService _adminStatsService;

            private readonly ChartService _chartService;
            private readonly ChartDataService _chartDataService;
            private readonly ChartImageService _chartImageService;

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
            IActivityRepository activityRepository,
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
            _scenarios = scenarios.ToList();
            _mealRepository = mealRepository;
            _activityRepository = activityRepository;
            _adminStatsService = adminStatsService;
            _chartService = chartService;
            _chartDataService = chartDataService;
            _chartImageService = chartImageService;
            _chartImageService = chartImageService;
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
                await _botClient.SendMessage(chatId, "Не удалось определить твой TelegramId.", cancellationToken: ct);
                return;
            }

            // 1. Пытаемся найти пользователя
            var user = await _userService.GetByTelegramIdAsync(telegramId);
            if (user == null)
            {
                // создаём пользователя БЕЗ города и возраста
                user = await _userService.RegisterOrUpdateAsync(
                    telegramId,
                    firstName,
                    null,            // age
                    null);           // city

                // запускаем сценарий регистрации
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

            // 2. Для существующего пользователя можно обновить только имя / lastActivity, без города
            user = await _userService.RegisterOrUpdateAsync(
                telegramId,
                firstName,
                user.Age,
                user.City);

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
                    await TodayCommand(chatId, user, ct); 
                    break;

                case "/setgoal":
                    await StartSetDailyGoalScenario(user, message, ct);
                    break;

                case "/setmeals":
                    await StartMealTimeSetupAsync(chatId, user, ct);
                    break;

                case "/addmeal":
                    await StartAddMealScenario(user, message, ct);
                    break;

                case "/activity_reminders":
                    await StartActivityReminderSettingsScenario(user, message, ct);
                    break;

                case "/report":
                    await ReportCommand(chatId, user, ct);
                    break;

                case "/chart_calories":
                    await ChartCaloriesCommand(chatId, user, ct);
                    break;

                case "/chart_steps":
                    await ChartStepsCommand(chatId, user, ct);
                    break;

                case "/chart_macros":
                    await ChartMacrosCommand(chatId, user, ct);
                    break;

                case "/charts":
                    await ChartsMenuCommand(chatId, ct);
                    break;

                case "/connectgooglefit":
                    await StartConnectGoogleFitScenario(user, message, ct);
                    break;

                case "/help":
                    await HelpCommand(chatId, ct);
                    break;

                case "/edit_profile":
                    await StartEditProfileScenario(user, message, ct);
                    break;

                case "/admin_users":
                    if (!IsAdmin(user))
                    {
                        await _botClient.SendMessage(
                            chatId,
                            "Эта команда доступна только администратору.",
                            cancellationToken: ct);
                        break;
                    }

                    await AdminUsersCommand(chatId, ct);
                    break;

                case "/admin_user":
                    if (!IsAdmin(user))
                    {
                        await _botClient.SendMessage(
                            chatId,
                            "Эта команда доступна только администратору.",
                            cancellationToken: ct);
                        break;
                    }

                    await AdminUserDetailsCommand(chatId, message.Text, ct);
                    break;

                case "/make_admin":
                    await MakeAdminCommand(chatId, user, message.Text, ct);
                    break;

                case "/make_user":
                    await MakeUserCommand(chatId, user, message.Text, ct);
                    break;

                case "/admin_find":
                    await AdminFindUserCommand(chatId, user, message.Text, ct);
                    break;

                case "/admin_activity":
                    await AdminActivityCommand(chatId, user, ct);
                    break;

                case "/admin_stats":
                    await AdminStatsCommand(chatId, user, ct);
                    break;

                case "/whoami":
                    await WhoAmICommand(chatId, user, ct);
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
                    var parts = data.Split(':');
                    if (parts.Length != 2 || !long.TryParse(parts[1], out var telegramId))
                    {
                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Некорректные данные.", cancellationToken: ct);
                        return;
                    }

                    var user = await _userService.GetByTelegramIdAsync(telegramId);
                    if (user == null)
                    {
                        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Пользователь не найден.", cancellationToken: ct);
                        return;
                    }

                    var context = new ScenarioContext
                    {
                        UserId = user.Id,
                        CurrentScenario = ScenarioType.CustomCalories,
                        CurrentStep = 0   // <‑‑ ВАЖНО: здесь должен быть 0
                    };

                    await _contextRepository.SetContext(user.Id, context, ct);

                    await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

                    if (callbackQuery.Message != null)
                    {
                        await _botClient.SendMessage(
                            callbackQuery.Message.Chat.Id,
                            "Введите количество калорий.",
                            cancellationToken: ct);
                    }

                    return;
                }

                // 3. Назначение админа
                if (data.StartsWith("make_admin", StringComparison.OrdinalIgnoreCase))
                {
                    var dto = AdminUserCallbackDto.FromString(data);

                    var caller = await _userService.GetByTelegramIdAsync(callbackQuery.From.Id);
                    if (caller == null || !IsAdmin(caller))
                    {
                        await _botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Нет прав для назначения админов.",
                            cancellationToken: ct);
                        return;
                    }

                    var ok = await _userService.MakeAdminAsync(dto.TelegramId);
                    if (!ok)
                    {
                        await _botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Пользователь не найден.",
                            cancellationToken: ct);
                        return;
                    }

                    await _botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        $"Пользователь {dto.TelegramId} назначен администратором.",
                        cancellationToken: ct);

                    if (callbackQuery.Message != null)
                    {
                        await _botClient.EditMessageText(
                            callbackQuery.Message.Chat.Id,
                            callbackQuery.Message.MessageId,
                            $"Пользователь {dto.TelegramId} назначен администратором.",
                            cancellationToken: ct);
                    }

                    return;
                }

                // 4. Настройки напоминаний об активности
                if (data.StartsWith("activity_reminders_", StringComparison.OrdinalIgnoreCase))
                {
                    var user = await _userService.GetByTelegramIdAsync(callbackQuery.From.Id);
                    if (user == null)
                    {
                        await _botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Пользователь не найден.",
                            cancellationToken: ct);
                        return;
                    }

                    // Обработка разных типов кнопок
                    switch (data)
                    {
                        case "activity_reminders_all_on":
                            user.ActivityRemindersEnabled = true;
                            user.MorningReminderEnabled = true;
                            user.LunchReminderEnabled = true;
                            user.AfternoonReminderEnabled = true;
                            user.EveningReminderEnabled = true;
                            await _userService.SaveAsync(user);

                            await _botClient.AnswerCallbackQuery(
                                callbackQuery.Id,
                                "✅ Все напоминания включены!",
                                cancellationToken: ct);

                            if (callbackQuery.Message != null)
                            {
                                await _botClient.EditMessageText(
                                    callbackQuery.Message.Chat.Id,
                                    callbackQuery.Message.MessageId,
                                    "✅ Настройки сохранены!\n\n" +
                                    "Все напоминания об активности включены:\n" +
                                    "☀️ Утренние (9:00) - включены\n" +
                                    "🍽 Обеденные (13:00) - включены\n" +
                                    "🧘‍♂️ Дневные (16:00) - включены\n" +
                                    "🌆 Вечерние (19:00) - включены",
                                    cancellationToken: ct);
                            }
                            break;

                        case "activity_reminders_all_off":
                            user.ActivityRemindersEnabled = false;
                            user.MorningReminderEnabled = false;
                            user.LunchReminderEnabled = false;
                            user.AfternoonReminderEnabled = false;
                            user.EveningReminderEnabled = false;
                            await _userService.SaveAsync(user);

                            await _botClient.AnswerCallbackQuery(
                                callbackQuery.Id,
                                "❌ Все напоминания отключены!",
                                cancellationToken: ct);

                            if (callbackQuery.Message != null)
                            {
                                await _botClient.EditMessageText(
                                    callbackQuery.Message.Chat.Id,
                                    callbackQuery.Message.MessageId,
                                    "❌ Настройки сохранены!\n\n" +
                                    "Все напоминания об активности отключены.\n" +
                                    "Вы можете включить их снова командой /activity_reminders",
                                    cancellationToken: ct);
                            }
                            break;

                        case "activity_reminders_morning":
                            user.MorningReminderEnabled = !user.MorningReminderEnabled;
                            await _userService.SaveAsync(user);

                            await _botClient.AnswerCallbackQuery(
                                callbackQuery.Id,
                                user.MorningReminderEnabled
                                    ? "✅ Утренние напоминания включены!"
                                    : "❌ Утренние напоминания отключены!",
                                cancellationToken: ct);

                            if (callbackQuery.Message != null)
                            {
                                await UpdateActivityReminderMenu(
                                    callbackQuery.Message.Chat.Id,
                                    callbackQuery.Message.MessageId,
                                    user,
                                    ct);
                            }
                            break;

                        case "activity_reminders_lunch":
                            user.LunchReminderEnabled = !user.LunchReminderEnabled;
                            await _userService.SaveAsync(user);

                            await _botClient.AnswerCallbackQuery(
                                callbackQuery.Id,
                                user.LunchReminderEnabled
                                    ? "✅ Обеденные напоминания включены!"
                                    : "❌ Обеденные напоминания отключены!",
                                cancellationToken: ct);

                            if (callbackQuery.Message != null)
                            {
                                await UpdateActivityReminderMenu(
                                    callbackQuery.Message.Chat.Id,
                                    callbackQuery.Message.MessageId,
                                    user,
                                    ct);
                            }
                            break;

                        case "activity_reminders_afternoon":
                            user.AfternoonReminderEnabled = !user.AfternoonReminderEnabled;
                            await _userService.SaveAsync(user);

                            await _botClient.AnswerCallbackQuery(
                                callbackQuery.Id,
                                user.AfternoonReminderEnabled
                                    ? "✅ Дневные напоминания включены!"
                                    : "❌ Дневные напоминания отключены!",
                                cancellationToken: ct);

                            if (callbackQuery.Message != null)
                            {
                                await UpdateActivityReminderMenu(
                                    callbackQuery.Message.Chat.Id,
                                    callbackQuery.Message.MessageId,
                                    user,
                                    ct);
                            }
                            break;

                        case "activity_reminders_evening":
                            user.EveningReminderEnabled = !user.EveningReminderEnabled;
                            await _userService.SaveAsync(user);

                            await _botClient.AnswerCallbackQuery(
                                callbackQuery.Id,
                                user.EveningReminderEnabled
                                    ? "✅ Вечерние напоминания включены!"
                                    : "❌ Вечерние напоминания отключены!",
                                cancellationToken: ct);

                            if (callbackQuery.Message != null)
                            {
                                await UpdateActivityReminderMenu(
                                    callbackQuery.Message.Chat.Id,
                                    callbackQuery.Message.MessageId,
                                    user,
                                    ct);
                            }
                            break;
                    }

                    return;
                }
                // 5. Обработка графиков
                if (data.StartsWith("chart_", StringComparison.OrdinalIgnoreCase))
                {
                    var user = await _userService.GetByTelegramIdAsync(callbackQuery.From.Id);
                    if (user == null)
                    {
                        await _botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Пользователь не найден.",
                            cancellationToken: ct);
                        return;
                    }

                    await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

                    if (callbackQuery.Message != null)
                    {
                        var chatId = callbackQuery.Message.Chat.Id;

                        switch (data)
                        {
                            case "chart_cal_7":
                                await ChartCaloriesCommand(chatId, user, 7, ct);
                                break;
                            case "chart_cal_14":
                                await ChartCaloriesCommand(chatId, user, 14, ct);
                                break;
                            case "chart_steps_7":
                                await ChartStepsCommand(chatId, user, 7, ct);
                                break;
                            case "chart_steps_14":
                                await ChartStepsCommand(chatId, user, 14, ct);
                                break;
                            case "chart_macros_7":
                                await ChartMacrosCommand(chatId, user, 7, ct);
                                break;
                            case "chart_macros_14":
                                await ChartMacrosCommand(chatId, user, 14, ct);
                                break;
                        }
                    }

                    return;
                }


                // 6. Дефолт для всех остальных callback'ов
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
        new KeyboardButton[] { "/addcalories" },
        new KeyboardButton[] { "/setgoal" },
        new KeyboardButton[] { "/setmeals" },
        new KeyboardButton[] { "/addmeal" },
        new KeyboardButton[] { "/activity_reminders" },
        new KeyboardButton[] { "/edit_profile" },
        new KeyboardButton[] { "/report" },
        new KeyboardButton[] { "/chart_calories" },
        new KeyboardButton[] { "/chart_steps" },
        new KeyboardButton[] { "/chart_macros" }, 
        new KeyboardButton[] { "/charts" },
        new KeyboardButton[] { "/connectgooglefit" },
        new KeyboardButton[] { "/whoami" },
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
                    "/setgoal — установить ежедневную цель 🎯\n"+
                    "/activity_reminders — настроить напоминания об активности 🏃\n" +
                    "/report — краткий отчёт за сегодня\n" +
                    "/charts — графики и статистика 📊\n" +
                    "/cancel — прервать текущий сценарий",
                    cancellationToken: ct);
            }
        // ---------------- Пользовательские команды ----------------
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
            var text = await _reportService.BuildDailySummaryAsync(user.Id, DateTime.UtcNow);
            await _botClient.SendMessage(chatId, text, cancellationToken: ct);
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
        private async Task StartAddMealScenario(DomainUser user, Message message, CancellationToken ct)
        {
            var context = new ScenarioContext
            {
                UserId = user.Id,
                CurrentScenario = ScenarioType.AddMeal,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(user.Id, context, ct);

            var scenario = GetScenario(ScenarioType.AddMeal);
            await scenario.HandleMessageAsync(_botClient, context, message, ct);
        }
        private async Task StartEditProfileScenario(DomainUser user, Message message, CancellationToken ct)
        {
            var context = new ScenarioContext
            {
                UserId = user.Id,
                CurrentScenario = ScenarioType.EditProfile,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(user.Id, context, ct);

            var scenario = GetScenario(ScenarioType.EditProfile);
            await scenario.HandleMessageAsync(_botClient, context, message, ct);
        }
        private async Task StartSetDailyGoalScenario(DomainUser user, Message message, CancellationToken ct)
        {
            var context = new ScenarioContext
            {
                UserId = user.Id,
                CurrentScenario = ScenarioType.SetDailyGoal,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(user.Id, context, ct);

            var scenario = GetScenario(ScenarioType.SetDailyGoal);
            await scenario.HandleMessageAsync(_botClient, context, message, ct);
        }
        private async Task StartActivityReminderSettingsScenario(DomainUser user, Message message, CancellationToken ct)
        {
            var context = new ScenarioContext
            {
                UserId = user.Id,
                CurrentScenario = ScenarioType.ActivityReminderSettings,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(user.Id, context, ct);

            var scenario = GetScenario(ScenarioType.ActivityReminderSettings);
            await scenario.HandleMessageAsync(_botClient, context, message, ct);
        }
        private async Task UpdateActivityReminderMenu(
    long chatId,
    int messageId,
    DomainUser user,
    CancellationToken ct)
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

            await _botClient.EditMessageText(
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
                cancellationToken: ct);
        }
        //----------------Графики---------------------
        // Перегрузка без указания дней (по умолчанию 7)
        private async Task ChartCaloriesCommand(long chatId, DomainUser user, CancellationToken ct)
        {
            await ChartCaloriesCommand(chatId, user, 7, ct);
        }

        // Основной метод с указанием дней
        private async Task ChartCaloriesCommand(long chatId, DomainUser user, int days, CancellationToken ct)
        {
            try
            {
                await _botClient.SendMessage(
                    chatId,
                    "⏳ Генерирую график калорий...",
                    cancellationToken: ct);

                var (caloriesIn, caloriesOut) = await _chartDataService.GetCaloriesDataAsync(user.Id, days);

                // Проверка на пустые данные
                if (!caloriesIn.Any() && !caloriesOut.Any())
                {
                    await _botClient.SendMessage(
                        chatId,
                        "📊 Недостаточно данных для построения графика.\n" +
                        "Добавьте записи о питании и активности.",
                        cancellationToken: ct);
                    return;
                }

                var chartUrl = _chartService.GenerateCaloriesChartUrl(
                    caloriesIn,
                    caloriesOut,
                    $"Калории за последние {days} дней");

                Console.WriteLine($"Chart URL: {chartUrl}");

                // Скачиваем изображение
                using var imageStream = await _chartImageService.DownloadChartImageAsync(chartUrl);

                await _botClient.SendPhoto(
                    chatId,
                    InputFile.FromStream(imageStream, "chart.png"),
                    caption: $"📊 График калорий за последние {days} дней\n\n" +
                             $"🔴 Красная линия - потреблено\n" +
                             $"🔵 Синяя линия - потрачено\n\n" +
                             $"Средние значения:\n" +
                             $"• Потребление: {caloriesIn.Values.Average():F0} ккал/день\n" +
                             $"• Расход: {caloriesOut.Values.Average():F0} ккал/день",
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации графика калорий: {ex}");
                await _botClient.SendMessage(
                    chatId,
                    "❌ Ошибка при генерации графика. Попробуйте позже.",
                    cancellationToken: ct);
            }
        }

        // График шагов
        private async Task ChartStepsCommand(long chatId, DomainUser user, CancellationToken ct)
        {
            await ChartStepsCommand(chatId, user, 7, ct);
        }

        private async Task ChartStepsCommand(long chatId, DomainUser user, int days, CancellationToken ct)
        {
            try
            {
                await _botClient.SendMessage(
                    chatId,
                    "⏳ Генерирую график шагов...",
                    cancellationToken: ct);

                var stepsData = await _chartDataService.GetStepsDataAsync(user.Id, days);

                if (!stepsData.Any() || stepsData.Values.All(v => v == 0))
                {
                    await _botClient.SendMessage(
                        chatId,
                        "👣 Недостаточно данных для построения графика шагов.\n" +
                        "Добавьте записи об активности.",
                        cancellationToken: ct);
                    return;
                }

                var chartUrl = _chartService.GenerateStepsChartUrl(
                    stepsData,
                    10000,
                    $"Шаги за последние {days} дней");

                Console.WriteLine($"Chart URL: {chartUrl}");

                using var imageStream = await _chartImageService.DownloadChartImageAsync(chartUrl);

                await _botClient.SendPhoto(
                    chatId,
                    InputFile.FromStream(imageStream, "chart.png"),
                    caption: $"👣 График шагов за последние {days} дней\n\n" +
                             $"Среднее: {stepsData.Values.Average():F0} шагов/день\n" +
                             $"Максимум: {stepsData.Values.Max()} шагов\n" +
                             $"Всего: {stepsData.Values.Sum()} шагов",
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации графика шагов: {ex}");
                await _botClient.SendMessage(
                    chatId,
                    "❌ Ошибка при генерации графика. Попробуйте позже.",
                    cancellationToken: ct);
            }
        }

        // График БЖУ
        private async Task ChartMacrosCommand(long chatId, DomainUser user, CancellationToken ct)
        {
            await ChartMacrosCommand(chatId, user, 7, ct);
        }

        private async Task ChartMacrosCommand(long chatId, DomainUser user, int days, CancellationToken ct)
        {
            try
            {
                await _botClient.SendMessage(
                    chatId,
                    "⏳ Генерирую график БЖУ...",
                    cancellationToken: ct);

                var macrosData = await _chartDataService.GetMacrosDataAsync(user.Id, days);

                if (!macrosData.Any() || macrosData.Values.All(m => m.protein == 0 && m.fat == 0 && m.carbs == 0))
                {
                    await _botClient.SendMessage(
                        chatId,
                        "🍖 Недостаточно данных для построения графика БЖУ.\n" +
                        "Добавьте записи о питании с указанием БЖУ.",
                        cancellationToken: ct);
                    return;
                }

                var chartUrl = _chartService.GenerateMacrosChartUrl(
                    macrosData,
                    $"Баланс БЖУ за последние {days} дней");

                Console.WriteLine($"Chart URL: {chartUrl}");

                using var imageStream = await _chartImageService.DownloadChartImageAsync(chartUrl);

                var avgProtein = macrosData.Values.Average(m => m.protein);
                var avgFat = macrosData.Values.Average(m => m.fat);
                var avgCarbs = macrosData.Values.Average(m => m.carbs);

                await _botClient.SendPhoto(
                    chatId,
                    InputFile.FromStream(imageStream, "chart.png"),
                    caption: $"🍖 Баланс БЖУ за последние {days} дней\n\n" +
                             $"Среднее в день:\n" +
                             $"• Белки: {avgProtein:F0} г\n" +
                             $"• Жиры: {avgFat:F0} г\n" +
                             $"• Углеводы: {avgCarbs:F0} г",
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации графика БЖУ: {ex}");
                await _botClient.SendMessage(
                    chatId,
                    "❌ Ошибка при генерации графика. Попробуйте позже.",
                    cancellationToken: ct);
            }
        }

        private async Task ChartsMenuCommand(long chatId, CancellationToken ct)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("📊 Калории (7 дней)", "chart_cal_7"),
            InlineKeyboardButton.WithCallbackData("📊 Калории (14 дней)", "chart_cal_14")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("👣 Шаги (7 дней)", "chart_steps_7"),
            InlineKeyboardButton.WithCallbackData("👣 Шаги (14 дней)", "chart_steps_14")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🍖 БЖУ (7 дней)", "chart_macros_7"),
            InlineKeyboardButton.WithCallbackData("🍖 БЖУ (14 дней)", "chart_macros_14")
        }
    });

            await _botClient.SendMessage(
                chatId,
                "📈 Выберите тип графика:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }

        private async Task StartConnectGoogleFitScenario(DomainUser user, Message message, CancellationToken ct)
        {
            var context = new ScenarioContext
            {
                UserId = user.Id,
                CurrentScenario = ScenarioType.ConnectGoogleFit,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(user.Id, context, ct);

            var scenario = GetScenario(ScenarioType.ConnectGoogleFit);
            await scenario.HandleMessageAsync(_botClient, context, message, ct);
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
        // ---------------- Администратор ----------------
        private static bool IsAdmin(DomainUser user) =>
                 user.Role == UserRole.Admin;

        private async Task AdminUsersCommand(long chatId, CancellationToken ct)
        {
            var users = await _userService.GetAllAsync();

            if (users.Count == 0)
            {
                await _botClient.SendMessage(
                    chatId,
                    "Пользователей пока нет.",
                    cancellationToken: ct);
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
            await _botClient.SendMessage(
                chatId,
                text,
                cancellationToken: ct);
        }

        private async Task AdminUserDetailsCommand(long chatId, string commandText, CancellationToken ct)
        {
            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !long.TryParse(parts[1], out var telegramId))
            {
                await _botClient.SendMessage(
                    chatId,
                    "Формат: /admin_user <telegramId>",
                    cancellationToken: ct);
                return;
            }

            var user = await _userService.GetByTelegramIdAsync(telegramId);
            if (user == null)
            {
                await _botClient.SendMessage(
                    chatId,
                    $"Пользователь с TelegramId={telegramId} не найден.",
                    cancellationToken: ct);
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

            await _botClient.SendMessage(
                chatId,
                text,
                cancellationToken: ct);
        }
        
        private async Task MakeAdminCommand(long chatId, DomainUser currentUser, string commandText, CancellationToken ct)
        {
            if (!IsAdmin(currentUser))
            {
                await _botClient.SendMessage(
                    chatId,
                    "Эта команда доступна только администратору.",
                    cancellationToken: ct);
                return;
            }

            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !long.TryParse(parts[1], out var targetTelegramId))
            {
                await _botClient.SendMessage(
                    chatId,
                    "Формат: /make_admin <telegramId>",
                    cancellationToken: ct);
                return;
            }

            var ok = await _userService.MakeAdminAsync(targetTelegramId);
            if (!ok)
            {
                await _botClient.SendMessage(
                    chatId,
                    $"Пользователь с TelegramId={targetTelegramId} не найден.",
                    cancellationToken: ct);
                return;
            }

            await _botClient.SendMessage(
                chatId,
                $"Пользователь с TelegramId={targetTelegramId} назначен администратором.",
                cancellationToken: ct);
        }

        private async Task MakeUserCommand(long chatId, DomainUser currentUser, string commandText, CancellationToken ct)
        {
            if (!IsAdmin(currentUser))
            {
                await _botClient.SendMessage(
                    chatId,
                    "Эта команда доступна только администратору.",
                    cancellationToken: ct);
                return;
            }

            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !long.TryParse(parts[1], out var targetTelegramId))
            {
                await _botClient.SendMessage(
                    chatId,
                    "Формат: /make_user <telegramId>",
                    cancellationToken: ct);
                return;
            }

            var ok = await _userService.MakeUserAsync(targetTelegramId);
            if (!ok)
            {
                await _botClient.SendMessage(
                    chatId,
                    $"Пользователь с TelegramId={targetTelegramId} не найден.",
                    cancellationToken: ct);
                return;
            }

            await _botClient.SendMessage(
                chatId,
                $"Пользователь с TelegramId={targetTelegramId} теперь обычный пользователь.",
                cancellationToken: ct);
        }

        private async Task AdminFindUserCommand(long chatId, DomainUser currentUser, string commandText, CancellationToken ct)
        {
            if (!IsAdmin(currentUser))
            {
                await _botClient.SendMessage(
                    chatId,
                    "Эта команда доступна только администратору.",
                    cancellationToken: ct);
                return;
            }

            var parts = commandText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                await _botClient.SendMessage(
                    chatId,
                    "Формат: /admin_find <часть имени>",
                    cancellationToken: ct);
                return;
            }

            var namePart = parts[1];

            var users = await _userService.FindByNameAsync(namePart);
            if (users.Count == 0)
            {
                await _botClient.SendMessage(
                    chatId,
                    $"Пользователи с именем, содержащим \"{namePart}\", не найдены.",
                    cancellationToken: ct);
                return;
            }

            var rows = users.Select(u =>
                new[]
                {
            InlineKeyboardButton.WithCallbackData(
                $"{u.Name} (TgId={u.TelegramId}, роль={u.Role})",
                new AdminUserCallbackDto("make_admin", u.TelegramId).ToString())
                }
            ).ToArray();

            var keyboard = new InlineKeyboardMarkup(rows);

            await _botClient.SendMessage(
                chatId,
                "Выберите пользователя, которого хотите сделать администратором:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
        private async Task AdminActivityCommand(long chatId, DomainUser currentUser, CancellationToken ct)
        {
            if (!IsAdmin(currentUser))
            {
                await _botClient.SendMessage(
                    chatId,
                    "Эта команда доступна только администратору.",
                    cancellationToken: ct);
                return;
            }

            var users = await _userService.GetAllAsync();
            if (users.Count == 0)
            {
                await _botClient.SendMessage(
                    chatId,
                    "Пользователей пока нет.",
                    cancellationToken: ct);
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

            await _botClient.SendMessage(
                chatId,
                text,
                cancellationToken: ct);
        }
        private async Task AdminStatsCommand(long chatId, DomainUser currentUser, CancellationToken ct)
        {
            if (!IsAdmin(currentUser))
            {
                await _botClient.SendMessage(
                    chatId,
                    "Эта команда доступна только администратору.",
                    cancellationToken: ct);
                return;
            }

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

            await _botClient.SendMessage(
                chatId,
                text,
                cancellationToken: ct);
        }

        private async Task WhoAmICommand(long chatId, DomainUser currentUser, CancellationToken ct)
        {
            var text =
                "Текущая учетная запись:\n" +
                $"\n" +
                $"TelegramId: {currentUser.TelegramId}\n" +
                $"Роль: {currentUser.Role}";

            await _botClient.SendMessage(
                chatId,
                text,
                cancellationToken: ct);
        }



        // ---------------- Обработчик ошибок ----------------
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

    
