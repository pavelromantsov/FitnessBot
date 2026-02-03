using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IDailyGoalRepository
    {
        Task<DailyGoal?> GetByUserAndDateAsync(long userId, DateTime date);
        Task SaveAsync(DailyGoal goal);
    }
}
