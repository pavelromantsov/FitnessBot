using System.Globalization;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FitnessBot.Scenarios
{
    public class AddMealScenario : IScenario
    {
        private readonly NutritionService nutritionService;

        public AddMealScenario(NutritionService nutritionService)
        {
            this.nutritionService = nutritionService;
        }

        public ScenarioType ScenarioType => ScenarioType.AddMeal;

        public bool CanHandle(ScenarioType type) => type == ScenarioType.AddMeal;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            var chatId = message.Chat.Id;
            var text = message.Text ?? string.Empty;

            switch (context.CurrentStep)
            {
                case 0:
                    {
                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                new KeyboardButton("Готовое блюдо (БЖУ на порцию)"),
                                new KeyboardButton("По 100 г продукта")
                            },
                            new[]
                            {
                                new KeyboardButton("Отмена")
                            }
                        })
                        {
                            ResizeKeyboard = true
                        };

                        await bot.SendMessage(
                            chatId,
                            "Что вы хотите добавить?\n" +
                            "1) Готовое блюдо с известными калориями и БЖУ.\n" +
                            "2) Продукт по значению на 100 г.",
                            replyMarkup: keyboard,
                            cancellationToken: ct);

                        context.CurrentStep = 1;
                        return ScenarioResult.InProgress;
                    }

                // Шаг 1 — выбор режима -> сохраняем в Data["mode"]
                case 1:
                    {
                        var mode = text.Trim().ToLowerInvariant();

                        if (mode.StartsWith("готовое"))
                        {
                            context.Data["mode"] = "manual";
                        }
                        else if (mode.StartsWith("по 100"))
                        {
                            context.Data["mode"] = "by100g";
                        }
                        else if (mode.StartsWith("отмена") || mode.Equals("cancel", 
                            StringComparison.OrdinalIgnoreCase))
                        {
                            await bot.SendMessage(chatId, "Добавление блюда отменено.", cancellationToken: ct);
                            return ScenarioResult.Completed;
                        }
                        else
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не понял режим. Выберите вариант с клавиатуры.",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        // спросим тип приёма пищи
                        await bot.SendMessage(
                            chatId,
                            "Выберите тип приёма пищи:",
                            replyMarkup: new ReplyKeyboardMarkup(new[]
                            {
                                new[] { new KeyboardButton("Завтрак"), new KeyboardButton("Обед") },
                                new[] { new KeyboardButton("Ужин"), new KeyboardButton("Перекус") }
                            })
                            {
                                ResizeKeyboard = true
                            },
                            cancellationToken: ct);

                        context.CurrentStep = 2;
                        return ScenarioResult.InProgress;
                    }

                // Шаг 2 — тип приёма пищи
                case 2:
                    {
                        var mealTypeInput = text.Trim().ToLowerInvariant();
                        string mealType;

                        if (mealTypeInput.StartsWith("завтрак"))
                            mealType = "breakfast";
                        else if (mealTypeInput.StartsWith("обед"))
                            mealType = "lunch";
                        else if (mealTypeInput.StartsWith("ужин"))
                            mealType = "dinner";
                        else if (mealTypeInput.StartsWith("перекус"))
                            mealType = "snack";
                        else
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не понял тип приёма. Выберите: Завтрак, Обед, Ужин или Перекус.",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        context.Data["mealType"] = mealType;

                        var mode = (string)context.Data["mode"];

                        if (mode == "manual")
                        {
                            await bot.SendMessage(
                                chatId,
                                "Опишите блюдо (например: курица с рисом).",
                                cancellationToken: ct);
                            context.CurrentStep = 3;
                        }
                        else if (mode == "by100g")
                        {
                            await bot.SendMessage(
                                chatId,
                                "Введите продукт и его БЖУ на 100 г.\n" +
                                "Формат:\n" +
                                "`название; калории; белки; жиры; углеводы`\n" +
                                "Пример:\n" +
                                "гречка варёная; 110; 4; 1.8; 21",
                                cancellationToken: ct);
                            context.CurrentStep = 10;
                        }

                        return ScenarioResult.InProgress;
                    }

                // ----- РЕЖИМ 1: Готовое блюдо (manual) -----

                // Шаг 3 — название блюда
                case 3:
                    {
                        context.Data["title"] = text.Trim();
                        await bot.SendMessage(
                            chatId,
                            "Сколько калорий в порции? (только число, ккал)",
                            cancellationToken: ct);
                        context.CurrentStep = 4;
                        return ScenarioResult.InProgress;
                    }

                // Шаг 4 — калории
                case 4:
                    {
                        if (!double.TryParse(text.Replace(",", "."), NumberStyles.Any, 
                            CultureInfo.InvariantCulture, out var calories)
                            || calories <= 0)
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не удалось прочитать калории. Пример: 550",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        context.Data["calories"] = calories;

                        await bot.SendMessage(
                            chatId,
                            "Введите количество белков в порции (в граммах), например: 30.",
                            cancellationToken: ct);

                        context.CurrentStep = 5; // переходим к вводу белков
                        return ScenarioResult.InProgress;
                    }

                // Шаг 5 — БЖУ одной порции, сохранение
                case 5:
                    {
                        if (!double.TryParse(text.Replace(",", "."), NumberStyles.Any, 
                            CultureInfo.InvariantCulture, out var protein)
                            || protein < 0)
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
                            "Введите количество жиров в порции (в граммах), например: 10.",
                            cancellationToken: ct);

                        context.CurrentStep = 6;
                        return ScenarioResult.InProgress;
                    }

                case 6:
                    {
                        if (!double.TryParse(text.Replace(",", "."), NumberStyles.Any, 
                            CultureInfo.InvariantCulture, out var fat)
                            || fat < 0)
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
                            "Введите количество углеводов в порции (в граммах), например: 40.",
                            cancellationToken: ct);

                        context.CurrentStep = 7;
                        return ScenarioResult.InProgress;
                    }
                case 7:
                    {
                        if (!double.TryParse(text.Replace(",", "."), NumberStyles.Any, 
                            CultureInfo.InvariantCulture, out var carbs)
                            || carbs < 0)
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не удалось прочитать углеводы. Пример: 40.",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        var mealType = (string)context.Data["mealType"];
                        var calories = (double)context.Data["calories"];
                        var protein = context.Data.TryGetValue("protein", out var pObj) 
                            && pObj is double p ? p : 0;
                        var fat = context.Data.TryGetValue("fat", out var fObj) 
                            && fObj is double f ? f : 0;

                        var meal = new Meal
                        {
                            UserId = context.UserId,
                            DateTime = DateTime.UtcNow,
                            MealType = mealType,
                            Calories = calories,
                            Protein = protein,
                            Fat = fat,
                            Carbs = carbs
                        };

                        await nutritionService.AddMealAsync(meal, ct);

                        await bot.SendMessage(
                            chatId,
                            $"Добавлено: {calories:F0} ккал, " +
                            $"БЖУ {protein:F0}/{fat:F0}/{carbs:F0}.",
                            cancellationToken: ct);

                        return ScenarioResult.Completed;
                    }
                // ----- РЕЖИМ 2: по 100 г продукта -----

                // Шаг 10 — строка "название; ккал; Б; Ж; У"
                case 10:
                    {
                        var input = text.Replace(",", ".").Trim();
                        var parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length != 5)
                        {
                            await bot.SendMessage(
                                chatId,
                                "Ожидаю формат: `название; калории; белки; жиры; углеводы`.\n" +
                                "Пример: `гречка варёная; 110; 4; 1.8; 21`",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        var name = parts[0].Trim();

                        if (!double.TryParse(parts[1].Trim(), NumberStyles.Any, 
                            CultureInfo.InvariantCulture, out var calories100) ||
                            !double.TryParse(parts[2].Trim(), NumberStyles.Any, 
                            CultureInfo.InvariantCulture, out var protein100) ||
                            !double.TryParse(parts[3].Trim(), NumberStyles.Any, 
                            CultureInfo.InvariantCulture, out var fat100) ||
                            !double.TryParse(parts[4].Trim(), NumberStyles.Any, 
                            CultureInfo.InvariantCulture, out var carbs100))
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не удалось прочитать числа. Пример: " +
                                "`гречка варёная; 110; 4; 1.8; 21`",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        context.Data["title"] = name;
                        context.Data["cal100"] = calories100;
                        context.Data["p100"] = protein100;
                        context.Data["f100"] = fat100;
                        context.Data["c100"] = carbs100;

                        await bot.SendMessage(
                            chatId,
                            "Сколько грамм вы съели? (например: 200)",
                            cancellationToken: ct);
                        context.CurrentStep = 11;
                        return ScenarioResult.InProgress;
                    }

                // Шаг 11 — масса в граммах, пересчёт и сохранение
                case 11:
                    {
                        if (!double.TryParse(text.Replace(",", "."), NumberStyles.Any, 
                            CultureInfo.InvariantCulture, out var grams)
                            || grams <= 0)
                        {
                            await bot.SendMessage(
                                chatId,
                                "Не удалось прочитать граммы. Пример: 200",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        var factor = grams / 100.0;

                        var cal100 = (double)context.Data["cal100"];
                        var p100 = (double)context.Data["p100"];
                        var f100 = (double)context.Data["f100"];
                        var c100 = (double)context.Data["c100"];
                        var mealType = (string)context.Data["mealType"];
                        var title = (string)context.Data["title"];

                        var calories = cal100 * factor;
                        var protein = p100 * factor;
                        var fat = f100 * factor;
                        var carbs = c100 * factor;

                        var meal = new Meal
                        {
                            UserId = context.UserId,
                            DateTime = DateTime.UtcNow,
                            MealType = mealType,
                            Calories = calories,
                            Protein = protein,
                            Fat = fat,
                            Carbs = carbs
                        };

                        await nutritionService.AddMealAsync(meal, ct);

                        await bot.SendMessage(
                            chatId,
                            $"Добавлено: {title}, {grams:F0} г — {calories:F0} ккал, " +
                            $"БЖУ {protein:F0}/{fat:F0}/{carbs:F0}.",
                            cancellationToken: ct);

                        return ScenarioResult.Completed;
                    }

                    default:
                    return ScenarioResult.Completed;
            }
        }
    }
}
