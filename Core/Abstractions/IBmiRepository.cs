using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IBmiRepository
    {
        Task SaveAsync(BmiRecord record);
        Task<BmiRecord?> GetLastAsync(long userId);
    }
}
