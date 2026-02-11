namespace FitnessBot.Core.Entities
{
    public class ChangeLog
    {
        public long Id { get; set; }
        public long? AdminUserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ChangeType { get; set; } = null!; 
        public string Details { get; set; } = null!;
    }
}
