using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Entities;
using LinqToDB.Mapping;

namespace FitnessBot.Core.DataAccess.Models
{
    [Table("users")]
    public class UserModel
    {
        [PrimaryKey, Identity, Column("id")]
        public long Id { get; set; }

        [Column("telegram_id"), NotNull]
        public long TelegramId { get; set; }

        [Column("name"), NotNull]
        public string Name { get; set; } = null!;

        [Column("role"), NotNull]
        public string RoleRaw { get; set; } = "User";

        [Column("age"), Nullable]
        public int? Age { get; set; }

        [Column("city"), Nullable]
        public string? City { get; set; }

        [Column("created_at"), NotNull]
        public DateTime CreatedAt { get; set; }

        [Column("last_activity_at"), NotNull]
        public DateTime LastActivityAt { get; set; }
    }
}
