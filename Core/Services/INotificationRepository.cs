using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Services
{
    public interface INotificationRepository
    {
        Task<long> AddAsync(Notification notification);
        Task MarkSentAsync(long id, DateTime sentAt);

        // Непросроченные, неотправленные уведомления до указанного времени
        Task<IReadOnlyList<Notification>> GetScheduledAsync(DateTime beforeUtc);
    }
}
