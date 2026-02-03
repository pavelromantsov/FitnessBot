using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace FitnessBot.Core.DataAccess.Models
{
    [Table("notifications")]
    public class NotificationModel
    {
        [PrimaryKey, Identity, Column("id")]
        public long Id { get; set; }

        [Column("user_id"), NotNull]
        public long UserId { get; set; }

        [Column("type"), NotNull]
        public string Type { get; set; } = null!;

        [Column("text"), NotNull]
        public string Text { get; set; } = null!;

        [Column("scheduled_at"), NotNull]
        public DateTime ScheduledAt { get; set; }

        [Column("is_sent"), NotNull]
        public bool IsSent { get; set; }

        [Column("sent_at"), Nullable]
        public DateTime? SentAt { get; set; }
    }
}
