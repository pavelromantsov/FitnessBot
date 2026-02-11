using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.Core.Services.LogMeal;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public class FoodPhotoHandler : IPhotoHandler
    {
        private readonly LogMealClient logMealClient;
        private readonly NutritionService nutritionService;
        private readonly UserService userService;
        private readonly string fileBaseUrl;

        public FoodPhotoHandler(
            LogMealClient logMealClient,
            NutritionService nutritionService,
            UserService userService,
            string fileBaseUrl)
        {
            this.logMealClient = logMealClient;
            this.nutritionService = nutritionService;
            this.userService = userService;
            this.fileBaseUrl = fileBaseUrl;
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
            var fileUrl = $"{fileBaseUrl}{file.FilePath}";

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
                segResult = await logMealClient.AnalyzeSegmentationAsync(ms, "meal.jpg", ct);
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

            // 1) Пытаемся взять суммарные нутриенты
            var info = segResult.TotalNutritionalInfo;

            // 2) Если нет — берём по первому блюду
            if (info == null && segResult.Dishes != null && segResult.Dishes.Length > 0)
            {
                info = segResult.Dishes[0].NutritionalInfo;
            }

            if (info == null || info.EnergyKcal <= 0)
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

                return true;
            }

            var meal = new Meal
            {
                UserId = user.Id,
                DateTime = DateTime.UtcNow,
                MealType = "snack",
                Calories = info.EnergyKcal,
                Protein = info.Proteins,
                Fat = info.Fats,
                Carbs = info.Carbs,
                PhotoUrl = fileUrl
            };

            if (meal.Calories <= 0)
            {
                await bot.SendMessage(
                    chatId,
                    "Не удалось корректно определить калорийность блюда. " +
                    "Попробуйте ввести данные вручную.",
                    cancellationToken: ct);
                return true;
            }

            await nutritionService.AddMealAsync(meal, ct);

            var text =
                "🍽 Определён приём пищи по фото\n" +
                $"Калории: {info.EnergyKcal:F0}\n" +
                $"Б: {info.Proteins:F0} г, Ж: {info.Fats:F0} г, У: {info.Carbs:F0} г\n\n" +
                "Записал этот приём пищи в дневник.";

            await bot.SendMessage(chatId, text, cancellationToken: ct);
            return true;
        }
    }
}
