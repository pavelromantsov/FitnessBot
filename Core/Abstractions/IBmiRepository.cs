using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IBmiRepository
    {
        Task SaveAsync(BmiRecord record);
        Task<BmiRecord?> GetLastAsync(long userId);
    }
}
