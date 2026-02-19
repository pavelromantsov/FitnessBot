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

        public Task AddAsync(long userId,
                            int steps,
                            int activeMinutes,
                            double caloriesBurned,
                            string source = "manual",
                            ActivityType type = ActivityType.StepsBased, 
                            DateTime? date = null,
                            string? name = null)
        {
            var activity = new Activity
            {
                UserId = userId,
                Date = date?.Date ?? DateTime.UtcNow.Date,
                Steps = steps,
                ActiveMinutes = activeMinutes,
                CaloriesBurned = caloriesBurned,
                Source = source,
                Type = type,
                Name = name
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
            // Получаем ВСЕ активности за период (без фильтрации по источнику)
            var list = await _activities.GetByUserAndPeriodAsync(userId, from, to);

            // Шаги: только из StepsBased активностей (ходьба/бег)
            var totalSteps = list
                .Where(a => a.Type == ActivityType.StepsBased)
                .Sum(a => a.Steps);

            // Калории: из ВСЕХ активностей (Google Fit + ручные тренировки)
            var totalCalories = list.Sum(a => a.CaloriesBurned);

            return (totalCalories, totalSteps);
        }

        public async Task<IReadOnlyList<Activity>> GetMergedForPeriodAsync(
    long userId,
    DateTime from,
    DateTime to)
        {
            var list = await _activities.GetByUserAndPeriodAsync(userId, from, to);

            var result = list
                .GroupBy(a => a.Date.Date)
                .Select(g =>
                {
                    // суммируем все источники за день

                    return new Activity
                    {
                        UserId = userId,
                        Date = g.Key,

                        // Шаги: суммируем только StepsBased активности (ходьба/бег)
                        Steps = g
                            .Where(a => a.Type == ActivityType.StepsBased)
                            .Sum(a => a.Steps),

                        // Время и калории: суммируем ВСЕ типы активностей
                        ActiveMinutes = g.Sum(a => a.ActiveMinutes),
                        CaloriesBurned = g.Sum(a => a.CaloriesBurned),

                        // Источник: если есть Google Fit — показываем его, иначе manual
                        Source = g.Any(a => a.Source == "googlefit") ? "googlefit" : "manual",

                        // Тип: если есть TimeBased — показываем тренировку
                        Type = g.Any(a => a.Type == ActivityType.TimeBased)
                            ? ActivityType.TimeBased
                            : ActivityType.StepsBased
                    };
                })
                .OrderBy(a => a.Date)
                .ToList();

            return result;
        }
    }

}
