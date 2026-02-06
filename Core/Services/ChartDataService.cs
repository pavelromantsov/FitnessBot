using FitnessBot.Core.Abstractions;
using FitnessBot.Infrastructure.DataAccess;

namespace FitnessBot.Core.Services
{
    public class ChartDataService
    {
        private readonly IMealRepository _mealRepository;
        private readonly IActivityRepository _activityRepository;

        public ChartDataService(
            IMealRepository mealRepository,
            IActivityRepository activityRepository)
        {
            _mealRepository = mealRepository;
            _activityRepository = activityRepository;
        }

        /// <summary>
        /// Получить данные по калориям за последние N дней
        /// </summary>
        public async Task<(Dictionary<DateTime, double> caloriesIn, Dictionary<DateTime, double> caloriesOut)>
            GetCaloriesDataAsync(long userId, int days = 7)
        {
            var today = DateTime.UtcNow.Date;
            var from = today.AddDays(-days);
            var to = today.AddDays(1);

            var meals = await _mealRepository.GetByUserAndPeriodAsync(userId, from, to);
            var activities = await _activityRepository.GetByUserAndPeriodAsync(userId, from, to);

            var caloriesIn = new Dictionary<DateTime, double>();
            var caloriesOut = new Dictionary<DateTime, double>();

            // Группируем приёмы пищи по дням
            var mealsByDate = meals
                .GroupBy(m => m.DateTime.Date)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.Calories));

            // Группируем активность по дням
            var activitiesByDate = activities
                .GroupBy(a => a.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(a => a.CaloriesBurned));

            // Заполняем данные за каждый день
            for (int i = 0; i < days; i++)
            {
                var date = from.AddDays(i);
                caloriesIn[date] = mealsByDate.ContainsKey(date) ? mealsByDate[date] : 0;
                caloriesOut[date] = activitiesByDate.ContainsKey(date) ? activitiesByDate[date] : 0;
            }

            return (caloriesIn, caloriesOut);
        }

        /// <summary>
        /// Получить данные по шагам за последние N дней
        /// </summary>
        public async Task<Dictionary<DateTime, int>> GetStepsDataAsync(long userId, int days = 7)
        {
            var today = DateTime.UtcNow.Date;
            var from = today.AddDays(-days);
            var to = today.AddDays(1);

            var activitiesList = await _activityRepository.GetByUserAndPeriodAsync(userId, from, to);
            // заменить на:
            // var activitiesList = await activityService.GetMergedForPeriodAsync(userId, from, to);

            var stepsByDate = activitiesList
                .GroupBy(a => a.Date.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(a => a.Steps));

            var result = new Dictionary<DateTime, int>();
            for (int i = 0; i < days; i++)
            {
                var date = from.AddDays(i);
                result[date] = stepsByDate.ContainsKey(date) ? stepsByDate[date] : 0;
            }

            return result;
        }

        /// <summary>
        /// Получить данные по БЖУ за последние N дней
        /// </summary>
        public async Task<Dictionary<DateTime, (double protein, double fat, double carbs)>>
            GetMacrosDataAsync(long userId, int days = 7)
        {
            var today = DateTime.UtcNow.Date;
            var from = today.AddDays(-days);
            var to = today.AddDays(1);

            var meals = await _mealRepository.GetByUserAndPeriodAsync(userId, from, to);

            var macrosByDate = meals
                .GroupBy(m => m.DateTime.Date)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        protein: g.Sum(m => m.Protein),
                        fat: g.Sum(m => m.Fat),
                        carbs: g.Sum(m => m.Carbs)
                    )
                );

            var result = new Dictionary<DateTime, (double, double, double)>();
            for (int i = 0; i < days; i++)
            {
                var date = from.AddDays(i);
                result[date] = macrosByDate.ContainsKey(date)
                    ? macrosByDate[date]
                    : (0, 0, 0);
            }

            return result;
        }
    }
}
