using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IActivityRepository
    {
        Task AddAsync(Activity activity);
        Task<IReadOnlyList<Activity>> GetByUserAndPeriodAsync(long userId, DateTime from, DateTime to);
    }
}
