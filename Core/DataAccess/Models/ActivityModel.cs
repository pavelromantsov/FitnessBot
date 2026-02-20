using LinqToDB.Mapping;

namespace FitnessBot.Core.DataAccess.Models
{
    [Table("activities")]
    public class ActivityModel
    {
        [PrimaryKey, Identity, Column("id")]
        public long Id { get; set; }

        [Column("user_id"), NotNull]
        public long UserId { get; set; }

        [Column("date"), NotNull]
        public DateTime Date { get; set; }

        [Column("steps"), NotNull]
        public int Steps { get; set; }

        [Column("active_minutes"), NotNull]
        public int ActiveMinutes { get; set; }

        [Column("calories_burned"), NotNull]
        public double CaloriesBurned { get; set; }

        [Column("source"), NotNull]
        public string Source { get; set; } = "manual";
        
        [Column("activity_type"), NotNull]
        public int Type { get; set; } = 0;
    }
}
