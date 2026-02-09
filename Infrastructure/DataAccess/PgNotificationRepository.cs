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

        private static DailyGoalModel MapDailyGoal(DailyGoal g) => new()
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

        private static DailyGoal MapDailyGoal(DailyGoalModel m) => new()
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
                .FirstOrDefaultAsync();

            return model == null ? null : MapDailyGoal(model);
        }

        public async Task SaveAsync(DailyGoal goal)
        {
            await using var db = _connectionFactory();

            var model = MapDailyGoal(goal);

            var existingModel = await db.DailyGoals
                .Where(g => g.UserId == model.UserId && g.Date == model.Date)
                .FirstOrDefaultAsync();

            if (existingModel != null)
            {
                model.Id = existingModel.Id;
                await db.UpdateAsync(model);
                goal.Id = model.Id;
            }
            else
            {
                model.Id = await db.InsertWithInt64IdentityAsync(model);
                goal.Id = model.Id;
            }
        }

        // ---------- BMI (IBmiRepository) ----------

        private static BmiRecordModel MapBmiRecord(BmiRecord r) => new()
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

        private static BmiRecord MapBmiRecord(BmiRecordModel m) => new()
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
            var model = MapBmiRecord(record);

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

            return model == null ? null : MapBmiRecord(model);
        }

        // ---------- ErrorLog (IErrorLogRepository) ----------

        private static ErrorLogModel MapErrorLog(ErrorLog e) => new()
        {
            Id = e.Id,
            UserId = e.UserId,
            Timestamp = e.Timestamp,
            Level = e.Level,
            Message = e.Message,
            StackTrace = e.StackTrace,
            ContextJson = null
        };

        private static ErrorLog MapErrorLog(ErrorLogModel m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            Timestamp = m.Timestamp,
            Level = m.Level,
            Message = m.Message,
            StackTrace = m.StackTrace
        };

        async Task IErrorLogRepository.AddAsync(ErrorLog log)
        {
            await using var db = _connectionFactory();
            var model = MapErrorLog(log);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            log.Id = model.Id;
        }

        async Task<List<ErrorLog>> IErrorLogRepository.GetRecentAsync(int count)
        {
            await using var db = _connectionFactory();

            var models = await db.ErrorLogs
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(MapErrorLog).ToList();
        }

        async Task<List<ErrorLog>> IErrorLogRepository.GetByUserIdAsync(long userId, int count)
        {
            await using var db = _connectionFactory();

            var models = await db.ErrorLogs
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(MapErrorLog).ToList();
        }

        async Task<int> IErrorLogRepository.GetCountAsync()
        {
            await using var db = _connectionFactory();
            return await db.ErrorLogs.CountAsync();
        }

        // ---------- ChangeLog (IChangeLogRepository) ----------

        private static ChangeLogModel MapChangeLog(ChangeLog c) => new()
        {
            Id = c.Id,
            AdminUserId = c.AdminUserId,
            Timestamp = c.Timestamp,
            ChangeType = c.ChangeType,
            Details = c.Details
        };

        private static ChangeLog MapChangeLog(ChangeLogModel m) => new()
        {
            Id = m.Id,
            AdminUserId = m.AdminUserId,
            Timestamp = m.Timestamp,
            ChangeType = m.ChangeType,
            Details = m.Details
        };

        async Task IChangeLogRepository.AddAsync(ChangeLog log)
        {
            await using var db = _connectionFactory();
            var model = MapChangeLog(log);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            log.Id = model.Id;
        }

        async Task<List<ChangeLog>> IChangeLogRepository.GetRecentAsync(int count)
        {
            await using var db = _connectionFactory();

            var models = await db.ChangeLogs
                .OrderByDescending(c => c.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(MapChangeLog).ToList();
        }

        async Task<List<ChangeLog>> IChangeLogRepository.GetByAdminIdAsync(long adminUserId, int count)
        {
            await using var db = _connectionFactory();

            var models = await db.ChangeLogs
                .Where(c => c.AdminUserId == adminUserId)
                .OrderByDescending(c => c.Timestamp)
                .Take(count)
                .ToListAsync();

            return models.Select(MapChangeLog).ToList();
        }

        async Task<int> IChangeLogRepository.GetCountAsync()
        {
            await using var db = _connectionFactory();
            return await db.ChangeLogs.CountAsync();
        }

        // ---------- ContentItem (IContentItemRepository) ----------

        private static ContentItemModel MapContentItem(ContentItem c) => new()
        {
            Id = c.Id,
            UserId = c.UserId,
            ContentType = c.ContentType,
            SizeBytes = c.SizeBytes,
            CreatedAt = c.CreatedAt,
            ExternalUrl = c.ExternalUrl
        };

        private static ContentItem MapContentItem(ContentItemModel m) => new()
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
            var model = MapContentItem(item);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            item.Id = model.Id;
        }

        public async Task<long> GetTotalSizeAsync()
        {
            await using var db = _connectionFactory();
            return await db.ContentItems
                       .SumAsync(ci => (long?)ci.SizeBytes) ?? 0L;
        }

        // ---------- Notification (INotificationRepository) ----------

        private static NotificationModel MapNotification(Notification n) => new()
        {
            Id = n.Id,
            UserId = n.UserId,
            Type = n.Type,
            Text = n.Text,
            ScheduledAt = n.ScheduledAt,
            IsSent = n.IsSent,
            SentAt = n.SentAt
        };

        private static Notification MapNotification(NotificationModel m) => new()
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
            var model = MapNotification(notification);
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

            return models.Select(MapNotification).ToList();
        }
    }
}
