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
    public class EditProfileAgeScenario : IScenario
    {
        private readonly UserService _userService;

        public EditProfileAgeScenario(UserService userService)
        {
            _userService = userService;
        }

        public ScenarioType ScenarioType => ScenarioType.EditProfileAge;

        public bool CanHandle(ScenarioType type) => type == ScenarioType.EditProfileAge;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            if (context.CurrentStep == 0)
            {
                if (!int.TryParse(message.Text, out var age) || age < 10 || age > 120)
                {
                    await bot.SendMessage(
                        message.Chat.Id,
                        "❌ Пожалуйста, введите корректный возраст (от 10 до 120 лет):",
                        cancellationToken: ct);
                    return ScenarioResult.InProgress;
                }

                var user = await _userService.GetByIdAsync(context.UserId);
                if (user != null)
                {
                    await _userService.RegisterOrUpdateAsync(
                        user.TelegramId,
                        user.Name,
                        age,
                        user.City);

                    await bot.SendMessage(
                        message.Chat.Id,
                        $"✅ Возраст успешно изменён на: **{age} лет**",
                        cancellationToken: ct);
                }

                return ScenarioResult.Completed;
            }

            return ScenarioResult.Completed;
        }
    }
}
