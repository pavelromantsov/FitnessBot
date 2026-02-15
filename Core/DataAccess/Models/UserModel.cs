using LinqToDB.Mapping;

namespace FitnessBot.Core.DataAccess.Models
{
    [Table("users")]
    public class UserModel
    {
        [PrimaryKey, Identity, Column("id")]
        public long Id { get; set; }

        [Column("telegram_id"), NotNull]
        public long TelegramId { get; set; }

        [Column("name"), NotNull]
        public string Name { get; set; } = null!;

        [Column("role"), NotNull]
        public string RoleRaw { get; set; } = "User";

        [Column("age"), Nullable]
        public int? Age { get; set; }

        [Column("city"), Nullable]
        public string? City { get; set; }

        [Column("created_at"), NotNull]
        public DateTime CreatedAt { get; set; }

        [Column("last_activity_at"), NotNull]
        public DateTime LastActivityAt { get; set; }

        [Column("breakfast_time")]
        public TimeSpan? BreakfastTime { get; set; }

        [Column("lunch_time")]
        public TimeSpan? LunchTime { get; set; }

        [Column("dinner_time")]
        public TimeSpan? DinnerTime { get; set; }

        [Column("heightcm"), NotNull]
        public double? HeightCm { get; set; }

        [Column("weightkg"), NotNull]
        public double? WeightKg { get; set; }

        [Column("activity_reminders_enabled"), NotNull]
        public bool ActivityRemindersEnabled { get; set; } = true;

        [Column("morning_reminder_enabled"), NotNull]
        public bool MorningReminderEnabled { get; set; } = true;

        [Column("lunch_reminder_enabled"), NotNull]
        public bool LunchReminderEnabled { get; set; } = true;

        [Column("afternoon_reminder_enabled"), NotNull]
        public bool AfternoonReminderEnabled { get; set; } = true;

        [Column("evening_reminder_enabled"), NotNull]
        public bool EveningReminderEnabled { get; set; } = true;
        
        [Column("googlefitaccesstoken", CanBeNull = true)]
        public string? GoogleFitAccessToken { get; set; }

        [Column("googlefitrefreshtoken", CanBeNull = true)]
        public string? GoogleFitRefreshToken { get; set; }

        [Column("googlefittokenexpiresat", CanBeNull = true)]
        public DateTime? GoogleFitTokenExpiresAt { get; set; }

    }
}
