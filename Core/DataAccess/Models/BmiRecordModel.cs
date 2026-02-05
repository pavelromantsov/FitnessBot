using LinqToDB.Mapping;

namespace FitnessBot.Core.DataAccess.Models
{
    [Table("bmi_records")]
    public class BmiRecordModel
    {
        [PrimaryKey, Identity, Column("id")]
        public long Id { get; set; }

        [Column("user_id"), NotNull]
        public long UserId { get; set; }

        [Column("height_cm"), NotNull]
        public double HeightCm { get; set; }

        [Column("weight_kg"), NotNull]
        public double WeightKg { get; set; }

        [Column("bmi"), NotNull]
        public double Bmi { get; set; }

        [Column("category"), NotNull]
        public string Category { get; set; } = null!;

        [Column("recommendation"), NotNull]
        public string Recommendation { get; set; } = null!;

        [Column("measured_at"), NotNull]
        public DateTime MeasuredAt { get; set; }
    }
}
