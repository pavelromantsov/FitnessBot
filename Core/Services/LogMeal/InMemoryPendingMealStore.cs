using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Core.Services.LogMeal
{
    public class InMemoryPendingMealStore : IPendingMealStore
    {
        private readonly ConcurrentDictionary<long, PendingMeal> _store = new();

        public void Set(PendingMeal meal) => _store[meal.UserId] = meal;

        public PendingMeal? Get(long userId)
            => _store.TryGetValue(userId, out var m) ? m : null;

        public void Clear(long userId) => _store.TryRemove(userId, out _);
    }
}
