using System;
using System.Collections.Generic;
using System.Linq;
using FitnessBot.Scenarios;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class CustomCaloriesCallbackHandler : ICallbackHandler
    {
        private readonly IScenarioContextRepository _contextRepository;
        private readonly List<IScenario> _scenarios;

        public CustomCaloriesCallbackHandler(
            IScenarioContextRepository contextRepository,
            IEnumerable<IScenario> scenarios)
        {
            _contextRepository = contextRepository;
            _scenarios = scenarios.ToList();
        }

        public async Task<bool> HandleAsync(UpdateContext ctx, string data)
        {
            if (!data.StartsWith("calories_macros_", StringComparison.OrdinalIgnoreCase))
                return false;

            var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !long.TryParse(parts[1], out var userId))
                return false;

            var scenarioContext = await _contextRepository.GetContext(userId, ctx.CancellationToken);
            if (scenarioContext == null || scenarioContext.CurrentScenario != ScenarioType.CustomCalories)
                return false;

            var bot = ctx.Bot;
            var chatId = ctx.CallbackQuery!.Message!.Chat.Id;
            var ct = ctx.CancellationToken;

            await bot.AnswerCallbackQuery(ctx.CallbackQuery.Id, cancellationToken: ct);

            if (data.StartsWith("calories_macros_yes", StringComparison.OrdinalIgnoreCase))
            {
                // пользователь хочет ввести БЖУ
                scenarioContext.CurrentStep = 3;
                await _contextRepository.SetContext(userId, scenarioContext, ct);

                await bot.SendMessage(
                    chatId,
                    "Введите количество белков (в граммах), например: 30.",
                    cancellationToken: ct);
            }
            // calories_macros_no
            else 
            {
                // не хочет БЖУ — используем шаг 2 сценария, как будто он ввёл "нет" текстом
                scenarioContext.CurrentStep = 2;
                await _contextRepository.SetContext(userId, scenarioContext, ct);

                var fakeMessage = ctx.CallbackQuery.Message!;
                fakeMessage.Text = "нет";

                var scenario = _scenarios.First(s => s.CanHandle(ScenarioType.CustomCalories));
                await scenario.HandleMessageAsync(bot, scenarioContext, fakeMessage, ct);
            }

            return true;
        }
    }
}
