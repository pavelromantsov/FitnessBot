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
    public class EditProfileCityScenario : IScenario
    {
        private readonly UserService _userService;

        public EditProfileCityScenario(UserService userService)
        {
            _userService = userService;
        }

        public ScenarioType ScenarioType => ScenarioType.EditProfileCity;

        public bool CanHandle(ScenarioType type) => type == ScenarioType.EditProfileCity;

        public async Task<ScenarioResult> HandleMessageAsync(
            ITelegramBotClient bot,
            ScenarioContext context,
            Message message,
            CancellationToken ct)
        {
            if (context.CurrentStep == 0)
            {
                var newCity = message.Text?.Trim();

                if (string.IsNullOrEmpty(newCity))
                {
                    await bot.SendMessage(
                        message.Chat.Id,
                        "❌ Название города не может быть пустым. Попробуйте ещё раз:",
                        cancellationToken: ct);
                    return ScenarioResult.InProgress;
                }

                var user = await _userService.GetByIdAsync(context.UserId);
                if (user != null)
                {
                    await _userService.RegisterOrUpdateAsync(
                        user.TelegramId,
                        user.Name,
                        user.Age,
                        newCity);

                    await bot.SendMessage(
                        message.Chat.Id,
                        $"✅ Город успешно изменён на: **{newCity}**",
                        cancellationToken: ct);
                }

                return ScenarioResult.Completed;
            }

            return ScenarioResult.Completed;
        }
    }
}
