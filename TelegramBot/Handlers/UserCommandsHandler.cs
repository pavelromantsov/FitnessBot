using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.Scenarios;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class UserCommandsHandler : ICommandHandler
    {
        private readonly BmiService _bmiService;
        private readonly IMealRepository _mealRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly ReportService _reportService;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly List<IScenario> _scenarios;

        public UserCommandsHandler(
            BmiService bmiService,
            IMealRepository mealRepository,
            IActivityRepository activityRepository,
            ReportService reportService,
            IScenarioContextRepository contextRepository,
            IEnumerable<IScenario> scenarios)
        {
            _bmiService = bmiService;
            _mealRepository = mealRepository;
            _activityRepository = activityRepository;
            _reportService = reportService;
            _contextRepository = contextRepository;
            _scenarios = scenarios.ToList();
        }

        public async Task<bool> HandleAsync(UpdateContext context, string command, string[] args)
        {
            switch (command.ToLowerInvariant())
            {
                case "/start":
                    await StartCommand(context);
                    return true;

                case "/bmi":
                    await BmiInlineCommand(context, args);
                    return true;

                case "/bmi_scenario":
                    await StartBmiScenario(context);
                    return true;

                case "/addcalories":
                    await ShowAddCaloriesMenuAsync(context);
                    return true;

                case "/today":
                    await TodayCommand(context);
                    return true;

                case "/setgoal":
                    await StartSetDailyGoalScenario(context);
                    return true;

                case "/setmeals":
                    await StartMealTimeSetupAsync(context);
                    return true;

                case "/addmeal":
                    await StartAddMealScenario(context);
                    return true;

                case "/activity_reminders":
                    await StartActivityReminderSettingsScenario(context);
                    return true;

                case "/report":
                    await ReportCommand(context);
                    return true;

                case "/connectgooglefit":
                    await StartConnectGoogleFitScenario(context);
                    return true;

                case "/help":
                    await HelpCommand(context);
                    return true;

                case "/edit_profile":
                    await StartEditProfileScenario(context);
                    return true;

                case "/whoami":
                    await WhoAmICommand(context);
                    return true;

                default:
                    return false;
            }
        }

        private async Task StartCommand(UpdateContext ctx)
        {
            var rows = new List<List<KeyboardButton>>
            {
                // Основные команды
                new() { new KeyboardButton("/today"), new KeyboardButton("/report") },
                new() { new KeyboardButton("/bmi 80 180"), new KeyboardButton("/addcalories") },
                new() { new KeyboardButton("/addmeal"), new KeyboardButton("/setgoal") },
                
                // Настройки
                new() { new KeyboardButton("/setmeals"), new KeyboardButton("/activity_reminders") },
                new() { new KeyboardButton("/edit_profile"), new KeyboardButton("/whoami") },
                
                // Графики и интеграции
                new() { new KeyboardButton("/charts"), new KeyboardButton("/connectgooglefit") },
                new() { new KeyboardButton("/help") }
            };

            if (ctx.User.Role == UserRole.Admin)
            {
                rows.Add(new List<KeyboardButton>
                {
                    new KeyboardButton("/admin_users"),
                    new KeyboardButton("/admin_stats")
                });
            }

            var keyboard = new ReplyKeyboardMarkup(rows)
            {
                ResizeKeyboard = true
            };

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                $"👋 Привет, {ctx.User.Name}!\n\n" +
                "🏃 Основные команды:\n" +
                "• /today — статистика за сегодня\n" +
                "• /addcalories — быстрое добавление калорий\n" +
                "• /addmeal — подробное добавление приёма пищи\n\n" +
                "📊 Графики:\n" +
                "• /charts — выбор графиков\n\n" +
                "⚙️ Настройки:\n" +
                "• /setgoal — установить цель на день\n" +
                "• /activity_reminders — напоминания об активности\n\n" +
                "ℹ️ /help — справка по командам",
                replyMarkup: keyboard,
                cancellationToken: default);
        }

        private async Task HelpCommand(UpdateContext ctx)
        {
            var helpText =
                "📋 **Справка по командам FitnessBot**\n\n" +

                "🏃 **Основные команды:**\n" +
                "/start — главное меню\n" +
                "/today — статистика за сегодня\n" +
                "/report — краткий отчёт\n" +
                "/addcalories — быстро добавить калории\n" +
                "/addmeal — добавить приём пищи с БЖУ\n\n" +

                "📊 **Расчёты и ИМТ:**\n" +
                "/bmi <вес> <рост> — расчёт ИМТ (пример: /bmi 80 180)\n" +
                "/bmi_scenario — пошаговый расчёт ИМТ\n\n" +

                "🎯 **Цели и напоминания:**\n" +
                "/setgoal — установить цель на день\n" +
                "/setmeals — настроить время приёмов пищи\n" +
                "/activity_reminders — напоминания об активности\n\n" +

                "📈 **Графики и статистика:**\n" +
                "/charts — меню графиков\n" +
                "/chart_calories — график калорий\n" +
                "/chart_steps — график шагов\n" +
                "/chart_macros — график БЖУ\n\n" +

                "⚙️ **Настройки:**\n" +
                "/edit_profile — редактировать профиль\n" +
                "/whoami — информация о вашем аккаунте\n" +
                "/connectgooglefit — подключить Google Fit\n\n" +

                "❌ **Управление:**\n" +
                "/cancel — отменить текущий сценарий\n" +
                "/help — эта справка";

            if (ctx.User.Role == UserRole.Admin)
            {
                helpText += "\n\n👨‍💼 **Команды администратора:**\n" +
                           "/admin_users — список пользователей\n" +
                           "/admin_stats — статистика системы\n" +
                           "/admin_activity — активность пользователей\n" +
                           "/admin_find <имя> — поиск пользователя\n" +
                           "/make_admin <telegram_id> — назначить админа\n" +
                           "/make_user <telegram_id> — снять права админа";
            }

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                helpText,
                cancellationToken: default);
        }

        private async Task BmiInlineCommand(UpdateContext ctx, string[] args)
        {
            // Original: /bmi 80 180
            if (args.Length != 2 ||
                !double.TryParse(args[0], out var weight) ||
                !double.TryParse(args[1], out var height))
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "Формат команды: /bmi <вес_кг> <рост_см>\nНапример: /bmi 80 180",
                    cancellationToken: default);
                return;
            }

            var record = await _bmiService.SaveMeasurementAsync(ctx.User.Id, height, weight);

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                $"Ваш ИМТ: {record.Bmi:F1}, категория: {record.Category}.\n{record.Recommendation}",
                cancellationToken: default);
        }

        private async Task StartBmiScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.Bmi,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.Bmi);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }

        private async Task ShowAddCaloriesMenuAsync(UpdateContext ctx)
        {
            var buttons = new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(
                        "100 ккал",
                        $"meal_add_calories:{ctx.User.TelegramId}:100"),
                    InlineKeyboardButton.WithCallbackData(
                        "200 ккал",
                        $"meal_add_calories:{ctx.User.TelegramId}:200"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(
                        "300 ккал",
                        $"meal_add_calories:{ctx.User.TelegramId}:300"),
                    InlineKeyboardButton.WithCallbackData(
                        "500 ккал",
                        $"meal_add_calories:{ctx.User.TelegramId}:500"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(
                        "Другое количество",
                        $"meal_add_custom:{ctx.User.TelegramId}")
                }
            };

            var keyboard = new InlineKeyboardMarkup(buttons);

            await ctx.Bot.SendMessage(
                chatId: ctx.ChatId,
                text: "Сколько калорий вы сейчас съели?",
                replyMarkup: keyboard,
                cancellationToken: default);
        }

        private async Task TodayCommand(UpdateContext ctx)
        {
            var userId = ctx.User.Id;
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

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

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                text,
                cancellationToken: default);
        }

        private async Task ReportCommand(UpdateContext ctx)
        {
            var text = await _reportService.BuildDailySummaryAsync(ctx.User.Id, DateTime.UtcNow);
            await ctx.Bot.SendMessage(ctx.ChatId, text, cancellationToken: default);
        }

        private async Task StartMealTimeSetupAsync(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.MealTimeSetup,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                "Введите время завтрака в формате HH:mm, например: 08:00",
                cancellationToken: default);
        }

        private async Task StartAddMealScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.AddMeal,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.AddMeal);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }

        private async Task StartEditProfileScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.EditProfile,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.EditProfile);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }

        private async Task StartSetDailyGoalScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.SetDailyGoal,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.SetDailyGoal);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }

        private async Task StartActivityReminderSettingsScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.ActivityReminderSettings,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.ActivityReminderSettings);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }

        private async Task StartConnectGoogleFitScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.ConnectGoogleFit,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.ConnectGoogleFit);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }

        private async Task WhoAmICommand(UpdateContext ctx)
        {
            var text =
                "Текущая учетная запись:\n" +
                $"\n" +
                $"TelegramId: {ctx.User.TelegramId}\n" +
                $"Роль: {ctx.User.Role}";

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                text,
                cancellationToken: default);
        }

        private IScenario GetScenario(ScenarioType type)
        {
            var scenario = _scenarios.FirstOrDefault(s => s.CanHandle(type));
            if (scenario == null)
                throw new InvalidOperationException($"Сценарий {type} не найден");
            return scenario;
        }
    }
}