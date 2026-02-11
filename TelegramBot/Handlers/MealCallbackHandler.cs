using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.Scenarios;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class MealCallbackHandler : ICallbackHandler
    {
        private readonly UserService _userService;
        private readonly IMealRepository _mealRepository;
        private readonly IScenarioContextRepository _contextRepository;

        public MealCallbackHandler(
            UserService userService,
            IMealRepository mealRepository,
            IScenarioContextRepository contextRepository)
        {
            _userService = userService;
            _mealRepository = mealRepository;
            _contextRepository = contextRepository;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string data)
        {
            if (!data.StartsWith("meal_", StringComparison.OrdinalIgnoreCase))
                return false;

            // 1. Быстрые калории: meal_add_calories:telegramId:calories
            if (data.StartsWith("meal_add_calories", StringComparison.OrdinalIgnoreCase))
            {
                await HandleQuickCaloriesAsync(context, data);
                return true;
            }

            // 2. Кнопка «Другое количество»: meal_add_custom:telegramId
            if (data.StartsWith("meal_add_custom", StringComparison.OrdinalIgnoreCase))
            {
                await HandleCustomCaloriesAsync(context, data);
                return true;
            }

            return false;
        }

        private async Task HandleQuickCaloriesAsync(UpdateContext ctx, string data)
        {
            var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 ||
                !long.TryParse(parts[1], out var telegramId) ||
                !int.TryParse(parts[2], out var calories))
            {
                await ctx.Bot.AnswerCallbackQuery(
                    ctx.CallbackQuery!.Id,
                    "Некорректные данные.",
                    cancellationToken: default);
                return;
            }

            var user = await _userService.GetByTelegramIdAsync(telegramId);
            if (user == null)
            {
                await ctx.Bot.AnswerCallbackQuery(
                    ctx.CallbackQuery!.Id,
                    "Пользователь не найден.",
                    cancellationToken: default);
                return;
            }

            var now = DateTime.UtcNow;
            var meal = new Meal
            {
                UserId = user.Id,
                DateTime = now,
                MealType = "snack",
                Calories = calories
            };

            await _mealRepository.AddAsync(meal);

            await ctx.Bot.AnswerCallbackQuery(
                ctx.CallbackQuery!.Id,
                $"Записал {calories} ккал в {now:HH:mm}",
                cancellationToken: default);

            if (ctx.CallbackQuery!.Message != null)
            {
                await ctx.Bot.EditMessageText(
                    ctx.CallbackQuery.Message.Chat.Id,
                    ctx.CallbackQuery.Message.MessageId,
                    $"Добавлено {calories} ккал ({now:dd.MM HH:mm}).",
                    cancellationToken: default);
            }
        }

        private async Task HandleCustomCaloriesAsync(UpdateContext ctx, string data)
        {
            var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !long.TryParse(parts[1], out var telegramId))
            {
                await ctx.Bot.AnswerCallbackQuery(
                    ctx.CallbackQuery!.Id,
                    "Некорректные данные.",
                    cancellationToken: default);
                return;
            }

            var user = await _userService.GetByTelegramIdAsync(telegramId);
            if (user == null)
            {
                await ctx.Bot.AnswerCallbackQuery(
                    ctx.CallbackQuery!.Id,
                    "Пользователь не найден.",
                    cancellationToken: default);
                return;
            }

            var context = new ScenarioContext
            {
                UserId = user.Id,
                CurrentScenario = ScenarioType.CustomCalories,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(user.Id, context, default);

            await ctx.Bot.AnswerCallbackQuery(ctx.CallbackQuery!.Id, cancellationToken: default);

            if (ctx.CallbackQuery!.Message != null)
            {
                await ctx.Bot.SendMessage(
                    ctx.CallbackQuery.Message.Chat.Id,
                    "Введите количество калорий.",
                    cancellationToken: default);
            }
        }
    }
}
