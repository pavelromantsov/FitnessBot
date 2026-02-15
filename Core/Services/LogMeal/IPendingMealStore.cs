using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Core.Services.LogMeal
{
    public interface IPendingMealStore
    {
        void Set(PendingMeal meal);
        PendingMeal? Get(long userId);
        void Clear(long userId);
    }
}
