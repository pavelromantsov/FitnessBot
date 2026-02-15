using System.Globalization;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FitnessBot.Scenarios
{
    public class PhotoMealGramsScenario : IScenario
    {
        private readonly NutritionService _nutritionService;
        private readonly UserService _userService;

        public PhotoMealGramsScenario(
            NutritionService nutritionService,
            UserService userService)
        {
            _nutritionService = nutritionService;
            _userService = userService;
        }

        public ScenarioType ScenarioType => ScenarioType.PhotoMealGrams;

        public bool CanHandle(ScenarioType type) => type == ScenarioType.PhotoMealGrams;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            var chatId = message.Chat.Id;
            var text = message.Text?.Trim() ?? string.Empty;

            switch (context.CurrentStep)
            {
                // Шаг 0 — пропускаем
                // Шаг 1 — парсим граммы, считаем и сохраняем приём пищи
                case 1:
                    {
                        if (!double.TryParse(
                                text.Replace(",", "."),
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out var grams) || grams <= 0)
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не удалось прочитать количество граммов. Пример: 120.",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        // забираем данные, которые положил FoodPhotoHandler
                        if (!context.Data.TryGetValue("serving_size", out var servingObj) ||
                            !context.Data.TryGetValue("base_calories", out var calObj) ||
                            !context.Data.TryGetValue("base_protein", out var protObj) ||
                            !context.Data.TryGetValue("base_fat", out var fatObj) ||
                            !context.Data.TryGetValue("base_carbs", out var carbObj) ||
                            !context.Data.TryGetValue("photo_url", out var photoObj))
                        {
                            await bot.SendMessage(
                                chatId,
                                "Ошибка: не удалось получить данные о блюде. " +
                                "Попробуйте снова сделать фото.",
                                cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        var servingSize = Convert.ToDouble(servingObj, CultureInfo.InvariantCulture);
                        if (servingSize <= 0)
                            servingSize = 100;

                        var baseCalories = Convert.ToDouble(calObj, CultureInfo.InvariantCulture);
                        var baseProtein = Convert.ToDouble(protObj, CultureInfo.InvariantCulture);
                        var baseFat = Convert.ToDouble(fatObj, CultureInfo.InvariantCulture);
                        var baseCarbs = Convert.ToDouble(carbObj, CultureInfo.InvariantCulture);
                        var photoUrl = photoObj as string;

                        var ratio = grams / servingSize;

                        var calories = baseCalories * ratio;
                        var protein = baseProtein * ratio;
                        var fat = baseFat * ratio;
                        var carbs = baseCarbs * ratio;

                        var user = await _userService.GetByTelegramIdAsync(message.From!.Id);
                        if (user == null)
                        {
                            await bot.SendMessage(
                                chatId,
                                "Пользователь не найден.",
                                cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }

                        var meal = new Meal
                        {
                            UserId = user.Id,
                            DateTime = DateTime.UtcNow,
                            MealType = "snack",
                            Calories = calories,
                            Protein = protein,
                            Fat = fat,
                            Carbs = carbs,
                            PhotoUrl = photoUrl
                        };

                        await _nutritionService.AddMealAsync(meal, ct);

                        await bot.SendMessage(
                            chatId,
                            $"🍽 Записал приём пищи.\n" +
                            $"Вес: {grams:F0} г\n" +
                            $"Калории: {calories:F0}\n" +
                            $"Б: {protein:F0} г, Ж: {fat:F0} г, У: {carbs:F0} г",
                            cancellationToken: ct);

                        return ScenarioResult.Completed;
                    }

                default:
                    return ScenarioResult.Completed;
            }
        }
    }
}
