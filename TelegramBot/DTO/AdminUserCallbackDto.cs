namespace FitnessBot.TelegramBot.DTO
{
    // action|telegramId
    public class AdminUserCallbackDto : CallbackDto
    {
        public long TelegramId { get; set; }

        public AdminUserCallbackDto(string action, long telegramId)
            : base(action)
        {
            TelegramId = telegramId;
        }

        public static new AdminUserCallbackDto FromString(string input)
        {
            var parts = input.Split('|');
            if (parts.Length < 2)
                throw new ArgumentException("Некорректный формат AdminUserCallbackDto.");

            var action = parts[0];

            if (!long.TryParse(parts[1], out var telegramId))
                throw new ArgumentException("Некорректный telegramId в AdminUserCallbackDto.");

            return new AdminUserCallbackDto(action, telegramId);
        }

        public override string ToString() => $"{Action}|{TelegramId}";
    }
}

