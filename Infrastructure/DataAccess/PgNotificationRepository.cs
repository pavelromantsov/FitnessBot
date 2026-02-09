using FitnessBot.Core.Abstractions;
using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgNotificationRepository :
    IDailyGoalRepository,
    IBmiRepository,
    IErrorLogRepository,
    IChangeLogRepository,
    IContentItemRepository,
    INotificationRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgNotificationRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // ---------- DailyGoal (IDailyGoalRepository) ----------

        private static DailyGoalModel Map(DailyGoal g) => new()
        {
            Id = g.Id,
            UserId = g.UserId,
            Date = g.Date,
            TargetSteps = g.TargetSteps,
            TargetCaloriesIn = g.TargetCaloriesIn,
            TargetCaloriesOut = g.TargetCaloriesOut,
            IsCompleted = g.IsCompleted,
            CompletedAt = g.CompletedAt
        };

        private static DailyGoal Map(DailyGoalModel m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            Date = m.Date,
            TargetSteps = m.TargetSteps,
            TargetCaloriesIn = m.TargetCaloriesIn,
            TargetCaloriesOut = m.TargetCaloriesOut,
            IsCompleted = m.IsCompleted,
            CompletedAt = m.CompletedAt
        };

        public async Task<DailyGoal?> GetByUserAndDateAsync(long userId, DateTime date)
        {
            await using var db = _connectionFactory();

            var model = await db.DailyGoals
                .Where(g => g.UserId == userId && g.Date == date.Date)
                .FirstOrDefaultAsync(); // DailyGoalModel?

            return model == null ? null : Map(model);
        }

        public async Task SaveAsync(DailyGoal goal)
        {
            await using var db = _connectionFactory(); // ИСПРАВЛЕНО: было _dataContextFactory

            // Преобразуем в модель
            var model = Map(goal);

            // Проверяем, существует ли уже цель на эту дату для этого пользователя
            var existingModel = await db.DailyGoals
                .Where(g => g.UserId == model.UserId && g.Date == model.Date)
                .FirstOrDefaultAsync();

            if (existingModel != null)
            {
                // Обновляем существующую запись
                model.Id = existingModel.Id;
                await db.UpdateAsync(model);
                goal.Id = model.Id;
            }
            else
            {
                // Создаём новую запись
                model.Id = await db.InsertWithInt64IdentityAsync(model);
                goal.Id = model.Id;
            }
        }

        // ---------- BMI (IBmiRepository) ----------

        private static BmiRecordModel Map(BmiRecord r) => new()
        {
            Id = r.Id,
            UserId = r.UserId,
            HeightCm = r.HeightCm,
            WeightKg = r.WeightKg,
            Bmi = r.Bmi,
            Category = r.Category,
            Recommendation = r.Recommendation,
            MeasuredAt = r.MeasuredAt
        };

        private static BmiRecord Map(BmiRecordModel m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            HeightCm = m.HeightCm,
            WeightKg = m.WeightKg,
            Bmi = m.Bmi,
            Category = m.Category,
            Recommendation = m.Recommendation,
            MeasuredAt = m.MeasuredAt
        };

        public async Task SaveAsync(BmiRecord record)
        {
            await using var db = _connectionFactory();
            var model = Map(record);

            if (model.Id == 0)
            {
                model.Id = await db.InsertWithInt64IdentityAsync(model);
            }
            else
            {
                await db.UpdateAsync(model);
            }

            record.Id = model.Id;
        }

        public async Task<BmiRecord?> GetLastAsync(long userId)
        {
            await using var db = _connectionFactory();

            var model = await db.BmiRecords
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.MeasuredAt)
                .FirstOrDefaultAsync();

            return model == null ? null : Map(model);
        }

        // ---------- ErrorLog (IErrorLogRepository) ----------

        private static ErrorLogModel Map(ErrorLog e) => new()
        {
            Id = e.Id,
            Timestamp = e.Timestamp,
            Level = e.Level,
            Message = e.Message,
            StackTrace = e.StackTrace,
            ContextJson = e.ContextJson
        };

        private static ErrorLog Map(ErrorLogModel m) => new()
        {
            Id = m.Id,
            Timestamp = m.Timestamp,
            Level = m.Level,
            Message = m.Message,
            StackTrace = m.StackTrace,
            ContextJson = m.ContextJson
        };

        public async Task AddAsync(ErrorLog log)
        {
            await using var db = _connectionFactory();
            var model = Map(log);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            log.Id = model.Id;
        }

        // ---------- ChangeLog (IChangeLogRepository) ----------

        private static ChangeLogModel Map(ChangeLog c) => new()
        {
            Id = c.Id,
            AdminUserId = c.AdminUserId,
            Timestamp = c.Timestamp,
            ChangeType = c.ChangeType,
            Details = c.Details
        };

        private static ChangeLog Map(ChangeLogModel m) => new()
        {
            Id = m.Id,
            AdminUserId = m.AdminUserId,
            Timestamp = m.Timestamp,
            ChangeType = m.ChangeType,
            Details = m.Details
        };

        public async Task AddAsync(ChangeLog log)
        {
            await using var db = _connectionFactory();
            var model = Map(log);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            log.Id = model.Id;
        }

        // ---------- ContentItem (IContentItemRepository) ----------

        private static ContentItemModel Map(ContentItem c) => new()
        {
            Id = c.Id,
            UserId = c.UserId,
            ContentType = c.ContentType,
            SizeBytes = c.SizeBytes,
            CreatedAt = c.CreatedAt,
            ExternalUrl = c.ExternalUrl
        };

        private static ContentItem Map(ContentItemModel m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            ContentType = m.ContentType,
            SizeBytes = m.SizeBytes,
            CreatedAt = m.CreatedAt,
            ExternalUrl = m.ExternalUrl
        };

        public async Task AddAsync(ContentItem item)
        {
            await using var db = _connectionFactory();
            var model = Map(item);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            item.Id = model.Id;
        }

        public async Task<long> GetTotalSizeAsync()
        {
            await using var db = _connectionFactory();
            return await db.ContentItems
                       .SumAsync(ci => (long?)ci.SizeBytes) ?? 0L;
        }

        private static NotificationModel Map(Notification n) => new()
        {
            Id = n.Id,
            UserId = n.UserId,
            Type = n.Type,
            Text = n.Text,
            ScheduledAt = n.ScheduledAt,
            IsSent = n.IsSent,
            SentAt = n.SentAt
        };

        private static Notification Map(NotificationModel m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            Type = m.Type,
            Text = m.Text,
            ScheduledAt = m.ScheduledAt,
            IsSent = m.IsSent,
            SentAt = m.SentAt
        };

        public async Task<long> AddAsync(Notification notification)
        {
            await using var db = _connectionFactory();
            var model = Map(notification);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            notification.Id = model.Id;
            return model.Id;
        }

        public async Task MarkSentAsync(long id, DateTime sentAt)
        {
            await using var db = _connectionFactory();

            await db.Notifications
                .Where(n => n.Id == id)
                .Set(n => n.IsSent, true)
                .Set(n => n.SentAt, sentAt)
                .UpdateAsync();
        }

        public async Task<IReadOnlyList<Notification>> GetScheduledAsync(DateTime beforeUtc)
        {
            await using var db = _connectionFactory();

            var models = await db.Notifications
                .Where(n => !n.IsSent && n.ScheduledAt <= beforeUtc)
                .OrderBy(n => n.ScheduledAt)
                .ToListAsync();

            return models.Select(Map).ToList();
        }
    }
}
