namespace FitnessBot.Core.Entities
{
    public class ErrorLog
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Level { get; set; } = "Error"; 
        public string Message { get; set; } = null!;
        public string? StackTrace { get; set; }
        public string? ContextJson { get; set; }
    }
}
