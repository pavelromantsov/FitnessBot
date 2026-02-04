using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace FitnessBot.Scenarios
{
    public class RegistrationScenario : IScenario
    {
        private readonly UserService _userService;
        private readonly BmiService _bmiService;

        public RegistrationScenario(UserService userService, BmiService bmiService)
        {
            _userService = userService;
            _bmiService = bmiService;
        }

        public ScenarioType ScenarioType => ScenarioType.Registration;

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
                    await bot.SendMessage(message.Chat.Id, "Укажи, пожалуйста, свой возраст (полных лет):", cancellationToken: ct);
                    context.CurrentStep = 1;
                    return ScenarioResult.InProgress;

                case 1:
                    if (!int.TryParse(text, out var age) || age < 10 || age > 100)
                    {
                        await bot.SendMessage(message.Chat.Id, "Введи возраст числом, например 30:", cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    context.Data["age"] = age.ToString(CultureInfo.InvariantCulture);   // ← ВОТ ТУТ

                    await bot.SendMessage(message.Chat.Id, "Теперь введи свой рост в сантиметрах, например 175:", cancellationToken: ct);
                    context.CurrentStep = 2;
                    return ScenarioResult.InProgress;

                case 2:
                    if (!double.TryParse(text, out var height) || height < 100 || height > 250)
                    {
                        await bot.SendMessage(message.Chat.Id, "Рост введи числом в см, например 175:", cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    context.Data["height"] = height.ToString("F1", CultureInfo.InvariantCulture);  // ← И ВОТ ТУТ

                    await bot.SendMessage(message.Chat.Id, "И последний шаг: введи свой вес в килограммах, например 80:", cancellationToken: ct);
                    context.CurrentStep = 3;
                    return ScenarioResult.InProgress;


                case 3:
                    if (!double.TryParse(text, out var weight) || weight < 30 || weight > 300)
                    {
                        await bot.SendMessage(message.Chat.Id, "Вес введи числом в кг, например 80:", cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    context.Data["weight"] = weight.ToString("F1", CultureInfo.InvariantCulture);

                    await bot.SendMessage(message.Chat.Id, "Укажи, пожалуйста, город (можно просто текстом):", cancellationToken: ct);
                    context.CurrentStep = 4;
                    return ScenarioResult.InProgress;

                case 4:
                    var city = string.IsNullOrWhiteSpace(text) ? null : text.Trim();

                    var ageStr = context.Data["age"]?.ToString() ?? "0";
                    var heightStr = context.Data["height"]?.ToString() ?? "0";
                    var weightStr = context.Data["weight"]?.ToString() ?? "0";

                    var ageVal = int.Parse(ageStr, CultureInfo.InvariantCulture);
                    var heightVal = double.Parse(heightStr, CultureInfo.InvariantCulture);
                    var weightVal = double.Parse(weightStr, CultureInfo.InvariantCulture);

                    var user = await _userService.GetByIdAsync(context.UserId);
                    if (user != null)
                    {
                        user.Age = ageVal;
                        user.HeightCm = heightVal;
                        user.WeightKg = weightVal;
                        user.City = city;
                        await _userService.SaveAsync(user);
                    }

                    await _bmiService.SaveMeasurementAsync(context.UserId, heightVal, weightVal);

                    await bot.SendMessage(message.Chat.Id,
                        "Спасибо! Данные профиля сохранены. Теперь можешь пользоваться ботом.",
                        cancellationToken: ct);

                    return ScenarioResult.Completed;

                default:
                    return ScenarioResult.Completed;
            }
        }
    }

}
