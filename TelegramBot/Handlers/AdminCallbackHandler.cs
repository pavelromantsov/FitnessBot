using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class AdminCallbackHandler : ICallbackHandler
    {
        private readonly IAdminService _adminService;
        private readonly ITelegramBotClient _bot;

        public AdminCallbackHandler(IAdminService adminService, ITelegramBotClient bot)
        {
            _adminService = adminService;
            _bot = bot;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string data)
        {
            if (!data.StartsWith("admin_", StringComparison.Ordinal))
                return false;

            // текущие admin callback’и, которые ты ловишь по префиксам
            return true;
        }
    }

}
