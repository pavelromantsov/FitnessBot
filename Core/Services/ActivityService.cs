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

        public async Task<(double caloriesOut, int steps)> GetMergedTotalsAsync(
                    long userId,
                    DateTime from,
                    DateTime to)
        {
            var list = await _activities.GetByUserAndPeriodAsync(userId, from, to);

            var grouped = list
                .GroupBy(a => a.Date.Date)
                .Select(g =>
                {
                    var google = g.Where(a => a.Source == "googlefit").ToList();
                    var selected = google.Any() ? google : g.ToList();

                    return new
                    {
                        Date = g.Key,
                        Steps = selected.Sum(a => a.Steps),
                        Calories = selected.Sum(a => a.CaloriesBurned)
                    };
                })
                .ToList();

            var totalCalories = grouped.Sum(x => x.Calories);
            var totalSteps = grouped.Sum(x => x.Steps);

            return (totalCalories, totalSteps);
        }



        public async Task<IReadOnlyList<Activity>> GetMergedForPeriodAsync(long userId, DateTime from, DateTime to)
        {
            var list = await _activities.GetByUserAndPeriodAsync(userId, from, to);

            var result = list
                .GroupBy(a => a.Date.Date)
                .Select(g =>
                {
                    var google = g.Where(a => a.Source == "googlefit").ToList();
                    var selected = google.Any() ? google : g.ToList();

                    return new Activity
                    {
                        UserId = userId,
                        Date = g.Key,
                        Steps = selected.Sum(a => a.Steps),
                        ActiveMinutes = selected.Sum(a => a.ActiveMinutes),
                        CaloriesBurned = selected.Sum(a => a.CaloriesBurned),
                        Source = google.Any() ? "googlefit" : "manual"
                    };
                })
                .OrderBy(a => a.Date)
                .ToList();

            return result;
        }
    }

}
