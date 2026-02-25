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

            var photo = message.Photo.OrderByDescending(p => p.FileSize).First();
            var file = await bot.GetFile(photo.FileId, cancellationToken: ct);
            var fileUrl = $"{_fileBaseUrl}{file.FilePath}";

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
                    "Не удалось распознать блюдо по фото 😔\n" +
                    "Попробуйте позже или введите калории вручную.",
                    cancellationToken: ct);
                return true;
            }

            if (segResult == null || segResult.ImageId == 0)
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
                    "Удалось распознать блюдо, но сервис не вернул калории и БЖУ.\n" +
                    "Вы можете добавить приём пищи вручную командой /addmeal.",
                    cancellationToken: ct);
                return true;
            }

            if (nutriResult == null)
            {
                await SendNoNutritionKeyboard(bot, chatId, ct);
                return true;
            }

            // Извлекаем данные с учётом новой структуры ответа
            var info = MapToSimple(nutriResult.Nutritional_Info);

            // Если Nutritional_Info пустой, пробуем взять из nutritional_info_per_item
            if (info.EnergyKcal <= 0 && nutriResult.Nutritional_Info_Per_Item?.Count > 0)
            {
                var firstItem = nutriResult.Nutritional_Info_Per_Item[0];
                info = MapToSimple(firstItem.Nutritional_Info);
            }

            // Если всё ещё 0, пробуем взять calories из корневого уровня
            if (info.EnergyKcal <= 0 && nutriResult.Calories.HasValue)
            {
                info.EnergyKcal = nutriResult.Calories.Value;
            }

            if (info.EnergyKcal <= 0)
            {
                await SendNoNutritionKeyboard(bot, chatId, ct);
                return true;
            }

            var serving = nutriResult.Serving_Size;
            if (serving <= 0)
            {
                // Пробуем взять serving_size из первого элемента nutritional_info_per_item
                if (nutriResult.Nutritional_Info_Per_Item?.Count > 0)
                {
                    serving = nutriResult.Nutritional_Info_Per_Item[0].Serving_Size;
                }
                else
                {
                    serving = 100;
                }
            }

            // Извлекаем название блюда из массива foodName
            var dishName = nutriResult.FoodName?.FirstOrDefault()
                        ?? segResult.Recognition_Results?.FirstOrDefault()?.Name
                        ?? "Неизвестное блюдо";

            // Создаём сценарий PhotoMealGrams
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
            scenarioContext.Data["dish_name"] = dishName;  

            await _contextRepository.SetContext(user.Id, scenarioContext, ct);

            // Показываем название блюда пользователю
            await bot.SendMessage(
                chatId,
                $"🍽️ *Распознано:* {dishName}\n\n" +
                $"По данным LogMeal это ~{serving:F0} г: {info.EnergyKcal:F0} ккал,  " +
                $"Б {info.Proteins:F1} г, Ж {info.Fats:F1} г, У {info.Carbs:F1} г.\n\n" +
                $"Сколько граммов ты съел? Введи число, например 120.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
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

        private static NutritionalInfo MapToSimple(NutritionalInfoRaw? raw)
        {
            if (raw?.TotalNutrients == null)
                return new NutritionalInfo();

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
