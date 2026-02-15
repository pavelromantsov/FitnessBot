using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IDailyGoalRepository
    {
        Task<DailyGoal?> GetByUserAndDateAsync(long userId, DateTime date);
        Task SaveAsync(DailyGoal goal);
    }
}
