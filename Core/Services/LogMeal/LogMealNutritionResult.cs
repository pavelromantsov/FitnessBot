namespace FitnessBot.Core.Services.LogMeal
{
    public class LogMealNutritionResult
    {
        public bool HasNutritionalInfo { get; set; }
        public NutritionalInfoRaw Nutritional_Info { get; set; } = default!;
        public double Serving_Size { get; set; }
    }
}
