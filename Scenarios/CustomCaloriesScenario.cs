using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using Telegram.Bot.Types;
using Telegram.Bot;

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

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            if (!int.TryParse(message.Text, out var calories) || calories <= 0)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "Введите количество калорий положительным числом, например: 350.",
                    cancellationToken: ct);
                return ScenarioResult.InProgress;
            }

            // message.From.Id — TelegramId, ищем пользователя
            var user = await _userService.GetByTelegramIdAsync(message.From.Id);
            if (user == null)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "Пользователь не найден.",
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            var meal = new Meal
            {
                UserId = user.Id,
                DateTime = DateTime.UtcNow,
                MealType = "snack",
                Calories = calories
            };

            await _nutritionService.AddMealAsync(meal, ct);

            await bot.SendMessage(
                message.Chat.Id,
                $"Записал {calories} ккал.",
                cancellationToken: ct);

            return ScenarioResult.Completed;
        }
    }
}
