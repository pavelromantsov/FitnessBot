using FitnessBot.Core.Abstractions;
using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgNutritionRepository : IMealRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgNutritionRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private static MealModel Map(Meal m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            DateTime = m.DateTime,
            MealType = m.MealType,
            Calories = m.Calories,
            Protein = m.Protein,
            Fat = m.Fat,
            Carbs = m.Carbs,
            PhotoUrl = m.PhotoUrl
        };

        private static Meal Map(MealModel m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            DateTime = m.DateTime,
            MealType = m.MealType,
            Calories = m.Calories,
            Protein = m.Protein,
            Fat = m.Fat,
            Carbs = m.Carbs,
            PhotoUrl = m.PhotoUrl
        };

        public async Task AddAsync(Meal meal)
        {
            await using var db = _connectionFactory();
            var model = Map(meal);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            meal.Id = model.Id;
        }

        public async Task<IReadOnlyList<Meal>> GetByUserAndPeriodAsync(long userId, DateTime from, DateTime to)
        {
            await using var db = _connectionFactory();
            var models = await db.Meals
                .Where(m => m.UserId == userId &&
                            m.DateTime >= from &&
                            m.DateTime < to)
                .OrderBy(m => m.DateTime)
                .ToListAsync(); 

            return models.Select(Map).ToList(); 
        }
    }
}
