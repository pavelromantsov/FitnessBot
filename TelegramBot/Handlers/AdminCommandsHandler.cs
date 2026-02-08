using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class AdminCommandsHandler : ICommandHandler
    {
        private readonly IAdminService _adminService;
        private readonly ITelegramBotClient _bot;

        public AdminCommandsHandler(IAdminService adminService, ITelegramBotClient bot)
        {
            _adminService = adminService;
            _bot = bot;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string command, string[] args)
        {
            if (!context.User.IsAdmin) // используй свой метод/поле
                return false;

            switch (command)
            {
                case "/admin":
                    return await HandleAdminAsync(context);

                case "/admin_users":
                    return await HandleAdminUsersAsync(context);

                case "/make_admin":
                    return await HandleMakeAdminAsync(context, args);

                // и остальные админские

                default:
                    return false;
            }
        }

        private async Task<bool> HandleAdminAsync(UpdateContext ctx)
        {
            // код старой команды /admin
            return true;
        }

        // ...
    }

}
