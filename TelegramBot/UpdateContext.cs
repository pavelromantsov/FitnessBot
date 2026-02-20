using Telegram.Bot;
using Telegram.Bot.Types;
using DomainUser = FitnessBot.Core.Entities.User;

namespace FitnessBot.TelegramBot
{
    public sealed class UpdateContext
    {
        internal readonly string Update;
        public ITelegramBotClient Bot { get; }
        public DomainUser User { get; }
        public long ChatId { get; }
        public Message? Message { get; }
        public CallbackQuery? CallbackQuery { get; }
        public CancellationToken CancellationToken { get; }

        public UpdateContext(
            ITelegramBotClient bot,
            DomainUser user,
            long chatId,
            Message? message,
            CallbackQuery? callbackQuery,
            CancellationToken cancellationToken)
        {
            Bot = bot;
            User = user;
            ChatId = chatId;
            Message = message;
            CallbackQuery = callbackQuery;
            CancellationToken = cancellationToken;
        }
    }
}
