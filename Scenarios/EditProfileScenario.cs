using System.Globalization;
using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FitnessBot.Scenarios
{
    public class EditProfileScenario : IScenario
    {
        private readonly UserService _userService;
        private readonly BmiService _bmiService;

        public EditProfileScenario(UserService userService, BmiService bmiService)
        {
            _userService = userService;
            _bmiService = bmiService;
        }

        public ScenarioType ScenarioType => ScenarioType.EditProfile;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            var text = message.Text ?? string.Empty;

            switch (context.CurrentStep)
            {
                case 0:
                    {
                        var user = await _userService.GetByIdAsync(context.UserId);

                        var profileText =
                            "Текущий профиль:\n" +
                            $"Возраст: {(user?.Age?.ToString() ?? "не задан")}\n" +
                            $"Рост: {(user?.HeightCm?.ToString("F1") ?? "не задан")}\n" +
                            $"Вес: {(user?.WeightKg?.ToString("F1") ?? "не задан")}\n" +
                            $"Город: {(user?.City ?? "не задан")}\n\n" +
                            "Отправь новый возраст (лет) или '-' если не хочешь менять:";

                        await bot.SendMessage(
                            message.Chat.Id,
                            profileText,
                            cancellationToken: ct);

                        context.CurrentStep = 1;
                        return ScenarioResult.InProgress;
                    }

                case 1:
                    {
                        if (text != "-" && (!int.TryParse(text, out var age) || 
                            age < 10 || age > 100))
                        {
                            await bot.SendMessage(
                                message.Chat.Id,
                                "Возраст введи числом или '-' для пропуска:",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        if (text != "-")
                            context.Data["age"] = text;

                        await bot.SendMessage(
                            message.Chat.Id,
                            "Теперь новый рост в см или '-' для пропуска:",
                            cancellationToken: ct);

                        context.CurrentStep = 2;
                        return ScenarioResult.InProgress;
                    }

                case 2:
                    {
                        if (text != "-" && (!double.TryParse(text, out var height) || 
                            height < 100 || height > 250))
                        {
                            await bot.SendMessage(
                                message.Chat.Id,
                                "Рост введи числом в см или '-':",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        if (text != "-")
                            context.Data["height"] = text;

                        await bot.SendMessage(
                            message.Chat.Id,
                            "Новый вес в кг или '-' для пропуска:",
                            cancellationToken: ct);

                        context.CurrentStep = 3;
                        return ScenarioResult.InProgress;
                    }

                case 3:
                    {
                        if (text != "-" && (!double.TryParse(text, out var weight) || 
                            weight < 30 || weight > 300))
                        {
                            await bot.SendMessage(
                                message.Chat.Id,
                                "Вес введи числом в кг или '-':",
                                cancellationToken: ct);
                            return ScenarioResult.InProgress;
                        }

                        if (text != "-")
                            context.Data["weight"] = text;

                        await bot.SendMessage(
                            message.Chat.Id,
                            "Новый город (текстом) или '-' для пропуска:",
                            cancellationToken: ct);

                        context.CurrentStep = 4;
                        return ScenarioResult.InProgress;
                    }

                case 4:
                    {
                        var city = text == "-" ? null : text.Trim();

                        var user = await _userService.GetByIdAsync(context.UserId);
                        if (user != null)
                        {
                            if (context.Data.TryGetValue("age", out var ageObj))
                                user.Age = int.Parse(ageObj!.ToString()!, 
                                    CultureInfo.InvariantCulture);

                            if (context.Data.TryGetValue("height", out var hObj))
                                user.HeightCm = double.Parse(hObj!.ToString()!, 
                                    CultureInfo.InvariantCulture);

                            if (context.Data.TryGetValue("weight", out var wObj))
                                user.WeightKg = double.Parse(wObj!.ToString()!, 
                                    CultureInfo.InvariantCulture);

                            if (city != null)
                                user.City = city;

                            await _userService.SaveAsync(user);

                            if (user.HeightCm.HasValue && user.WeightKg.HasValue)
                                await _bmiService.SaveMeasurementAsync(user.Id, 
                                    user.HeightCm.Value, user.WeightKg.Value);
                        }

                        await bot.SendMessage(
                            message.Chat.Id,
                            "Профиль обновлён.",
                            cancellationToken: ct);

                        return ScenarioResult.Completed;
                    }

                default:
                    return ScenarioResult.Completed;
            }
        }
    }
}

