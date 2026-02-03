using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace FitnessBot.Scenarios
{
    public class BmiScenario : IScenario
    {
        public ScenarioType ScenarioType => ScenarioType.Bmi;

        private readonly BmiService _bmiService;

        public BmiScenario(BmiService bmiService)
        {
            _bmiService = bmiService;
        }

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            var chatId = message.Chat.Id;

            switch (context.CurrentStep)
            {
                case 0:
                    await bot.SendMessage(
                        chatId,
                        "Введите ваш рост в сантиметрах:",
                        cancellationToken: ct);
                    context.CurrentStep = 1;
                    return ScenarioResult.InProgress;

                case 1:
                    if (!double.TryParse(message.Text, out var heightCm))
                    {
                        await bot.SendMessage(
                            chatId,
                            "Не удалось распознать число. Введите рост в сантиметрах, например 180.",
                            cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    context.Data["HeightCm"] = heightCm;

                    await bot.SendMessage(
                        chatId,
                        "Теперь введите ваш вес в килограммах:",
                        cancellationToken: ct);
                    context.CurrentStep = 2;
                    return ScenarioResult.InProgress;

                case 2:
                    if (!double.TryParse(message.Text, out var weightKg))
                    {
                        await bot.SendMessage(
                            chatId,
                            "Не удалось распознать число. Введите вес в килограммах, например 80.",
                            cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    var h = (double)context.Data["HeightCm"]!;
                    var record = await _bmiService.SaveMeasurementAsync(context.UserId, h, weightKg);

                    await bot.SendMessage(
                        chatId,
                        $"Ваш ИМТ: {record.Bmi:F1}, категория: {record.Category}.\n{record.Recommendation}",
                        cancellationToken: ct);

                    return ScenarioResult.Completed;

                default:
                    return ScenarioResult.Completed;
            }
        }
    }
}
