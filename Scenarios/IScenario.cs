using Telegram.Bot;
using Telegram.Bot.Types;

namespace FitnessBot.Scenarios
{
    public interface IScenario
    {
        ScenarioType ScenarioType { get; }

        bool CanHandle(ScenarioType type) => type == ScenarioType;

        Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct);
    }
}
