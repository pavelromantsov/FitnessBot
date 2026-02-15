using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.Core.Services.LogMeal;
using FitnessBot.Scenarios;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public class FoodPhotoHandler : IPhotoHandler
    {
        private readonly LogMealClient _logMealClient;
        private readonly NutritionService _nutritionService;
        private readonly UserService _userService;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly string _fileBaseUrl;

        public FoodPhotoHandler(
            LogMealClient logMealClient,
            NutritionService nutritionService,
            UserService userService,
            IScenarioContextRepository contextRepository,
            string fileBaseUrl)
        {
            _logMealClient = logMealClient;
            _nutritionService = nutritionService;
            _userService = userService;
            _contextRepository = contextRepository;
            _fileBaseUrl = fileBaseUrl;
        }

        public async Task<bool> HandleAsync(UpdateContext context)
        {
            var message = context.Message;
            if (message?.Photo is not { Length: > 0 })
                return false;

            var bot = context.Bot;
            var ct = context.CancellationToken;
            var chatId = context.ChatId;
            var user = context.User;

            // Самое большое фото
            var photo = message.Photo.OrderByDescending(p => p.FileSize).First();

            // Получаем файл на серверах Telegram
            var file = await bot.GetFile(photo.FileId, cancellationToken: ct);
            var fileUrl = $"{_fileBaseUrl}{file.FilePath}";

            // Скачиваем в память для LogMeal
            await using var ms = new MemoryStream();
            await bot.DownloadFile(file.FilePath!, ms, cancellationToken: ct);
            ms.Position = 0;

            await bot.SendMessage(
                chatId,
                "🔍 Анализирую фото, подождите пару секунд...",
                cancellationToken: ct);

            LogMealSegmentationResult? segResult;
            try
            {
                segResult = await _logMealClient.AnalyzeSegmentationAsync(ms, "meal.jpg", ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LogMeal segmentation error: {ex}");
                await bot.SendMessage(
                    chatId,
                    "Не удалось распознать блюдо по фото 😔 " +
                    "Попробуйте позже или введите калории вручную.",
                    cancellationToken: ct);
                return true;
            }

            if (segResult == null)
            {
                await bot.SendMessage(
                    chatId,
                    "Не удалось определить блюдо на фото.",
                    cancellationToken: ct);
                return true;
            }

            LogMealNutritionResult? nutriResult;
            try
            {
                nutriResult = await _logMealClient.GetNutritionAsync(segResult.ImageId, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LogMeal nutrition error: {ex}");
                await bot.SendMessage(
                    chatId,
                    "Удалось распознать блюдо, но сервис не вернул калории и БЖУ. " +
                    "Вы можете добавить приём пищи вручную командой /addmeal.",
                    cancellationToken: ct);
                return true;
            }

            if (nutriResult == null || !nutriResult.HasNutritionalInfo)
            {
                await SendNoNutritionKeyboard(bot, chatId, ct);
                return true;
            }

            var info = MapToSimple(nutriResult.Nutritional_Info);

            if (info.EnergyKcal <= 0)
            {
                await SendNoNutritionKeyboard(bot, chatId, ct);
                return true;
            }

            var serving = nutriResult.Serving_Size;
            if (serving <= 0)
                serving = 100; 

            // Создаём сценарий PhotoMealGrams и кладём базовые данные
            var scenarioContext = new ScenarioContext
            {
                UserId = user.Id,
                CurrentScenario = ScenarioType.PhotoMealGrams,
                CurrentStep = 1
            };

            scenarioContext.Data["serving_size"] = serving;
            scenarioContext.Data["base_calories"] = info.EnergyKcal;
            scenarioContext.Data["base_protein"] = info.Proteins;
            scenarioContext.Data["base_fat"] = info.Fats;
            scenarioContext.Data["base_carbs"] = info.Carbs;
            scenarioContext.Data["photo_url"] = fileUrl;

            await _contextRepository.SetContext(user.Id, scenarioContext, ct);

            await bot.SendMessage(
                chatId,
                $"Фото распознано.\n" +
                $"По данным LogMeal это ~{serving:F0} г: {info.EnergyKcal:F0} ккал, " +
                $"Б {info.Proteins:F1} г, Ж {info.Fats:F1} г, У {info.Carbs:F1} г.\n\n" +
                "Сколько граммов ты съел? Введи число, например 120.",
                cancellationToken: ct);

            return true;
        }

        private static async Task SendNoNutritionKeyboard(
            ITelegramBotClient bot,
            long chatId,
            CancellationToken ct)
        {
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("/addmeal"),
                    new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Отмена")
                }
            })
            {
                ResizeKeyboard = true
            };

            await bot.SendMessage(
                chatId,
                "Удалось распознать блюдо, но сервис не вернул калории и БЖУ.\n" +
                "Вы можете добавить приём пищи вручную командой /addmeal.",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }

        private static NutritionalInfo MapToSimple(NutritionalInfoRaw raw)
        {
            double Get(string code) =>
                raw.TotalNutrients.TryGetValue(code, out var v) ? v.Quantity : 0.0;

            return new NutritionalInfo
            {
                EnergyKcal = Get("ENERC_KCAL"),
                Proteins = Get("PROCNT"),
                Carbs = Get("CHOCDF"),
                Fats = Get("FAT")
            };
        }
    }
}
