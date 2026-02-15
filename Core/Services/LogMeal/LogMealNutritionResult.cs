namespace FitnessBot.Core.Services.LogMeal
{
    public class LogMealNutritionResult
    {
        public long ImageId { get; set; }
        public bool HasNutritionalInfo { get; set; }
        public NutritionalInfoRaw Nutritional_Info { get; set; } = default!;
        //public List<NutritionalInfoPerItem>? Nutritional_Info_Per_Item { get; set; }
        public double Serving_Size { get; set; }
    }
}
