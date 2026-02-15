using System.Globalization;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FitnessBot.Scenarios
{
    public class CustomCaloriesScenario : IScenario
    {
        private readonly NutritionService _nutritionService;
        private readonly UserService _userService;

        public CustomCaloriesScenario(NutritionService nutritionService, UserService userService)
        {
            _nutritionService = nutritionService;
            _userService = userService;
        }

        public ScenarioType ScenarioType => ScenarioType.CustomCalories;

        public bool CanHandle(ScenarioType type) => type == ScenarioType.CustomCalories;

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
                // Шаг 0 — запрос калорий
                case 0:
                    {
                        context.CurrentStep = 1;
                        return ScenarioResult.InProgress;
                    }

                // Шаг 1 — парсим калории и спрашиваем про БЖУ
                case 1:
                    {
                        if (!double.TryParse(
                                text.Replace(",", "."),
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out var calories) || calories <= 0)
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не удалось прочитать калории. Пример: 350.",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        context.Data["calories"] = calories;

                        var keyboard = new ReplyKeyboardMarkup(
                            new[]
                            {
            new KeyboardButton[] { new KeyboardButton("Да"), new KeyboardButton("Нет") }
                            })
                        {
                            ResizeKeyboard = true,
                            OneTimeKeyboard = true
                        };

                        await bot.SendMessage(
                            chatId,
                            "Хотите указать белки, жиры и углеводы?\nВыберите «Да» или «Нет» ниже.",
                            replyMarkup: keyboard,
                            cancellationToken: ct);

                        context.CurrentStep = 2;
                        return ScenarioResult.InProgress;
                    }


                // Шаг 2 — ответ «да/нет»
                case 2:
                    {
                        if (text.Equals("Да", StringComparison.OrdinalIgnoreCase))
                        {
                            // убираем клавиатуру
                            var remove = new ReplyKeyboardRemove();

                            await bot.SendMessage(
                                chatId,
                                "Введите количество белков (в граммах), например: 30.",
                                replyMarkup: remove,
                                cancellationToken: ct);

                            context.CurrentStep = 3;
                            return ScenarioResult.InProgress;
                        }

                        if (text.Equals("Нет", StringComparison.OrdinalIgnoreCase))
                        {
                            var remove = new ReplyKeyboardRemove();

                            // убираем клавиатуру и сразу сохраняем
                            await bot.SendMessage(
                                chatId,
                                "Ок, сохраняю только калории.",
                                replyMarkup: remove,
                                cancellationToken: ct);

                            return await SaveMealAndFinish(bot, context, message, ct,
                                protein: 0, fat: 0, carbs: 0);
                        }

                        await bot.SendMessage(
                            chatId,
                            "Пожалуйста, нажмите кнопку «Да» или «Нет».",
                            cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }


                // Шаг 3 — белки
                case 3:
                    {
                        if (!double.TryParse(
                                text.Replace(",", "."),
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out var protein) || protein < 0)
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не удалось прочитать белки. Пример: 30.",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        context.Data["protein"] = protein;

                        await bot.SendMessage(
                            chatId,
                            "Введите количество жиров (в граммах), например: 10.",
                            cancellationToken: ct);

                        context.CurrentStep = 4;
                        return ScenarioResult.InProgress;
                    }

                // Шаг 4 — жиры
                case 4:
                    {
                        if (!double.TryParse(
                                text.Replace(",", "."),
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out var fat) || fat < 0)
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не удалось прочитать жиры. Пример: 10.",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        context.Data["fat"] = fat;

                        await bot.SendMessage(
                            chatId,
                            "Введите количество углеводов (в граммах), например: 40.",
                            cancellationToken: ct);

                        context.CurrentStep = 5;
                        return ScenarioResult.InProgress;
                    }

                // Шаг 5 — углеводы и сохранение
                case 5:
                    {
                        if (!double.TryParse(
                                text.Replace(",", "."),
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out var carbs) || carbs < 0)
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не удалось прочитать углеводы. Пример: 40.",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        var protein = context.Data.TryGetValue("protein", out var pObj) && 
                            pObj is double pVal ? pVal : 0;
                        var fat = context.Data.TryGetValue("fat", out var fObj) && 
                            fObj is double fVal ? fVal : 0;

                        return await SaveMealAndFinish(bot, context, message, ct, 
                            protein, fat, carbs);
                    }

                default:
                    return ScenarioResult.Completed;
            }
        }

        private async Task<ScenarioResult> SaveMealAndFinish(
    ITelegramBotClient bot,
    ScenarioContext context,
    Message message,
    CancellationToken ct,
    double protein,
    double fat,
    double carbs)
        {
            var chatId = message.Chat.Id;

            if (!context.Data.TryGetValue("calories", out var calObj) ||
                calObj is not double calories)
            {
                await bot.SendMessage(
                    chatId,
                    "Ошибка: не удалось получить значение калорий.",
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            // Используем UserId из контекста, не ищем по message.From
            var meal = new Meal
            {
                UserId = context.UserId,
                DateTime = DateTime.UtcNow,
                MealType = "snack",
                Calories = calories,
                Protein = protein,
                Fat = fat,
                Carbs = carbs
            };

            await _nutritionService.AddMealAsync(meal, ct);

            if (protein == 0 && fat == 0 && carbs == 0)
            {
                await bot.SendMessage(
                    chatId,
                    $"Записал {calories:F0} ккал.",
                    cancellationToken: ct);
            }
            else
            {
                await bot.SendMessage(
                    chatId,
                    $"Записал {calories:F0} ккал, БЖУ {protein:F0}/{fat:F0}/{carbs:F0}.",
                    cancellationToken: ct);
            }

            return ScenarioResult.Completed;
        }
    }
}
