namespace FitnessBot.TelegramBot.DTO
{
    public class CallbackDto
    {
        public string Action { get; set; } 

        public CallbackDto(string action)
        {
            Action = action;
        }

        public static CallbackDto FromString(string input) 
        {
            if (input.Contains("|"))
            {
                var parts = input.Split('|');
                return new CallbackDto(parts[0]); 
            }
            else
            {
                return new CallbackDto(input); 
            }
        }

        public override string ToString()
        {
            return Action;
        }
    }
}
