using FitnessBot.BackgroundTasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Services;
using FitnessBot.Infrastructure;
using FitnessBot.Infrastructure.DataAccess;
using FitnessBot.Scenarios;
using FitnessBot.TelegramBot;
using FitnessBot.TelegramBot.Handlers;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace FitnessBot
{
    public class Program
    {
        public static async Task Main()
        {
            // 1. Конфигурация
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            string botToken = configuration["Telegram_key"]
                ?? throw new InvalidOperationException("Telegram_key not found");

            string connectionString = configuration.GetConnectionString("FitnessBotDb")
                ?? throw new InvalidOperationException("Connection string not found");

            // 2. DataContext + фабрика
            var dataContextFactory = new Func<PgDataContext>(() => new PgDataContext(connectionString));

            // 3. Репозитории и сервисы
            var userRepo = new PgUserRepository(dataContextFactory);
            var notificationRepo = new PgNotificationRepository(dataContextFactory);
            var notificationService = new NotificationService(notificationRepo);

            var activityRepo = new PgActivityRepository(dataContextFactory);
            var nutritionRepo = new PgNutritionRepository(dataContextFactory);

            var errorLogRepo = new PgErrorLogRepository(dataContextFactory);
            var changeLogRepo = new PgChangeLogRepository(dataContextFactory);
            var contentRepo = new PgContentItemRepository(dataContextFactory);

            var userService = new UserService(userRepo);
            var bmiService = new BmiService(notificationRepo);
            var activityService = new ActivityService(activityRepo);
            var nutritionService = new NutritionService(nutritionRepo);
            var reportService = new ReportService(nutritionRepo, activityRepo, notificationRepo);
            var adminStatsRepo = new PgAdminStatsRepository(dataContextFactory);
            var adminStatsService = new AdminStatsService(adminStatsRepo);
            var chartService = new ChartService();
            var chartDataService = new ChartDataService(nutritionRepo, activityRepo);
            var chartImageService = new ChartImageService();
            var httpClient = new HttpClient();
            var googleClientId = configuration["GoogleFit:ClientId"];
            var googleClientSecret = configuration["GoogleFit:ClientSecret"];
            var googleFitClient = new GoogleFitClient(httpClient, googleClientId, googleClientSecret);

            var contextRepository = new InMemoryScenarioContextRepository();

            // сценарии
            var scenarios = new IScenario[]
            {
                new BmiScenario(bmiService),
                new CustomCaloriesScenario(nutritionService, userService),
                new MealTimeSetupScenario(userService),
                new RegistrationScenario(userService, bmiService),
                new EditProfileScenario(userService, bmiService),
                new SetDailyGoalScenario(notificationRepo),
                new ActivityReminderSettingsScenario(userService),
                new AddMealScenario(nutritionService),
                new ConnectGoogleFitScenario(userService),
                new EditProfileAgeScenario(userService),
                new EditProfileCityScenario(userService),
                new EditProfileHeightWeightScenario(bmiService)
            };

            // 4. Telegram bot
            var botClient = new TelegramBotClient(botToken);

            // 5. HANDLERS (в порядке приоритета!)
            // Command Handlers - ИСПРАВЛЕНО:
            var commandHandlers = new ICommandHandler[]
            {
                new AdminCommandsHandler(
                    userService,
                    adminStatsService,
                    errorLogRepo,      // IErrorLogRepository
                    contentRepo,       // IContentItemRepository
                    changeLogRepo      // IChangeLogRepository
                ),
                new ChartsCommandsHandler(chartService, chartDataService, chartImageService),
                new UserCommandsHandler(
                    bmiService,
                    nutritionRepo,
                    activityService,
                    reportService,
                    contextRepository,
                    scenarios)
            };

            // Callback Handlers
            var callbackHandlers = new ICallbackHandler[]
            {
                new AdminCallbackHandler(userService),
                new MealCallbackHandler(userService, nutritionRepo, contextRepository),
                new ActivityReminderCallbackHandler(userService),
                new ChartCallbackHandler(chartService, chartDataService, chartImageService),
                new ReportCallbackHandler(nutritionRepo, activityRepo, reportService),
                new ProfileCallbackHandler(userService, bmiService, contextRepository),
                new BmiCallbackHandler(contextRepository)
            };

            // 6. UpdateHandler с новой архитектурой
            var updateHandler = new UpdateHandler(
                botClient,
                userService,
                contextRepository,
                scenarios,
                commandHandlers,
                callbackHandlers);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            var cts = new CancellationTokenSource();

            // 7. Background Tasks - ДОБАВЛЕН using
            using var backgroundRunner = new BackgroundTaskRunner();

            backgroundRunner.AddTask(new MealReminderBackgroundTask(
                userService,
                nutritionService,
                notificationService));

            backgroundRunner.AddTask(new DailyGoalCheckBackgroundTask(
                userService,
                activityRepo,
                nutritionRepo,
                notificationRepo,
                notificationService));

            backgroundRunner.AddTask(new NotificationSenderBackgroundTask(
                botClient,
                notificationService,
                userService));

            backgroundRunner.AddTask(new GoogleFitSyncBackgroundTask(
                userService,
                activityRepo,
                googleFitClient));

            backgroundRunner.AddTask(new ActivityReminderBackgroundTask(
                userService,
                activityRepo,
                notificationRepo,
                notificationService));

            // Обработка Ctrl+C для graceful shutdown
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\nПолучен сигнал остановки. Завершение работы...");
            };

            try
            {
                // запускаем фоновые задачи
                backgroundRunner.StartTasks(cts.Token);

                // запускаем бота
                botClient.StartReceiving(
                    updateHandler,
                    receiverOptions,
                    cts.Token);

                var me = await botClient.GetMe();
                Console.WriteLine($"Бот {me.FirstName} запущен. Нажмите Ctrl+C для остановки.");

                // Ждём сигнала остановки
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Остановка бота...");
            }
            finally
            {
                // корректно останавливаем фоновые задачи
                await backgroundRunner.StopTasks(CancellationToken.None);
                Console.WriteLine("Бот остановлен.");
            }
        }
    }
}
