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

            // 3. ИСПРАВЛЕНИЕ: Раздельные репозитории
            var userRepo = new PgUserRepository(dataContextFactory);
            var notificationRepo = new PgNotificationRepository(dataContextFactory);
            var dailyGoalRepo = new PgDailyGoalRepository(dataContextFactory);  // ← НОВЫЙ
            var bmiRepo = new PgBmiRepository(dataContextFactory);               // ← НОВЫЙ
            var errorLogRepo = new PgErrorLogRepository(dataContextFactory);     // ← НОВЫЙ
            var changeLogRepo = new PgChangeLogRepository(dataContextFactory);   // ← НОВЫЙ
            var contentRepo = new PgContentItemRepository(dataContextFactory);   // ← НОВЫЙ
            var activityRepo = new PgActivityRepository(dataContextFactory);
            var nutritionRepo = new PgNutritionRepository(dataContextFactory);

            // 4. ИСПРАВЛЕНИЕ: NotificationService с правильными зависимостями
            var notificationService = new NotificationService(
                notificationRepo,
                dailyGoalRepo);  // ← ИСПРАВЛЕНО

            // 5. Сервисы
            var userService = new UserService(userRepo);
            var bmiService = new BmiService(bmiRepo);  // ← ИСПРАВЛЕНО
            var activityService = new ActivityService(activityRepo);
            var nutritionService = new NutritionService(nutritionRepo);
            var reportService = new ReportService(nutritionRepo, activityRepo, dailyGoalRepo);
            var adminStatsRepo = new PgAdminStatsRepository(dataContextFactory);
            var adminStatsService = new AdminStatsService(adminStatsRepo);
            var chartService = new ChartService();
            var chartDataService = new ChartDataService(nutritionRepo, activityRepo);
            var chartImageService = new ChartImageService();

            // Google Fit
            var httpClient = new HttpClient();
            var googleClientId = configuration["GoogleFit:ClientId"];
            var googleClientSecret = configuration["GoogleFit:ClientSecret"];
            var googleFitClient = new GoogleFitClient(httpClient, googleClientId, googleClientSecret);

            var contextRepository = new InMemoryScenarioContextRepository();

            // 6. Сценарии
            var scenarios = new IScenario[]
            {
            new BmiScenario(bmiService),
            new CustomCaloriesScenario(nutritionService, userService),
            new MealTimeSetupScenario(userService),
            new RegistrationScenario(userService, bmiService),
            new EditProfileScenario(userService, bmiService),
            new SetDailyGoalScenario(dailyGoalRepo),
            new ActivityReminderSettingsScenario(userService),
            new AddMealScenario(nutritionService),
            new ConnectGoogleFitScenario(userService),
            new EditProfileAgeScenario(userService),
            new EditProfileCityScenario(userService),
            new EditProfileHeightWeightScenario(bmiService)
            };

            // 7. Telegram bot
            var botClient = new TelegramBotClient(botToken);

            // 8. Command Handlers
            var commandHandlers = new ICommandHandler[]
            {
            new AdminCommandsHandler(
                userService,
                adminStatsService,
                errorLogRepo,
                contentRepo,
                changeLogRepo
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

            // 9. Callback Handlers
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

            // 10. ИСПРАВЛЕНИЕ: UpdateHandler с errorLogRepo
            var updateHandler = new UpdateHandler(
                botClient,
                userService,
                contextRepository,
                scenarios,
                commandHandlers,
                callbackHandlers,
                errorLogRepo);  // ← ИСПРАВЛЕНО

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            var cts = new CancellationTokenSource();

            // 11. Background Tasks
            using var backgroundRunner = new BackgroundTaskRunner();

            backgroundRunner.AddTask(new MealReminderBackgroundTask(
                userService,
                nutritionService,
                notificationService));

            backgroundRunner.AddTask(new DailyGoalCheckBackgroundTask(
                userService,
                activityRepo,
                nutritionRepo,
                dailyGoalRepo,
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
                dailyGoalRepo,
                notificationService));

            // 12. Graceful shutdown
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\nПолучен сигнал остановки. Завершение работы...");
            };

            try
            {
                backgroundRunner.StartTasks(cts.Token);

                botClient.StartReceiving(
                    updateHandler,
                    receiverOptions,
                    cts.Token);

                var me = await botClient.GetMe();
                Console.WriteLine($"Бот {me.FirstName} запущен. Нажмите Ctrl+C для остановки.");

                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Остановка бота...");
            }
            finally
            {
                await backgroundRunner.StopTasks(CancellationToken.None);
                Console.WriteLine("Бот остановлен.");
            }
        }
    }
}
