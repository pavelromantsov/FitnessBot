using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class ChartsCommandsHandler : ICommandHandler
    {
        private readonly IChartDataService _chartDataService;
        private readonly IChartImageService _chartImageService;
        private readonly ITelegramBotClient _bot;

        public ChartsCommandsHandler(
            IChartDataService chartDataService,
            IChartImageService chartImageService,
            ITelegramBotClient bot)
        {
            _chartDataService = chartDataService;
            _chartImageService = chartImageService;
            _bot = bot;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string command, string[] args)
        {
            switch (command)
            {
                case "/charts":
                    await ShowChartsMenuAsync(context);
                    return true;

                case "/chart_weight":
                    await ShowWeightChartAsync(context);
                    return true;

                // и другие команды графиков

                default:
                    return false;
            }
        }

        // ...
    }

}
