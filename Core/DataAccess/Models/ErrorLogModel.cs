using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace FitnessBot.Core.DataAccess.Models
{
    [Table("error_logs")]
    public class ErrorLogModel
    {
        [PrimaryKey, Identity, Column("id")]
        public long Id { get; set; }

        [Column("timestamp"), NotNull]
        public DateTime Timestamp { get; set; }

        [Column("level"), NotNull]
        public string Level { get; set; } = null!;

        [Column("message"), NotNull]
        public string Message { get; set; } = null!;

        [Column("stack_trace"), Nullable]
        public string? StackTrace { get; set; }

        [Column("context_json"), Nullable]
        public string? ContextJson { get; set; }
    }
}
