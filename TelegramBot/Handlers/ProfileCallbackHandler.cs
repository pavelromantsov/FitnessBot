using System;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using FitnessBot.Infrastructure.DataAccess;
using FitnessBot.Scenarios;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class ProfileCallbackHandler : ICallbackHandler
    {
        private readonly UserService _userService;
        private readonly BmiService _bmiService;
        private readonly IScenarioContextRepository _contextRepository;

        public ProfileCallbackHandler(
            UserService userService,
            BmiService bmiService,
            IScenarioContextRepository contextRepository)
        {
            _userService = userService;
            _bmiService = bmiService;
            _contextRepository = contextRepository;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string data)
        {
            if (!data.StartsWith("profile_"))
                return false;

            if (data == "profile_edit_menu")
            {
                await ShowEditMenu(context);
                return true;
            }

            if (data == "profile_edit_bmi")
            {
                await StartEditBmi(context);
                return true;
            }

            if (data == "profile_edit_age")
            {
                await StartEditAge(context);
                return true;
            }

            if (data == "profile_edit_city")
            {
                await StartEditCity(context);
                return true;
            }

            if (data == "profile_edit_meals")
            {
                await StartEditMealTimes(context);
                return true;
            }

            if (data == "profile_back")
            {
                await ShowProfile(context);
                return true;
            }

            return false;
        }


        private async Task ShowEditMenu(UpdateContext context)
        {
            var buttons = new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("📏 Изменить рост и вес", "profile_edit_bmi") },
        new[] { InlineKeyboardButton.WithCallbackData("🎂 Изменить возраст", "profile_edit_age") },
        new[] { InlineKeyboardButton.WithCallbackData("🏙️ Изменить город", "profile_edit_city") },
        new[] { InlineKeyboardButton.WithCallbackData("🕐 Изменить время приёмов пищи", "profile_edit_meals") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад к профилю", "profile_back") }
    };

            var keyboard = new InlineKeyboardMarkup(buttons);

            await context.Bot.EditMessageText(
                context.ChatId,
                context.CallbackQuery!.Message!.MessageId,
                "✏️ **Что вы хотите изменить?**",
                replyMarkup: keyboard,
                cancellationToken: default);

            await context.Bot.AnswerCallbackQuery(
                context.CallbackQuery.Id,
                cancellationToken: default);
        }

        private async Task StartEditAge(UpdateContext context)
        {
            await context.Bot.DeleteMessage(
                context.ChatId,
                context.CallbackQuery!.Message!.MessageId,
                cancellationToken: default);

            var scenarioContext = new ScenarioContext
            {
                UserId = context.User.Id,
                CurrentScenario = ScenarioType.EditProfileAge,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(context.User.Id, scenarioContext, default);

            await context.Bot.SendMessage(
                context.ChatId,
                "🎂 **Изменение возраста**\n\n" +
                $"Текущий возраст: {(context.User.Age.HasValue ? context.User.Age.ToString() : "не указан")}\n\n" +
                "Введите новый возраст (число от 10 до 120):",
                cancellationToken: default);

            await context.Bot.AnswerCallbackQuery(
                context.CallbackQuery.Id,
                cancellationToken: default);
        }

        private async Task StartEditCity(UpdateContext context)
        {
            await context.Bot.DeleteMessage(
                context.ChatId,
                context.CallbackQuery!.Message!.MessageId,
                cancellationToken: default);

            var scenarioContext = new ScenarioContext
            {
                UserId = context.User.Id,
                CurrentScenario = ScenarioType.EditProfileCity,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(context.User.Id, scenarioContext, default);

            await context.Bot.SendMessage(
                context.ChatId,
                "🏙️ **Изменение города**\n\n" +
                $"Текущий город: {(string.IsNullOrEmpty(context.User.City) ? "не указан" : context.User.City)}\n\n" +
                "Введите название города:",
                cancellationToken: default);

            await context.Bot.AnswerCallbackQuery(
                context.CallbackQuery.Id,
                cancellationToken: default);
        }

        private async Task StartEditMealTimes(UpdateContext context)
        {
            await context.Bot.DeleteMessage(
                context.ChatId,
                context.CallbackQuery!.Message!.MessageId,
                cancellationToken: default);

            var scenarioContext = new ScenarioContext
            {
                UserId = context.User.Id,
                CurrentScenario = ScenarioType.MealTimeSetup,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(context.User.Id, scenarioContext, default);

            await context.Bot.SendMessage(
                context.ChatId,
                "🕐 **Настройка времени приёмов пищи**\n\n" +
                "Введите время завтрака в формате HH:mm\n" +
                "Например: 08:00",
                cancellationToken: default);

            await context.Bot.AnswerCallbackQuery(
                context.CallbackQuery.Id,
                cancellationToken: default);
        }

        private async Task ShowProfile(UpdateContext context)
        {
            var user = await _userService.GetByTelegramIdAsync(context.User.TelegramId);
            if (user == null) return;

            // Получаем последний замер ИМТ
            var latestBmi = await _bmiService.GetLastAsync(user.Id);

            var bmiInfo = latestBmi != null
                ? $"📏 Рост: {latestBmi.HeightCm} см\n" +
                  $"⚖️ Вес: {latestBmi.WeightKg} кг\n" +
                  $"📊 ИМТ: {latestBmi.Bmi:F1} ({latestBmi.Category})\n\n"
                : "📏 Рост и вес: не указаны\n\n";

            var profileText =
                $"👤 **Ваш профиль**\n\n" +
                $"Имя: {user.Name}\n" +
                $"Возраст: {(user.Age.HasValue ? user.Age.ToString() : "не указан")}\n" +
                $"Город: {(string.IsNullOrEmpty(user.City) ? "не указан" : user.City)}\n" +
                $"Роль: {user.Role}\n" +
                $"TelegramId: `{user.TelegramId}`\n\n" +
                bmiInfo +
                $"🕐 **Время приёмов пищи:**\n" +
                $"Завтрак: {(user.BreakfastTime.HasValue ? user.BreakfastTime.Value.ToString(@"hh\:mm") : "не установлено")}\n" +
                $"Обед: {(user.LunchTime.HasValue ? user.LunchTime.Value.ToString(@"hh\:mm") : "не установлено")}\n" +
                $"Ужин: {(user.DinnerTime.HasValue ? user.DinnerTime.Value.ToString(@"hh\:mm") : "не установлено")}\n\n" +
                $"📅 Создан: {user.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                $"🕐 Последняя активность: {user.LastActivityAt:dd.MM.yyyy HH:mm}";

            var buttons = new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("✏️ Редактировать профиль", "profile_edit_menu")
        }
    };

            var keyboard = new InlineKeyboardMarkup(buttons);

            await context.Bot.EditMessageText(
                context.ChatId,
                context.CallbackQuery!.Message!.MessageId,
                profileText,
                replyMarkup: keyboard,
                cancellationToken: default);

            await context.Bot.AnswerCallbackQuery(
                context.CallbackQuery.Id,
                cancellationToken: default);
        }


        private async Task StartEditBmi(UpdateContext context)
        {
            await context.Bot.DeleteMessage(
                context.ChatId,
                context.CallbackQuery!.Message!.MessageId,
                cancellationToken: default);

            var scenarioContext = new ScenarioContext
            {
                UserId = context.User.Id,
                CurrentScenario = ScenarioType.EditProfileHeightWeight, // ИЗМЕНЕНО ЗДЕСЬ
                CurrentStep = 0
            };

            await _contextRepository.SetContext(context.User.Id, scenarioContext, default);

            // Получаем текущие данные, если есть
            var latestBmi = await _bmiService.GetLastAsync(context.User.Id);
            var currentDataText = latestBmi != null
                ? $"Текущие данные: рост {latestBmi.HeightCm} см, вес {latestBmi.WeightKg} кг\n\n"
                : "Данные о росте и весе ещё не указаны.\n\n";

            await context.Bot.SendMessage(
                context.ChatId,
                $"📏 **Изменение роста и веса**\n\n" +
                currentDataText +
                "Введите ваш рост в сантиметрах (например: 180):",
                cancellationToken: default);

            await context.Bot.AnswerCallbackQuery(
                context.CallbackQuery.Id,
                cancellationToken: default);
        }


    }
}
