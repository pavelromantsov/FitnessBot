using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Services
{
    public class NutritionService
    {
        private readonly IMealRepository _meals;

        public NutritionService(IMealRepository meals)
        {
            _meals = meals;
        }

        public Task AddMealAsync(long userId, DateTime at, string mealType,
            double calories, double protein, double fat, double carbs, string? photoUrl)
        {
            var meal = new Meal
            {
                UserId = userId,
                DateTime = at,
                MealType = mealType,
                Calories = calories,
                Protein = protein,
                Fat = fat,
                Carbs = carbs,
                PhotoUrl = photoUrl
            };

            return _meals.AddAsync(meal);
        }

        public Task<IReadOnlyList<Meal>> GetMealsAsync(long userId, DateTime from, DateTime to) =>
            _meals.GetByUserAndPeriodAsync(userId, from, to);

        public async Task<(double calories, double protein, double fat, double carbs)>
            GetTotalsAsync(long userId, DateTime from, DateTime to)
        {
            var meals = await _meals.GetByUserAndPeriodAsync(userId, from, to);

            return (
                meals.Sum(m => m.Calories),
                meals.Sum(m => m.Protein),
                meals.Sum(m => m.Fat),
                meals.Sum(m => m.Carbs)
            );
        }
        public async Task AddMealAsync(Meal meal, CancellationToken ct = default)
        {

            if (meal.Calories <= 0) throw new ArgumentException("Калории должны быть > 0");
            await _meals.AddAsync(meal);
        }

        public Task<IReadOnlyList<Meal>> GetMealsByUserAndPeriodAsync
            (long userId, DateTime from, DateTime to, CancellationToken ct) =>
            _meals.GetByUserAndPeriodAsync(userId, from, to);
    }
}
