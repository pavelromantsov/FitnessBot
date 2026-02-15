using FitnessBot.Scenarios;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class BmiCallbackHandler : ICallbackHandler
    {
        private readonly IScenarioContextRepository _contextRepository;

        public BmiCallbackHandler(IScenarioContextRepository contextRepository)
        {
            _contextRepository = contextRepository;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string data)
        {
            if (data != "bmi_edit_profile")
                return false;

            if (context.CallbackQuery?.Message != null)
            {
                await context.Bot.DeleteMessage(
                    context.ChatId,
                    context.CallbackQuery.Message.MessageId,
                    cancellationToken: default);
            }

            var scenarioContext = new ScenarioContext
            {
                UserId = context.User.Id,
                CurrentScenario = ScenarioType.EditProfileHeightWeight, 
                CurrentStep = 0
            };

            await _contextRepository.SetContext(context.User.Id, scenarioContext, default);
            
            await context.Bot.SendMessage(
                context.ChatId,
                "📏 Обновление данных для расчёта ИМТ\n\n" +
                "Введите ваш рост в сантиметрах (например: 180):",
                cancellationToken: default);

            if (context.CallbackQuery != null)
            {
                await context.Bot.AnswerCallbackQuery(
                    context.CallbackQuery.Id,
                    cancellationToken: default);
            }

            return true;
        }
    }
}
