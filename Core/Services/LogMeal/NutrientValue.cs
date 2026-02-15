namespace FitnessBot.Core.Services.LogMeal
{
    public class NutrientValue
    {
        public string Label { get; set; } = default!;
        public double Quantity { get; set; }
        public string Unit { get; set; } = default!;
    }
}
