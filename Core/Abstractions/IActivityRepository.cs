using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IActivityRepository
    {
        Task AddAsync(Activity activity);
        Task<IReadOnlyList<Activity>> GetByUserAndPeriodAsync(long userId, DateTime from, DateTime to);
    }
}
