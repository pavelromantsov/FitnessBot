namespace FitnessBot.Core.Services.LogMeal
{
    public class NutritionalInfoRaw
    {
        public double Calories { get; set; }
        public Dictionary<string, NutrientValue> TotalNutrients { get; set; } = default!;
    }
}
