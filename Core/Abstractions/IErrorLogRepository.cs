using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IErrorLogRepository
    {
        Task AddAsync(ErrorLog log);
    }
}
