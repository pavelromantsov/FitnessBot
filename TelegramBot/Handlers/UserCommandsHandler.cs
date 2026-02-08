using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class UserCommandsHandler : ICommandHandler
    {
        private readonly IScenarioRunner _scenarioRunner;
        private readonly IUserService _userService;
        private readonly ITelegramBotClient _bot;

        public UserCommandsHandler(
            IScenarioRunner scenarioRunner,
            IUserService userService,
            ITelegramBotClient bot)
        {
            _scenarioRunner = scenarioRunner;
            _userService = userService;
            _bot = bot;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string command, string[] args)
        {
            switch (command)
            {
                case "/start":
                    await HandleStartAsync(context);
                    return true;

                case "/bmi":
                    await HandleBmiAsync(context);
                    return true;

                case "/today":
                    await HandleTodayAsync(context);
                    return true;

                // сюда переносишь всё, что не админское и не графики

                default:
                    return false; // команда не наша
            }
        }

        private Task HandleStartAsync(UpdateContext ctx)
        {
            // сюда почти дословно перенести код из старого UpdateHandler
        }

        private Task HandleBmiAsync(UpdateContext ctx)
        {
            // вызов сценария BMI или сервиса BMI
        }

        private Task HandleTodayAsync(UpdateContext ctx)
        {
            // текущая логика /today
        }
    }

}
