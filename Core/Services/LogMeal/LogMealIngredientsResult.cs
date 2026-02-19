namespace FitnessBot.Core.Services.LogMeal
{
    public class LogMealIngredientsResult
    {
        public List<IngredientInfo>? Ingredients { get; set; }
    }

    public class IngredientInfo
    {
        public string? Name { get; set; }           
        public double Quantity { get; set; }       
        public string? Unit { get; set; }           
    }
}
