using LinqToDB.Mapping;

namespace FitnessBot.Core.DataAccess.Models
{
    [Table("daily_goals")]
    public class DailyGoalModel
    {
        [PrimaryKey, Identity, Column("id")]
        public long Id { get; set; }

        [Column("user_id"), NotNull]
        public long UserId { get; set; }

        [Column("date"), NotNull]
        public DateTime Date { get; set; }

        [Column("target_steps"), NotNull]
        public int TargetSteps { get; set; }

        [Column("target_calories_in"), NotNull]
        public double TargetCaloriesIn { get; set; }

        [Column("target_calories_out"), NotNull]
        public double TargetCaloriesOut { get; set; }

        [Column("is_completed"), NotNull]
        public bool IsCompleted { get; set; }

        [Column("completed_at"), Nullable]
        public DateTime? CompletedAt { get; set; }
    }
}
