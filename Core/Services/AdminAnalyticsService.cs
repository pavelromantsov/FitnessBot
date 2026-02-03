using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Services
{
    public class AdminAnalyticsService
    {
        private readonly IUserRepository _users;
        private readonly IContentItemRepository _content;
        private readonly IErrorLogRepository _errors;

        public AdminAnalyticsService(
            IUserRepository users,
            IContentItemRepository content,
            IErrorLogRepository errors)
        {
            _users = users;
            _content = content;
            _errors = errors;
        }

        public Task<int> GetDailyActiveUsersAsync(DateTime dayUtc)
        {
            var from = dayUtc.Date;
            var to = from.AddDays(1);
            return _users.GetActiveUsersCountAsync(from, to);
        }

        public async Task<IDictionary<string, int>> GetAgeDistributionAsync()
        {
            var all = await _users.GetAllAsync();
            return all
                .Where(u => u.Age.HasValue)
                .GroupBy(u =>
                {
                    var age = u.Age!.Value;
                    if (age < 18) return "<18";
                    if (age < 30) return "18-29";
                    if (age < 45) return "30-44";
                    if (age < 60) return "45-59";
                    return "60+";
                })
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<IDictionary<string, int>> GetGeoDistributionAsync()
        {
            var all = await _users.GetAllAsync();
            return all
                .Where(u => !string.IsNullOrWhiteSpace(u.City))
                .GroupBy(u => u.City!)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public Task<long> GetTotalContentVolumeAsync() => _content.GetTotalSizeAsync();

        public Task LogErrorAsync(string message, string? stackTrace, string level = "Error")
        {
            var log = new ErrorLog
            {
                Message = message,
                StackTrace = stackTrace,
                Level = level,
                Timestamp = DateTime.UtcNow
            };
            return _errors.AddAsync(log);
        }
    }
}
