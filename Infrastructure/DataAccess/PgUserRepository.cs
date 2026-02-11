using FitnessBot.Core.Abstractions;
using FitnessBot.Core.DataAccess.Models;
using FitnessBot.Core.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgUserRepository : IUserRepository
    {
        private readonly Func<PgDataContext> _connectionFactory;

        public PgUserRepository(Func<PgDataContext> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private static User Map(UserModel m) => new User
        {
            Id = m.Id,
            TelegramId = m.TelegramId,
            Name = m.Name,
            Role = Enum.TryParse<UserRole>(m.RoleRaw, out var r) ? r : UserRole.User,
            Age = m.Age,
            City = m.City,
            CreatedAt = m.CreatedAt,
            LastActivityAt = m.LastActivityAt,
            BreakfastTime = m.BreakfastTime,
            LunchTime = m.LunchTime,
            DinnerTime = m.DinnerTime,
            HeightCm = m.HeightCm,
            WeightKg = m.WeightKg,
            ActivityRemindersEnabled = m.ActivityRemindersEnabled,
            MorningReminderEnabled = m.MorningReminderEnabled,
            LunchReminderEnabled = m.LunchReminderEnabled,
            AfternoonReminderEnabled = m.AfternoonReminderEnabled,
            EveningReminderEnabled = m.EveningReminderEnabled,
            GoogleFitAccessToken = m.GoogleFitAccessToken,
            GoogleFitRefreshToken = m.GoogleFitRefreshToken,
            GoogleFitTokenExpiresAt = m.GoogleFitTokenExpiresAt
        };

        private static UserModel Map(User u) => new UserModel
        {
            Id = u.Id,
            TelegramId = u.TelegramId,
            Name = u.Name,
            RoleRaw = u.Role.ToString(),
            Age = u.Age,
            City = u.City,
            CreatedAt = u.CreatedAt,
            LastActivityAt = u.LastActivityAt,
            BreakfastTime = u.BreakfastTime,
            LunchTime = u.LunchTime,
            DinnerTime = u.DinnerTime,
            HeightCm = u.HeightCm,
            WeightKg = u.WeightKg,
            ActivityRemindersEnabled = u.ActivityRemindersEnabled,
            MorningReminderEnabled = u.MorningReminderEnabled,
            LunchReminderEnabled = u.LunchReminderEnabled,
            AfternoonReminderEnabled = u.AfternoonReminderEnabled,
            EveningReminderEnabled = u.EveningReminderEnabled,
            GoogleFitAccessToken = u.GoogleFitAccessToken,
            GoogleFitRefreshToken = u.GoogleFitRefreshToken,
            GoogleFitTokenExpiresAt = u.GoogleFitTokenExpiresAt
        };

        public async Task<User?> GetByTelegramIdAsync(long telegramId)
        {
            await using var db = _connectionFactory();
            var model = await db.Users
                .Where(u => u.TelegramId == telegramId)
                .FirstOrDefaultAsync();

            return model == null ? null : Map(model);
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            await using var db = _connectionFactory();
            var model = await db.Users
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync();

            return model == null ? null : Map(model);
        }

        public async Task<User> SaveAsync(User user)
        {
            await using var db = _connectionFactory();

            // ищем существующую запись по telegram_id
            var existingModel = await db.Users
                .FirstOrDefaultAsync(u => u.TelegramId == user.TelegramId);

            UserModel model;
            

            if (existingModel == null)
            {
                // новый пользователь
                model = Map(user);
                model.CreatedAt = DateTime.UtcNow;
                model.LastActivityAt = DateTime.UtcNow;
                model.HeightCm=user.HeightCm;
                model.WeightKg=user.WeightKg;

                model.Id = await db.InsertWithInt64IdentityAsync(model);
            }
            else
            {
                // обновление существующего
                model = existingModel;
                model.Name = user.Name;
                model.RoleRaw = user.Role.ToString();
                model.Age = user.Age;
                model.City = user.City;
                model.LastActivityAt = DateTime.UtcNow;
                model.BreakfastTime = user.BreakfastTime;
                model.LunchTime = user.LunchTime;
                model.DinnerTime = user.DinnerTime;
                model.HeightCm = user.HeightCm;
                model.WeightKg = user.WeightKg;
                model.GoogleFitAccessToken = user.GoogleFitAccessToken;
                model.GoogleFitRefreshToken = user.GoogleFitRefreshToken;
                model.GoogleFitTokenExpiresAt = user.GoogleFitTokenExpiresAt;
                await db.UpdateAsync(model);
            }

            // синхронизируем доменную сущность
            user.Id = model.Id;
            user.CreatedAt = model.CreatedAt;
            user.LastActivityAt = model.LastActivityAt;

            return user;
        }

        public async Task<int> GetActiveUsersCountAsync(DateTime from, DateTime to)
        {
            await using var db = _connectionFactory();
            return await db.Users
                .Where(u => u.LastActivityAt >= from && u.LastActivityAt < to)
                .CountAsync();
        }

        public async Task UpdateLastActivityAsync(long userId, DateTime at)
        {
            await using var db = _connectionFactory();
            await db.Users
                .Where(u => u.Id == userId)
                .Set(u => u.LastActivityAt, at)
                .UpdateAsync();
        }

        public async Task<IReadOnlyList<User>> GetAllAsync()
        {
            await using var db = _connectionFactory();
            var models = await db.Users.ToListAsync();
            return models.Select(Map).ToList();
        }
        public async Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken ct)
        {
            await using var db = _connectionFactory();

            var model = await db.Users
                .FirstOrDefaultAsync(u => u.TelegramId == telegramId, ct);

            return model == null ? null : Map(model);
        }
        public async Task<IReadOnlyList<User>> FindByNameAsync(string namePart)
        {
            await using var db = _connectionFactory();

            var models = await db.Users
                .Where(u => u.Name.ToLower().Contains(namePart.ToLower()))
                .OrderBy(u => u.Name)
                .Take(20)
                .ToListAsync();

            return models.Select(Map).ToList();
        }
        public async Task UpdateAsync(User user)
        {
            await using var db = _connectionFactory();
            var model = Map(user);
            await db.UpdateAsync(model);
        }
    }
}
