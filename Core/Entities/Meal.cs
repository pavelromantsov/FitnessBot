namespace FitnessBot.Core.Entities
{
    public class Meal
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        public DateTime DateTime { get; set; }
        public string MealType { get; set; } = "unknown"; 

        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbs { get; set; }

        public string? PhotoUrl { get; set; }
    }
}
