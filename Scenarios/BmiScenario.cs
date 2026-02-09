using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using Telegram.Bot.Types;
using Telegram.Bot;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;

namespace FitnessBot.Scenarios
{
    public class BmiScenario : IScenario
    {
        public ScenarioType ScenarioType => ScenarioType.Bmi;

        private readonly BmiService _bmiService;
        private readonly IErrorLogRepository _errorLog;

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
            try
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
            catch (Exception ex)
            {
                await _errorLog.AddAsync(new ErrorLog
                {
                    Level = "Error",
                    Message = $"BmiScenario Error for User {context.UserId}: {ex.Message}",
                    StackTrace = ex.StackTrace,
                    Timestamp = DateTime.UtcNow
                });

                await bot.SendMessage(
                    message.Chat.Id,
                    "❌ Произошла ошибка. Попробуйте позже.",
                    cancellationToken: ct);

                return ScenarioResult.Failed;
            }
        }
    }
}
