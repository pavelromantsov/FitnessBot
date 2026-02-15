namespace FitnessBot.Core.Entities
{
    public class Activity
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public DateTime Date { get; set; }
        public int Steps { get; set; }
        public int ActiveMinutes { get; set; }
        public double CaloriesBurned { get; set; }
        public string Source { get; set; } = "manual"; 
    }
}
