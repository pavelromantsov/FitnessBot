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
