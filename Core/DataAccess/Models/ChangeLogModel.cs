using LinqToDB.Mapping;

namespace FitnessBot.Core.DataAccess.Models
{
    [Table("change_logs")]
    public class ChangeLogModel
    {
        [PrimaryKey, Identity, Column("id")]
        public long Id { get; set; }

        [Column("admin_user_id"), Nullable]
        public long? AdminUserId { get; set; }

        [Column("timestamp"), NotNull]
        public DateTime Timestamp { get; set; }

        [Column("change_type"), NotNull]
        public string ChangeType { get; set; } = null!;

        [Column("details"), NotNull]
        public string Details { get; set; } = null!;
    }
}
