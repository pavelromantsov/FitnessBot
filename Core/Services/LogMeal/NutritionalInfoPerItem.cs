namespace FitnessBot.Core.Services.LogMeal
{
    public class NutritionalInfoPerItem
    {
        public int Food_Item_Position { get; set; }
        public bool HasNutritionalInfo { get; set; }
        public NutritionalInfoRaw Nutritional_Info { get; set; } = default!;
    }
}
