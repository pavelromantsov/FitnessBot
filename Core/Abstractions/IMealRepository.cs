using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IMealRepository
    {
        Task AddAsync(Meal meal);
        Task<IReadOnlyList<Meal>> GetByUserAndPeriodAsync(long userId, DateTime from, DateTime to);
    }
}
