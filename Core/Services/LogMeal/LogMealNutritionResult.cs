namespace FitnessBot.Core.Services.LogMeal
{
    public class LogMealNutritionResult
    {
        public List<string>? FoodName { get; set; }

        public bool HasNutritionalInfo { get; set; }

        public NutritionalInfoRaw? Nutritional_Info { get; set; }

        public double Serving_Size { get; set; }

        public double? Calories { get; set; }

        public long? ImageId { get; set; }

        public List<NutritionalInfoPerItem>? Nutritional_Info_Per_Item { get; set; }
    }

    public class NutritionalInfoPerItem
    {
        public int Food_Item_Position { get; set; }
        public bool HasNutriScore { get; set; }
        public bool HasNutritionalInfo { get; set; }
        public int Id { get; set; }
        public NutritionalInfoRaw? Nutritional_Info { get; set; }
        public double Serving_Size { get; set; }
    }
}
