using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgNotificationRepository : INotificationRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgNotificationRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory ?? 
                throw new ArgumentNullException(nameof(connectionFactory));
        }

        private static NotificationModel MapToModel(Notification n) => new()
        {
            Id = n.Id,
            UserId = n.UserId,
            Type = n.Type,
            Text = n.Text,
            ScheduledAt = n.ScheduledAt,
            IsSent = n.IsSent,
            SentAt = n.SentAt
        };

        private static Notification MapToEntity(NotificationModel m) => new()
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
            var model = MapToModel(notification);
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

            return models.Select(MapToEntity).ToList();
        }
    }
}
