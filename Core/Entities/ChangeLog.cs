using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Core.Entities
{
    public class ChangeLog
    {
        public long Id { get; set; }
        public long? AdminUserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string ChangeType { get; set; } = null!; // e.g. "ConfigUpdated", "UserDeleted"
        public string Details { get; set; } = null!;
    }
}
