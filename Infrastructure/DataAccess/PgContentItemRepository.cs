using System;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using LinqToDB;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgContentItemRepository : IContentItemRepository
    {
        private readonly Func<PgDataContext> _dataContextFactory;

        public PgContentItemRepository(Func<PgDataContext> dataContextFactory)
        {
            _dataContextFactory = dataContextFactory;
        }

        public Task AddAsync(ContentItem contentItem)
        {
            // Если у вас нет таблицы content_items, просто заглушка
            return Task.CompletedTask;
        }

        public Task<long> GetTotalSizeAsync()
        {
            // Возвращаем 0, если таблицы нет
            return Task.FromResult(0L);
        }
    }
}

