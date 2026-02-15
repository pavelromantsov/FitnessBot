using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IChangeLogRepository
    {
        Task AddAsync(ChangeLog changeLog);
        Task<List<ChangeLog>> GetRecentAsync(int count);
        Task<List<ChangeLog>> GetByAdminIdAsync(long adminUserId, int count);
        Task<int> GetCountAsync();
    }
}
