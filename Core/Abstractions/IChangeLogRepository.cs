using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IChangeLogRepository
    {
        Task AddAsync(ChangeLog log);
    }
}
