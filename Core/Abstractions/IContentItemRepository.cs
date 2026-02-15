using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IContentItemRepository
    {
        Task AddAsync(ContentItem item);
        Task<long> GetTotalSizeAsync();
    }
}
