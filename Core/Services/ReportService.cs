using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Core.Services
{
    public class ReportService
    {
        private readonly ActivityService _activityService;
        private readonly NutritionService _nutritionService;

        public ReportService(ActivityService activityService, NutritionService nutritionService)
        {
            _activityService = activityService;
            _nutritionService = nutritionService;
        }

        public async Task<string> BuildDailySummaryAsync(long userId, DateTime dayUtc)
        {
            var from = dayUtc.Date;
            var to = from.AddDays(1);

            var caloriesOut = await _activityService.GetTotalCaloriesBurnedAsync(userId, from, to);
            var (calIn, p, f, c) = await _nutritionService.GetTotalsAsync(userId, from, to);

            return
                $"Суточный отчёт:\n" +
                $"- Потрачено калорий: {caloriesOut:F0}\n" +
                $"- Потреблено калорий: {calIn:F0}\n" +
                $"- Б: {p:F0} г, Ж: {f:F0} г, У: {c:F0} г\n";
        }
    }
}
