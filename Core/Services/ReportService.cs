namespace FitnessBot.Core.Services
{
    public class ReportService
    {
        private readonly ActivityService activityService;
        private readonly NutritionService nutritionService;

        public ReportService(ActivityService activityService, NutritionService nutritionService)
        {
            this.activityService = activityService;
            this.nutritionService = nutritionService;
        }

        public async Task<string> BuildDailySummaryAsync(long userId, DateTime dayUtc)
        {
            var from = dayUtc.Date;
            var to = from.AddDays(1);

            var (caloriesOut, steps) = await activityService.GetMergedTotalsAsync(userId, from, to);
            var (calIn, p, f, c) = await nutritionService.GetTotalsAsync(userId, from, to);

            return
                $"- Шаги: {steps}\n" +
                $"- Калории потрачено: {caloriesOut:F0}\n" +
                $"- Калории съедено: {calIn:F0}\n" +
                $"- БЖУ: {p:F0} / {f:F0} / {c:F0}";
        }



    }
}
