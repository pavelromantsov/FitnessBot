using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IErrorLogRepository
    {
        Task AddAsync(ErrorLog errorLog);
        Task<List<ErrorLog>> GetRecentAsync(int count);
        Task<List<ErrorLog>> GetByUserIdAsync(long userId, int count);
        Task<int> GetCountAsync();
    }
}
