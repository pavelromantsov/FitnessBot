namespace FitnessBot.TelegramBot.DTO
{
    // action|userId|calories
    public class MealCaloriesCallbackDto : CallbackDto
    {
        public long TelegramId { get; set; }
        public int Calories { get; set; }

        public MealCaloriesCallbackDto(string action, long telegramId, int calories)
            : base(action)
        {
            TelegramId = telegramId;
            Calories = calories;
        }

        public static new MealCaloriesCallbackDto FromString(string input)
        {
            var parts = input.Split('|');
            if (parts.Length < 3)
                throw new ArgumentException("Некорректный формат callbackData для MealCaloriesCallbackDto.");

            var action = parts[0];

            if (!long.TryParse(parts[1], out var telegramId))
                throw new ArgumentException("Некорректный telegramId в MealCaloriesCallbackDto.");

            if (!int.TryParse(parts[2], out var calories))
                throw new ArgumentException("Некорректные калории в MealCaloriesCallbackDto.");

            return new MealCaloriesCallbackDto(action, telegramId, calories);
        }

        public override string ToString() => $"{Action}|{TelegramId}|{Calories}";
    }
}
