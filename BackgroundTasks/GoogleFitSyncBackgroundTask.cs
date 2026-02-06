using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Services;
using FitnessBot.Core.Entities;

namespace FitnessBot.BackgroundTasks
{
    public class GoogleFitSyncBackgroundTask : IBackgroundTask
    {
        private readonly UserService _userService;
        private readonly IActivityRepository _activityRepository;
        private readonly GoogleFitClient _googleFitClient;

        public GoogleFitSyncBackgroundTask(
            UserService userService,
            IActivityRepository activityRepository,
            GoogleFitClient googleFitClient)
        {
            _userService = userService;
            _activityRepository = activityRepository;
            _googleFitClient = googleFitClient;
        }

        public async Task Start(CancellationToken ct)
        {
            Console.WriteLine("GoogleFitSyncBackgroundTask started");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await SyncAllUsers(ct);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"GoogleFitSyncBackgroundTask error: {ex}");
                }

                await Task.Delay(TimeSpan.FromMinutes(15), ct);
            }
        }

        private async Task SyncAllUsers(CancellationToken ct)
        {
            var users = await _userService.GetAllAsync();
            var today = DateTime.UtcNow.Date;

            Console.WriteLine($"[GoogleFitSync] SyncAllUsers, today={today:yyyy-MM-dd}, users={users.Count}");

            foreach (var user in users)
            {
                if (ct.IsCancellationRequested)
                    break;

                if (string.IsNullOrWhiteSpace(user.GoogleFitAccessToken))
                {
                    Console.WriteLine($"[GoogleFitSync] user {user.Id}: no token, skip");
                    continue;
                }

                Console.WriteLine($"[GoogleFitSync] user {user.Id}: has token, syncing...");

                try
                {
                    await SyncUserDay(user, today, ct);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"GoogleFitSyncBackgroundTask user {user.Id}: {ex.Message}");
                }
            }
        }

        private async Task SyncUserDay(User user, DateTime dayUtc, CancellationToken ct)
        {
            var accessToken = user.GoogleFitAccessToken;
            var refreshToken = user.GoogleFitRefreshToken;

            Console.WriteLine($"[GoogleFitSync] SyncUserDay user={user.Id}, date={dayUtc:yyyy-MM-dd}, expiresAt={user.GoogleFitTokenExpiresAt:O}");

            if (string.IsNullOrWhiteSpace(accessToken))
                return;

            var now = DateTime.UtcNow;
            var expiresAt = user.GoogleFitTokenExpiresAt ?? now.AddMinutes(-1);

            if (expiresAt <= now && !string.IsNullOrWhiteSpace(refreshToken))
            {
                try
                {
                    var (newAccessToken, newExpiresAt) = await _googleFitClient.RefreshTokenAsync(refreshToken, ct);

                    accessToken = newAccessToken;
                    user.GoogleFitAccessToken = newAccessToken;
                    user.GoogleFitTokenExpiresAt = newExpiresAt;

                    await _userService.SaveAsync(user);
                    Console.WriteLine($"[GoogleFitSync] refresh OK user={user.Id}, newExpires={newExpiresAt:O}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"GoogleFit refresh failed for user {user.Id}: {ex.Message}");
                    return;
                }
            }

            var (steps, calories) = await _googleFitClient.GetDailyActivityAsync(
                accessToken!,
                dayUtc,
                ct);

            Console.WriteLine($"[GoogleFitSync] user={user.Id}, date={dayUtc:yyyy-MM-dd}, steps={steps}, calories={calories:F1}");

            var existing = await _activityRepository
                .GetByUserDateAndSourceAsync(user.Id, dayUtc, "googlefit", ct);

            if (existing != null)
            {
                existing.Steps = steps;
                existing.ActiveMinutes = 0;
                existing.CaloriesBurned = calories;

                await _activityRepository.UpdateAsync(existing, ct);
            }
            else
            {
                var activity = new Activity
                {
                    UserId = user.Id,
                    Date = dayUtc.Date,
                    Steps = steps,
                    ActiveMinutes = 0,
                    CaloriesBurned = calories,
                    Source = "googlefit"
                };

                await _activityRepository.AddAsync(activity);
            }
        }

    }
}
