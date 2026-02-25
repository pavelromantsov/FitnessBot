using LinqToDB.Mapping;

namespace FitnessBot.Core.DataAccess.Models
{

 [Table("meals")]
    public class MealModel
    {
        [PrimaryKey, Identity, Column("id")]
        public long Id { get; set; }

        [Column("user_id"), NotNull]
        public long UserId { get; set; }

        [Column("date_time"), NotNull]
        public DateTime DateTime { get; set; }

        [Column("meal_type"), NotNull]
        public string MealType { get; set; } = "unknown";

        [Column("calories"), NotNull]
        public double Calories { get; set; }

        [Column("protein"), NotNull]
        public double Protein { get; set; }

        [Column("fat"), NotNull]
        public double Fat { get; set; }

        [Column("carbs"), NotNull]
        public double Carbs { get; set; }

        [Column("photo_url"), Nullable]
        public string? PhotoUrl { get; set; }
        
        [Column("dish_name"), Nullable]
        public string? DishName { get; set; }
    }
}
