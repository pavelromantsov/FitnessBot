using FitnessBot.Core.Abstractions;
using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgBmiRepository : IBmiRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgBmiRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        private static BmiRecordModel MapToModel(BmiRecord r) => new()
        {
            Id = r.Id,
            UserId = r.UserId,
            HeightCm = r.HeightCm,
            WeightKg = r.WeightKg,
            Bmi = r.Bmi,
            Category = r.Category,
            Recommendation = r.Recommendation,
            MeasuredAt = r.MeasuredAt
        };

        private static BmiRecord MapToEntity(BmiRecordModel m) => new()
        {
            Id = m.Id,
            UserId = m.UserId,
            HeightCm = m.HeightCm,
            WeightKg = m.WeightKg,
            Bmi = m.Bmi,
            Category = m.Category,
            Recommendation = m.Recommendation,
            MeasuredAt = m.MeasuredAt
        };

        public async Task SaveAsync(BmiRecord record)
        {
            await using var db = _connectionFactory();
            var model = MapToModel(record);

            if (model.Id == 0)
            {
                model.Id = await db.InsertWithInt64IdentityAsync(model);
            }
            else
            {
                await db.UpdateAsync(model);
            }

            record.Id = model.Id;
        }

        public async Task<BmiRecord?> GetLastAsync(long userId)
        {
            await using var db = _connectionFactory();
            var model = await db.BmiRecords
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.MeasuredAt)
                .FirstOrDefaultAsync();

            return model == null ? null : MapToEntity(model);
        }
    }
}
