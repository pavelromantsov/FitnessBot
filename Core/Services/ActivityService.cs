using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Services
{
    public class ActivityService
    {
        private readonly IActivityRepository _activities;

        public ActivityService(IActivityRepository activities)
        {
            _activities = activities;
        }

        public Task AddAsync(long userId, int steps, int activeMinutes, double caloriesBurned, string source)
        {
            var activity = new Activity
            {
                UserId = userId,
                Date = DateTime.UtcNow.Date,
                Steps = steps,
                ActiveMinutes = activeMinutes,
                CaloriesBurned = caloriesBurned,
                Source = source
            };

            return _activities.AddAsync(activity);
        }

        public Task<IReadOnlyList<Activity>> GetForPeriodAsync(long userId, DateTime from, DateTime to) =>
            _activities.GetByUserAndPeriodAsync(userId, from, to);

        public async Task<double> GetTotalCaloriesBurnedAsync(long userId, DateTime from, DateTime to)
        {
            var list = await _activities.GetByUserAndPeriodAsync(userId, from, to);
            return list.Sum(a => a.CaloriesBurned);
        }
    }

}
