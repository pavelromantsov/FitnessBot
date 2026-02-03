using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Core.Entities
{
    public class ContentItem
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        public string ContentType { get; set; } = null!; // photo/report/text
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? ExternalUrl { get; set; }
    }
}
