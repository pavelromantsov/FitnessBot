namespace FitnessBot.Core.Entities
{
    public class Notification
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        public string Type { get; set; } = null!;

        public string Text { get; set; } = null!;

        public DateTime ScheduledAt { get; set; }
        public bool IsSent { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
