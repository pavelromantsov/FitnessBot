using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace FitnessBot.TelegramBot
{
    public sealed class UpdateContext
    {
        public ITelegramBotClient Bot { get; }
        public UserDb User { get; }
        public long ChatId { get; }
        public Message Message { get; }
        public CallbackQuery CallbackQuery { get; }
        public IScenarioRunner ScenarioRunner { get; }
        public IAdminService AdminService { get; }
        public IChartService ChartService { get; }
        // + то, что реально используется в логике команд/колбеков

        public UpdateContext(
            ITelegramBotClient bot,
            UserDb user,
            long chatId,
            Message message,
            CallbackQuery callbackQuery,
        IScenarioRunner scenarioRunner,
            IAdminService adminService,
            IChartService chartService /* ... */)
        {
            Bot = bot;
            User = user;
            ChatId = chatId;
            Message = message;
            CallbackQuery = callbackQuery;
            ScenarioRunner = scenarioRunner;
            AdminService = adminService;
            ChartService = chartService;
        }
    }
}
