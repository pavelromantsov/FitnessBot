using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.Infrastructure.DataAccess;
using FitnessBot.Scenarios;
using FitnessBot.TelegramBot;
using FitnessBot.BackgroundTasks;
using FitnessBot.Infrastructure;

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

            var userService = new UserService(userRepo);
            var bmiService = new BmiService(notificationRepo);
            var activityService = new ActivityService(activityRepo);
            var nutritionService = new NutritionService(nutritionRepo);
            var reportService = new ReportService(activityService, nutritionService);

            var contextRepository = new InMemoryScenarioContextRepository();

            // сценарии
            var scenarios = new IScenario[]
            {
    new BmiScenario(bmiService),
    new CustomCaloriesScenario(nutritionService, userService),
    new MealTimeSetupScenario(userService),
            };

            // 5. Background Tasks
            var backgroundRunner = new BackgroundTaskRunner();
            backgroundRunner.AddTask(new MealReminderBackgroundTask(userService, nutritionService, notificationService));

            // 6. Telegram bot + UpdateHandler
            var botClient = new TelegramBotClient(botToken);
            var updateHandler = new UpdateHandler(
                botClient,
                userService,
                bmiService,
                activityService,
                nutritionService,
                reportService,
                contextRepository,
                scenarios,
                nutritionRepo,
                activityRepo);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            var cts = new CancellationTokenSource();

            // запускаем фоновые задачи
            backgroundRunner.StartTasks(cts.Token);

            // запускаем бота
            botClient.StartReceiving(
                updateHandler,
                receiverOptions,
                cts.Token);

            var me = await botClient.GetMe();
            Console.WriteLine($"Бот {me.FirstName} запущен. Нажмите Enter для остановки.");

            Console.ReadLine();
            cts.Cancel();

            // корректно останавливаем фоновые задачи
            await backgroundRunner.StopTasks(CancellationToken.None);

        }
    }
}