using System;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgContentItemRepository : IContentItemRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgContentItemRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        private static ContentItemModel MapToModel(ContentItem c) => new()
        {
            Id = c.Id,
            UserId = c.UserId,
            ContentType = c.ContentType,
            SizeBytes = c.SizeBytes,
            CreatedAt = c.CreatedAt,
            ExternalUrl = c.ExternalUrl
        };

        public async Task AddAsync(ContentItem item)
        {
            await using var db = _connectionFactory();
            var model = MapToModel(item);
            model.Id = await db.InsertWithInt64IdentityAsync(model);
            item.Id = model.Id;
        }

        public async Task<long> GetTotalSizeAsync()
        {
            await using var db = _connectionFactory();
            return await db.ContentItems
                       .SumAsync(ci => (long?)ci.SizeBytes) ?? 0L;
        }
    }
}

