using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.TelegramBot
{
    public interface ICommandHandler
    {
        Task<bool> HandleAsync(UpdateContext context, string command, string[] args);
    }

    public interface ICallbackHandler
    {
        Task<bool> HandleAsync(UpdateContext context, string data);
    }
}
