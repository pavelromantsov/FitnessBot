using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Services;
using FitnessBot.Scenarios;
using Telegram.Bot;

namespace FitnessBot.TelegramBot.Handlers
{
    public class ActivityCallbackHandler : ICallbackHandler
    {
        private readonly IScenarioContextRepository _contextRepository;

        public ActivityCallbackHandler(IScenarioContextRepository contextRepository)
        {
            _contextRepository = contextRepository;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string callbackData)
        {
            if (!callbackData.StartsWith("act_type:"))
                return false;

            var type = callbackData.Split(':')[1]; 
            var cb = context.CallbackQuery;
            if (cb == null)
                return false;

            var callbackId = cb.Id;
            var chatId = cb.Message!.Chat.Id;
            var messageId = cb.Message.MessageId;

            // 1. Получаем и обновляем контекст сценария
            var scenarioContext = await _contextRepository.GetContext(
                context.User.Id,
                context.CancellationToken);

            if (scenarioContext == null)
                return false;

            scenarioContext.Data["activityType"] = type;
            scenarioContext.CurrentStep = 1;

            await _contextRepository.SetContext(
                context.User.Id,
                scenarioContext,
                context.CancellationToken);

            // 2. Убираем «часики» в Telegram
            await context.Bot.AnswerCallbackQuery(
                callbackId,
                $"✅ Выбрано: {(type == "steps" ? "👣 Шаги" : "🏋️ Тренировка")}",
                cancellationToken: context.CancellationToken);

            // 3. редактируем сообщение и просим ввести минуты
            await context.Bot.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: $"✅ Выбрано: {(type == "steps" ? "👣 Шаги" : "🏋️ Тренировка")}\n\n" +
                      $"⏱️ Введите длительность в минутах:",
                cancellationToken: context.CancellationToken);

            return true;
        }
    }
}
