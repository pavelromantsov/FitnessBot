using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Core.Entities
{
    public class Notification
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        // Тип: BreakfastReminder / LunchReminder / DinnerReminder / GoalReminder / Error и т.п.
        public string Type { get; set; } = null!;

        public string Text { get; set; } = null!;

        public DateTime ScheduledAt { get; set; }
        public bool IsSent { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
