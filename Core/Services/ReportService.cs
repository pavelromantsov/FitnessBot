using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Services
{
    public class ReportService
    {
        private readonly IMealRepository _mealRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly IDailyGoalRepository _dailyGoalRepository; 

        public ReportService(
            IMealRepository mealRepository,
            IActivityRepository activityRepository,
            IDailyGoalRepository dailyGoalRepository) 
        {
            _mealRepository = mealRepository;
            _activityRepository = activityRepository;
            _dailyGoalRepository = dailyGoalRepository; 
        }

        public async Task<string> BuildDailySummaryAsync(long userId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var meals = await _mealRepository.GetByUserAndPeriodAsync(userId, startDate, endDate);
            var activities = await _activityRepository.GetByUserAndPeriodAsync(userId, startDate, endDate);

            var totalCaloriesIn = meals.Sum(m => m.Calories);
            var totalCaloriesOut = activities.Sum(a => a.CaloriesBurned);
            var totalSteps = activities.Sum(a => a.Steps);

            return
                $"Калории: {totalCaloriesIn:F0} (съедено) / {totalCaloriesOut:F0} (потрачено)\n" +
                $"Шаги: {totalSteps}\n" +
                $"Баланс: {(totalCaloriesIn - totalCaloriesOut):F0} ккал";
        }

        public async Task<DailyGoal?> GetDailyGoalAsync(long userId, DateTime date)
        {
            return await _dailyGoalRepository.GetByUserAndDateAsync(userId, date.Date);
        }
    }
}
