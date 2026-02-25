namespace FitnessBot.Core.Services.LogMeal
{
    public class LogMealSegmentationResult
    {
        public long ImageId { get; set; }

        public List<RecognitionResult>? Recognition_Results { get; set; }

        public List<FoodFamily>? FoodFamily { get; set; }

        public FoodType? FoodType { get; set; }
    }

    public class RecognitionResult
    {
        public string? Name { get; set; }
        public int? Id { get; set; }
        public double Prob { get; set; }
        public List<FoodFamily>? FoodFamily { get; set; }
        public FoodType? FoodType { get; set; }
        public List<Subclass>? Subclasses { get; set; }
    }

    public class Subclass
    {
        public string? Name { get; set; }
        public int? Id { get; set; }
        public double Prob { get; set; }
    }

    public class FoodFamily
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public double? Prob { get; set; }
    }

    public class FoodType
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
    }
}
