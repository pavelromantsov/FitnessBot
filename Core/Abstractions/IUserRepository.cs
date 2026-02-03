using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Abstractions
{
    public interface IUserRepository
    {
        Task<User?> GetByTelegramIdAsync(long telegramId);
        Task<User?> GetByIdAsync(long id);
        Task<User> SaveAsync(User user);
        Task<int> GetActiveUsersCountAsync(DateTime from, DateTime to);
        Task UpdateLastActivityAsync(long userId, DateTime at);
        Task<IReadOnlyList<User>> GetAllAsync();
        Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken ct);
        Task<IReadOnlyList<User>> FindByNameAsync(string namePart);
    }
}
