using TableAttribute = System.ComponentModel.DataAnnotations.Schema.TableAttribute;


namespace FitnessBot.Core.Entities
{
    [Table("users")]
    public class User
    {
        public long Id { get; set; }
        public long TelegramId { get; set; }
        public string Name { get; set; } = null!;
        public UserRole Role { get; set; } = UserRole.User;
        public int? Age { get; set; }
        public string? City { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public TimeSpan? BreakfastTime { get; set; }
        public TimeSpan? LunchTime { get; set; }
        public TimeSpan? DinnerTime { get; set; }
        public double? HeightCm { get; set; }
        public double? WeightKg { get; set; }
        public bool ActivityRemindersEnabled { get; set; } = true;
        public bool MorningReminderEnabled { get; set; } = true;
        public bool LunchReminderEnabled { get; set; } = true;
        public bool AfternoonReminderEnabled { get; set; } = true;
        public bool EveningReminderEnabled { get; set; } = true;
    }
}
