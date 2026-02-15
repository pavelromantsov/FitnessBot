using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace FitnessBot.Scenarios
{
    public class EditProfileNameScenario : IScenario
    {
        private readonly UserService _userService;

        public EditProfileNameScenario(UserService userService)
        {
            _userService = userService;
        }

        public ScenarioType ScenarioType => ScenarioType.EditProfileName;

        public bool CanHandle(ScenarioType type) => type == ScenarioType.EditProfileName;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            if (context.CurrentStep == 0)
            {
                var newName = message.Text?.Trim();

                if (string.IsNullOrEmpty(newName) || newName.Length < 2)
                {
                    await bot.SendMessage(
                        message.Chat.Id,
                        "❌ Имя должно содержать минимум 2 символа. Попробуйте ещё раз:",
                        cancellationToken: ct);
                    return ScenarioResult.InProgress;
                }

                // Обновляем имя
                var user = await _userService.GetByIdAsync(context.UserId);
                if (user != null)
                {
                    await _userService.RegisterOrUpdateAsync(
                        user.TelegramId,
                        newName,
                        user.Age,
                        user.City);

                    await bot.SendMessage(
                        message.Chat.Id,
                        $"✅ Имя успешно изменено на: **{newName}**",
                        cancellationToken: ct);
                }

                return ScenarioResult.Completed;
            }

            return ScenarioResult.Completed;
        }
    }
}
