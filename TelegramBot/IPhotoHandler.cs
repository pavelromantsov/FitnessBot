namespace FitnessBot.TelegramBot
{
    public interface IPhotoHandler
    {
        Task<bool> HandleAsync(UpdateContext context);
    }
}
