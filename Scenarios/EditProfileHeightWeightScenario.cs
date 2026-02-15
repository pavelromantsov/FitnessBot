using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FitnessBot.Scenarios
{
    public class EditProfileHeightWeightScenario : IScenario
    {
        private readonly BmiService _bmiService;

        public EditProfileHeightWeightScenario(BmiService bmiService)
        {
            _bmiService = bmiService;
        }

        public ScenarioType ScenarioType => ScenarioType.EditProfileHeightWeight;

        public bool CanHandle(ScenarioType type) => type == ScenarioType.EditProfileHeightWeight;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            switch (context.CurrentStep)
            {
                case 0: // Получение роста
                    if (!double.TryParse(message.Text, out var height) || height < 100 || 
                        height > 250)
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            "❌ Пожалуйста, введите корректный рост (от 100 до 250 см):",
                            cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    context.Data["height"] = height;
                    context.CurrentStep = 1;

                    await bot.SendMessage(
                        message.Chat.Id,
                        "⚖️ Введите ваш вес в килограммах (например: 75):",
                        cancellationToken: ct);
                    return ScenarioResult.InProgress;

                case 1: // Получение веса и сохранение
                    if (!double.TryParse(message.Text, out var weight) || weight < 30 || 
                        weight > 300)
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            "❌ Пожалуйста, введите корректный вес (от 30 до 300 кг):",
                            cancellationToken: ct);
                        return ScenarioResult.InProgress;
                    }

                    var heightValue = (double)context.Data["height"];

                    // Сохраняем замер ИМТ
                    var record = await _bmiService.SaveMeasurementAsync(context.UserId, 
                        heightValue, weight);

                    await bot.SendMessage(
                        message.Chat.Id,
                        $"✅ **Данные успешно обновлены!**\n\n" +
                        $"⚖️ **Ваш ИМТ: {record.Bmi:F1}**\n" +
                        $"📏 Рост: {heightValue} см\n" +
                        $"⚖️ Вес: {weight} кг\n\n" +
                        $"**Категория:** {record.Category}\n\n" +
                        $"💡 {record.Recommendation}",
                        cancellationToken: ct);

                    return ScenarioResult.Completed;

                default:
                    return ScenarioResult.Completed;
            }
        }
    }
}
