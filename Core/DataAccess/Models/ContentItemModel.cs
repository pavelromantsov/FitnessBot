using LinqToDB.Mapping;

namespace FitnessBot.Core.DataAccess.Models
{
    [Table("content_items")]
    public class ContentItemModel
    {
        [PrimaryKey, Identity, Column("id")]
        public long Id { get; set; }

        [Column("user_id"), NotNull]
        public long UserId { get; set; }

        [Column("content_type"), NotNull]
        public string ContentType { get; set; } = null!;

        [Column("size_bytes"), NotNull]
        public long SizeBytes { get; set; }

        [Column("created_at"), NotNull]
        public DateTime CreatedAt { get; set; }

        [Column("external_url"), Nullable]
        public string? ExternalUrl { get; set; }
    }
}
