using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class ReportCallbackHandler : ICallbackHandler
    {
        private readonly IMealRepository _mealRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly ReportService _reportService;

        public ReportCallbackHandler(
            IMealRepository mealRepository,
            IActivityRepository activityRepository,
            ReportService reportService)
        {
            _mealRepository = mealRepository;
            _activityRepository = activityRepository;
            _reportService = reportService;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string data)
        {
            // Обрабатываем только callback'и, связанные с календарём и отчётами
            if (!data.StartsWith("cal_") && !data.StartsWith("report_"))
                return false;

            if (data == "cal_ignore")
            {
                await context.Bot.AnswerCallbackQuery(
                    context.CallbackQuery!.Id,
                    cancellationToken: default);
                return true;
            }

            if (data.StartsWith("cal_prev:"))
            {
                await HandlePrevMonth(context, data);
                return true;
            }

            if (data.StartsWith("cal_next:"))
            {
                await HandleNextMonth(context, data);
                return true;
            }

            if (data == "report_today")
            {
                await ShowReportForDate(context, DateTime.UtcNow.Date);
                return true;
            }

            if (data.StartsWith("report_date:"))
            {
                await HandleDateSelection(context, data);
                return true;
            }

            return false;
        }

        private async Task HandlePrevMonth(UpdateContext context, string data)
        {
            var parts = data.Split(':');
            var year = int.Parse(parts[1]);
            var month = int.Parse(parts[2]);

            month--;
            if (month < 1)
            {
                month = 12;
                year--;
            }

            var keyboard = CreateCalendarKeyboard(year, month);

            await context.Bot.EditMessageReplyMarkup(
                context.ChatId,
                context.CallbackQuery!.Message!.MessageId,
                replyMarkup: keyboard,
                cancellationToken: default);

            await context.Bot.AnswerCallbackQuery(
                context.CallbackQuery.Id,
                cancellationToken: default);
        }

        private async Task HandleNextMonth(UpdateContext context, string data)
        {
            var parts = data.Split(':');
            var year = int.Parse(parts[1]);
            var month = int.Parse(parts[2]);

            month++;
            if (month > 12)
            {
                month = 1;
                year++;
            }

            var keyboard = CreateCalendarKeyboard(year, month);

            await context.Bot.EditMessageReplyMarkup(
                context.ChatId,
                context.CallbackQuery!.Message!.MessageId,
                replyMarkup: keyboard,
                cancellationToken: default);

            await context.Bot.AnswerCallbackQuery(
                context.CallbackQuery.Id,
                cancellationToken: default);
        }

        private async Task HandleDateSelection(UpdateContext context, string data)
        {
            var parts = data.Split(':');
            var year = int.Parse(parts[1]);
            var month = int.Parse(parts[2]);
            var day = int.Parse(parts[3]);

            var selectedDate = new DateTime(year, month, day);
            await ShowReportForDate(context, selectedDate);
        }

        private async Task ShowReportForDate(UpdateContext context, DateTime date)
        {
            var text = await _reportService.BuildDailySummaryAsync(context.User.Id, date);

            // Удаляем календарь
            await context.Bot.DeleteMessage(
                context.ChatId,
                context.CallbackQuery!.Message!.MessageId,
                cancellationToken: default);

            // Отправляем отчёт
            await context.Bot.SendMessage(
                context.ChatId,
                $"📈 **Отчёт за {date:dd.MM.yyyy}**\n\n{text}",
                cancellationToken: default);

            await context.Bot.AnswerCallbackQuery(
                context.CallbackQuery.Id,
                cancellationToken: default);
        }

        private InlineKeyboardMarkup CreateCalendarKeyboard(int year, int month)
        {
            var firstDay = new DateTime(year, month, 1);
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var startDayOfWeek = (int)firstDay.DayOfWeek;
            if (startDayOfWeek == 0) startDayOfWeek = 7;

            var buttons = new System.Collections.Generic.List<InlineKeyboardButton[]>();

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("◀️", $"cal_prev:{year}:{month}"),
                InlineKeyboardButton.WithCallbackData($"{GetMonthName(month)} {year}", 
                "cal_ignore"),
                InlineKeyboardButton.WithCallbackData("▶️", $"cal_next:{year}:{month}")
            });

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("Пн", "cal_ignore"),
                InlineKeyboardButton.WithCallbackData("Вт", "cal_ignore"),
                InlineKeyboardButton.WithCallbackData("Ср", "cal_ignore"),
                InlineKeyboardButton.WithCallbackData("Чт", "cal_ignore"),
                InlineKeyboardButton.WithCallbackData("Пт", "cal_ignore"),
                InlineKeyboardButton.WithCallbackData("Сб", "cal_ignore"),
                InlineKeyboardButton.WithCallbackData("Вс", "cal_ignore")
            });

            var currentWeek = new System.Collections.Generic.List<InlineKeyboardButton>();

            for (int i = 1; i < startDayOfWeek; i++)
            {
                currentWeek.Add(InlineKeyboardButton.WithCallbackData(" ", "cal_ignore"));
            }

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                var isToday = date == DateTime.UtcNow.Date;
                var isFuture = date > DateTime.UtcNow.Date;

                string buttonText = isToday ? $"[{day}]" : day.ToString();
                string callbackData = isFuture ? "cal_ignore" : 
                    $"report_date:{year}:{month}:{day}";

                currentWeek.Add(InlineKeyboardButton.WithCallbackData(buttonText, callbackData));

                if ((startDayOfWeek + day - 1) % 7 == 0)
                {
                    buttons.Add(currentWeek.ToArray());
                    currentWeek = new System.Collections.Generic.List<InlineKeyboardButton>();
                }
            }

            if (currentWeek.Count > 0)
            {
                while (currentWeek.Count < 7)
                {
                    currentWeek.Add(InlineKeyboardButton.WithCallbackData(" ", "cal_ignore"));
                }
                buttons.Add(currentWeek.ToArray());
            }

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("📅 Сегодня", "report_today")
            });

            return new InlineKeyboardMarkup(buttons);
        }

        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "Январь",
                2 => "Февраль",
                3 => "Март",
                4 => "Апрель",
                5 => "Май",
                6 => "Июнь",
                7 => "Июль",
                8 => "Август",
                9 => "Сентябрь",
                10 => "Октябрь",
                11 => "Ноябрь",
                12 => "Декабрь",
                _ => "???"
            };
        }
    }
}
