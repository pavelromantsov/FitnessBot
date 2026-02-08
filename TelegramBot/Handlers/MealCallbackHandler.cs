using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class MealCallbackHandler : ICallbackHandler
    {
        private readonly IScenarioRunner _scenarioRunner;
        private readonly ITelegramBotClient _bot;

        public MealCallbackHandler(IScenarioRunner scenarioRunner, ITelegramBotClient bot)
        {
            _scenarioRunner = scenarioRunner;
            _bot = bot;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string data)
        {
            if (!data.StartsWith("meal_", StringComparison.Ordinal))
                return false;

            // дальше твой текущий разбор callbackData: meal_add, meal_edit и т.п.
            return true;
        }
    }

}
