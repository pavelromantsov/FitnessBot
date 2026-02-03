using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using FitnessBot.Infrastructure.DataAccess;

namespace FitnessBot.Core.Services
{
    public class UserService
    {
        private readonly IUserRepository _users;

        public UserService(IUserRepository users)
        {
            _users = users;
        }

        public Task<User?> GetByTelegramIdAsync(long telegramId) =>
            _users.GetByTelegramIdAsync(telegramId);

        public async Task<User> RegisterOrUpdateAsync(long telegramId, string name, int? age, string? city)
        {
            var user = await _users.GetByTelegramIdAsync(telegramId)
                       ?? new User { TelegramId = telegramId, CreatedAt = DateTime.UtcNow };

            user.Name = name;
            user.Age = age;
            user.City = city;
            user.LastActivityAt = DateTime.UtcNow;

            return await _users.SaveAsync(user);
        }

        public Task<int> GetDailyActiveUsersAsync(DateTime dayUtc)
        {
            var from = dayUtc.Date;
            var to = from.AddDays(1);
            return _users.GetActiveUsersCountAsync(from, to);
        }

        public Task<IReadOnlyList<User>> GetAllAsync() => _users.GetAllAsync();

        public Task<User> SaveAsync(User user) => _users.SaveAsync(user);
    }
}
