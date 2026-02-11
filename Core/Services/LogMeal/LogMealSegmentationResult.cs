namespace FitnessBot.Core.Services.LogMeal
{
    public class LogMealSegmentationResult
    {
        public NutritionalInfo? TotalNutritionalInfo { get; set; }
        public DishNutritionalInfo[]? Dishes { get; set; }
    }
}
