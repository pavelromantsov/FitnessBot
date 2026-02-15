namespace FitnessBot.Core.Services.LogMeal
{
    public class PendingMeal
    {
        public long UserId { get; set; }
        public string MealType { get; set; } = "snack";
        public string PhotoUrl { get; set; } = default!;
        public double BaseCalories { get; set; }  
        public double BaseProtein { get; set; }
        public double BaseFat { get; set; }
        public double BaseCarbs { get; set; }
        public double ServingSizeGrams { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
