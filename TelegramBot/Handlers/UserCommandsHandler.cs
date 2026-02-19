using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;
using FitnessBot.Core.Services;
using FitnessBot.Scenarios;

using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class UserCommandsHandler : ICommandHandler
    {
        private readonly BmiService _bmiService;
        private readonly IMealRepository _mealRepository;
        private readonly ActivityService _activityService;
        private readonly ReportService _reportService;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly List<IScenario> _scenarios;

        public UserCommandsHandler(
            BmiService bmiService,
            IMealRepository mealRepository,
            ActivityService activityService,
            ReportService reportService,
            IScenarioContextRepository contextRepository,
            IEnumerable<IScenario> scenarios)
        {
            _bmiService = bmiService;
            _mealRepository = mealRepository;
            _activityService = activityService;
            _reportService = reportService;
            _contextRepository = contextRepository;
            _scenarios = scenarios.ToList();
        }

        public async Task<bool> HandleAsync(UpdateContext context, string command, string[] args)
        {
            var normalizedCommand = command.Trim().ToLowerInvariant();

            Console.WriteLine($"DEBUG: '{normalizedCommand}'");

            if (normalizedCommand.Contains("помощь") || normalizedCommand == "/help")
            {
                await HelpCommand(context);
                return true;
            }

            if (normalizedCommand.Contains("сегодня") || normalizedCommand == "/today")
            {
                await TodayCommand(context);
                return true;
            }

            if (normalizedCommand.Contains("добавить еду") || normalizedCommand == "/addcalories")
            {
                await ShowAddCaloriesMenuAsync(context);
                return true;
            }

            if (normalizedCommand.Contains("добавить активность") || normalizedCommand == "/addactivity")
            {
                await StartManualActivityScenario(context); // твой метод запуска ManualActivityScenario
                return true;
            }

            if (normalizedCommand.Contains("приём пищи") || normalizedCommand.Contains("прием пищи") || normalizedCommand == "/addmeal")
            {
                await StartAddMealScenario(context);
                return true;
            }

            if (normalizedCommand.Contains("отчёт") || normalizedCommand.Contains("отчет") || normalizedCommand == "/report")
            {
                await ReportCommand(context);
                return true;
            }

            if (normalizedCommand.Contains("имт") || normalizedCommand == "/bmi")
            {
                await ShowBmiFromProfile(context);
                return true;
            }

            if (normalizedCommand.Contains("распознать блюдо по фото") || normalizedCommand == "/foodphoto")
            {
                await StartFoodPhotoFlow(context);
                return true;
            }

            if (normalizedCommand.Contains("цель дня") || normalizedCommand == "/setgoal")
            {
                await StartSetDailyGoalScenario(context);
                return true;
            }

            if (normalizedCommand.Contains("время питания") || normalizedCommand == "/setmeals")
            {
                await StartMealTimeSetupAsync(context);
                return true;
            }

            if (normalizedCommand.Contains("напоминания") || normalizedCommand == "/activity_reminders")
            {
                await StartActivityReminderSettingsScenario(context);
                return true;
            }

            if (normalizedCommand.Contains("графики") || normalizedCommand == "/charts")
            {
                await ChartsMenuCommand(context);
                return true;
            }

            if (normalizedCommand.Contains("профиль") || normalizedCommand == "/edit_profile")
            {
                await ProfileCommand(context);
                return true;
            }

            if (normalizedCommand.Contains("google fit") || normalizedCommand == "/connectgooglefit")
            {
                await StartConnectGoogleFitScenario(context);
                return true;
            }

            if (normalizedCommand == "/start")
            {
                await StartCommand(context);
                return true;
            }

            if (normalizedCommand.Contains("админ"))
            {
                return false;
            }

            Console.WriteLine("DEBUG: Команда не распознана");
            return false;
        }

        private async Task StartCommand(UpdateContext ctx)
        {
            var rows = new List<List<KeyboardButton>>
    {
        // Самое частое: сегодня + быстрые действия
        new()
        {
            new KeyboardButton("📊 Сегодня"),
            new KeyboardButton("🍽️ Добавить еду")
        },
        new()
        {
            new KeyboardButton("🏃 Добавить активность"),
            new KeyboardButton("🥗 Приём пищи")
        },

        // Аналитика
        new()
        {
            new KeyboardButton("📈 Отчёт"),
            new KeyboardButton("📊 Графики")
        },

        // Здоровье и фото
        new()
        {
            new KeyboardButton("⚖️ ИМТ"),
            new KeyboardButton("📷 Распознать блюдо по фото")
        },

        // Настройки
        new()
        {
            new KeyboardButton("🎯 Цель дня"),
            new KeyboardButton("🕐 Время питания")
        },
        new()
        {
            new KeyboardButton("⏰ Напоминания"),
            new KeyboardButton("✏️ Профиль")
        },

        // Интеграции и помощь
        new()
        {
            new KeyboardButton("🔗 Google Fit"),
            new KeyboardButton("ℹ️ Помощь")
        }
    };

            if (ctx.User.Role == UserRole.Admin)
            {
                rows.Add(new List<KeyboardButton>
        {
            new KeyboardButton("👨‍💼 Админ: Пользователи"),
            new KeyboardButton("📊 Админ: Статистика")
        });
            }

            var keyboard = new ReplyKeyboardMarkup(rows)
            {
                ResizeKeyboard = true
            };

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                $"👋 Привет, {ctx.User.Name}!\n\n" +
                "🏃‍♂️ Основные действия:\n" +
                "• 📊 Сегодня — статистика за день\n" +
                "• 🍽️ Добавить еду — быстрое добавление\n" +
                "• 🏃 Добавить активность — ходьба, тренировки\n" +
                "• 🥗 Приём пищи — полная запись с БЖУ\n\n" +
                "📈 Аналитика:\n" +
                "• 📈 Отчёт — краткий отчёт за период\n" +
                "• 📊 Графики — визуализация прогресса\n\n" +
                "⚙️ Настройки и здоровье:\n" +
                "• 🎯 Цель дня — установить дневную цель\n" +
                "• 🕐 Время питания — расписание приёмов\n" +
                "• ⏰ Напоминания — уведомления\n" +
                "• ⚖️ ИМТ — индекс массы тела\n\n" +
                "ℹ️ Используйте кнопки меню или /help для справки",
                replyMarkup: keyboard,
                cancellationToken: default);
        }



        private async Task HelpCommand(UpdateContext ctx)
        {
            var helpText =
                "📋 **Справка по командам FitnessBot**\n\n" +

                "🏃 **Основные команды:**\n" +
                "📊 Сегодня — статистика за сегодня\n" +
                "📈 Отчёт — краткий отчёт за период\n" +
                "🍽️ Добавить еду — быстро добавить калории\n" +
                "🥗 Приём пищи — добавить с БЖУ\n" +
                "📷 Распознать блюдо по фото — отправьте фото, я попробую определить блюдо\n\n" +

                "⚖️ **Расчёты и ИМТ:**\n" +
                "⚖️ ИМТ — расчёт индекса массы тела\n" +

                "🎯 **Цели и напоминания:**\n" +
                "🎯 Цель дня — установить цель на день\n" +
                "🕐 Время приёмов — настроить расписание\n" +
                "⏰ Напоминания — уведомления об активности\n\n" +

                "📈 **Графики и статистика:**\n" +
                "📊 Графики — меню графиков\n" +
                "/chart_calories — график калорий\n" +
                "/chart_steps — график шагов\n" +
                "/chart_macros — график БЖУ\n\n" +

                "⚙️ **Настройки:**\n" +
                "✏️ Профиль — редактировать профиль\n" +
                "🔗 Google Fit — подключить Google Fit\n" +

                "❌ **Управление:**\n" +
                "/cancel — отменить текущий сценарий\n" +
                "ℹ️ Помощь — эта справка";

            if (ctx.User.Role == UserRole.Admin)
            {
                helpText += "\n\n👨‍💼 **Команды администратора:**\n" +
                           "👨‍💼 Админ: Пользователи — список пользователей\n" +
                           "📊 Админ: Статистика — статистика системы\n" +
                           "/admin_activity — активность пользователей\n" +
                           "/admin_find <имя> — поиск пользователя\n" +
                           "/make_admin <telegram_id> — назначить админа\n" +
                           "/make_user <telegram_id> — снять права админа";
            }

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                helpText,
                cancellationToken: default);
        }

        private async Task ShowBmiFromProfile(UpdateContext ctx)
        {
            // Получаем последний замер ИМТ пользователя
            var latestBmi = await _bmiService.GetLastAsync(ctx.User.Id);

            if (latestBmi == null)
            {
                // Если данных нет, предлагаем заполнить профиль
                var buttons = new[]
                {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✏️ Заполнить рост и вес", "bmi_edit_profile")
            }
        };

                var keyboard = new InlineKeyboardMarkup(buttons);

                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "⚖️ **Индекс массы тела (ИМТ)**\n\n" +
                    "У вас пока нет сохранённых данных о росте и весе.\n\n" +
                    "Нажмите кнопку ниже, чтобы добавить эти данные в профиль:",
                    replyMarkup: keyboard,
                    cancellationToken: default);
                return;
            }

            // Рассчитываем возраст замера
            var daysSinceLastMeasurement = (DateTime.UtcNow - latestBmi.MeasuredAt).Days;
            var measurementInfo = daysSinceLastMeasurement == 0
                ? "сегодня"
                : daysSinceLastMeasurement == 1
                    ? "вчера"
                    : $"{daysSinceLastMeasurement} дн. назад";

            var buttons2 = new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄 Обновить данные", "bmi_edit_profile")
        }
    };

            var keyboard2 = new InlineKeyboardMarkup(buttons2);

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                $"⚖️ **Ваш индекс массы тела**\n\n" +
                $"📊 ИМТ: **{latestBmi.Bmi:F1}**\n" +
                $"📏 Рост: {latestBmi.HeightCm} см\n" +
                $"⚖️ Вес: {latestBmi.WeightKg} кг\n" +
                $"📅 Замер: {measurementInfo}\n\n" +
                $"**Категория:** {latestBmi.Category}\n\n" +
                $"💡 {latestBmi.Recommendation}",
                replyMarkup: keyboard2,
                cancellationToken: default);
        }

        private async Task ShowAddCaloriesMenuAsync(UpdateContext ctx)
        {
            var buttons = new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData(
                        "🍎 100 ккал",
                        $"meal_add_calories:{ctx.User.TelegramId}:100"),
                    InlineKeyboardButton.WithCallbackData(
                        "🥪 200 ккал",
                        $"meal_add_calories:{ctx.User.TelegramId}:200"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(
                        "🍱 300 ккал",
                        $"meal_add_calories:{ctx.User.TelegramId}:300"),
                    InlineKeyboardButton.WithCallbackData(
                        "🍔 500 ккал",
                        $"meal_add_calories:{ctx.User.TelegramId}:500"),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData(
                        "✏️ Другое количество",
                        $"meal_add_custom:{ctx.User.TelegramId}")
                }
            };

            var keyboard = new InlineKeyboardMarkup(buttons);

            await ctx.Bot.SendMessage(
                chatId: ctx.ChatId,
                text: "🍽️ Сколько калорий вы сейчас съели?",
                replyMarkup: keyboard,
                cancellationToken: default);
        }

        private async Task TodayCommand(UpdateContext ctx)
        {
            var userId = ctx.User.Id;
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Получаем данные за день
            var meals = await _mealRepository.GetByUserAndPeriodAsync(userId, today, tomorrow);
            var eatenCalories = meals.Sum(m => m.Calories);
            var eatenCount = meals.Count;

            // ИСПОЛЬЗУЕМ ActivityService вместо репозитория
            var totals = await _activityService.GetMergedTotalsAsync(userId, today, tomorrow);
            
            var burnedCalories = totals.caloriesOut;
            var steps = totals.steps;
            
            
            //ОТЛАДКА
            
            var activities = await _activityService.GetMergedForPeriodAsync(userId, today, tomorrow);

            Console.WriteLine($"[DEBUG] TodayCommand: found {activities.Count} activity records");
            foreach (var a in activities)
            {
                Console.WriteLine($"  - Date={a.Date}, Steps={a.Steps}, Calories={a.CaloriesBurned}, Source={a.Source}, Type={a.Type}");
            }
            
            
            
            var netCalories = eatenCalories - burnedCalories;
            var balanceEmoji = netCalories > 0 ? "📈" : netCalories < 0 ? "📉" : "➡️";

            // Получаем дневную цель
            var dailyGoal = await _reportService.GetDailyGoalAsync(userId, today);

            var text = $"📊 **Статистика за сегодня** ({today:dd.MM.yyyy})\n\n";

            // Если есть цель, показываем прогресс с progress bar
            if (dailyGoal != null)
            {
                var completedGoals = 0;
                var totalGoals = 0;

                text += "🎯 **Прогресс по целям:**\n\n";

                // Прогресс по калориям
                if (dailyGoal.TargetCaloriesIn > 0)
                {
                    totalGoals++;
                    var caloriesProgress = (eatenCalories / dailyGoal.TargetCaloriesIn) * 100;
                    var caloriesBar = CreateProgressBar(caloriesProgress);
                    text += $"🍽️ Калории: ({eatenCount} приём{GetMealEnding(eatenCount)})\n";
                    text += $"{caloriesBar} {caloriesProgress:F0}%\n";
                    text += $"{eatenCalories:F0} / {dailyGoal.TargetCaloriesIn:F0} ккал\n\n";
                    if (caloriesProgress >= 100) completedGoals++;
                }
                else
                {
                    text += $"🍽️ Съедено: {eatenCalories:F0} ккал ({eatenCount} приём{GetMealEnding(eatenCount)})\n\n";
                }

                // Прогресс по шагам
                if (dailyGoal.TargetSteps > 0)
                {
                    totalGoals++;
                    var stepsProgress = ((double)steps / dailyGoal.TargetSteps) * 100;
                    var stepsBar = CreateProgressBar(stepsProgress);
                    text += $"👣 Шаги:\n";
                    text += $"{stepsBar} {stepsProgress:F0}%\n";
                    text += $"{steps:N0} / {dailyGoal.TargetSteps:N0} шагов\n\n";
                    if (stepsProgress >= 100) completedGoals++;
                }
                else
                {
                    text += $"👣 Шаги: {steps:N0}\n\n";
                }

                // Прогресс по сожженным калориям
                if (dailyGoal.TargetCaloriesOut > 0)
                {
                    totalGoals++;
                    var burnProgress = (burnedCalories / dailyGoal.TargetCaloriesOut) * 100;
                    var burnBar = CreateProgressBar(burnProgress);
                    text += $"🔥 Активность:\n";
                    text += $"{burnBar}  {burnProgress:F0} %\n";
                    text += $"{burnedCalories:F0} / {dailyGoal.TargetCaloriesOut:F0} ккал\n\n";
                    if (burnProgress >= 100) completedGoals++;
                }
                else
                {
                    text += $"🔥 Потрачено: {burnedCalories:F0} ккал\n\n";
                }

                // Баланс калорий
                text += $"{balanceEmoji} Баланс: {netCalories:F0} ккал\n\n";

                // Общий прогресс
                if (totalGoals > 0)
                {
                    var overallProgress = ((double)completedGoals / totalGoals) * 100;
                    text += $"✅ Общий прогресс: {completedGoals}/{totalGoals} целей ({overallProgress:F0}%)";

                    if (completedGoals == totalGoals)
                    {
                        text += "\n🎉 Отлично! Все цели достигнуты!";
                    }
                }
            }
            else
            {
                // Если цели нет, показываем простую статистику
                text += $"🍽️ Съедено: {eatenCalories:F0} ккал ({eatenCount} приём{GetMealEnding(eatenCount)})\n";
                text += $"🔥 Потрачено: {burnedCalories:F0} ккал\n";
                text += $"👣 Шаги: {steps:N0}\n\n";
                text += $"{balanceEmoji} Баланс: {netCalories:F0} ккал\n\n";
                text += "💡 Установите дневную цель через \"🎯 Цель дня\" для отслеживания прогресса";
            }

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                text,
                cancellationToken: default);
        }

        private string CreateProgressBar(double percentage)
        {
            const int barLength = 10;
            var filledLength = (int)Math.Min(Math.Round(percentage / 10), barLength);

            var emoji = percentage switch
            {
                >= 100 => "🟢",
                >= 70 => "🟡",
                >= 40 => "🟠",
                _ => "🔴"
            };

            var filled = new string('█', filledLength);
            var empty = new string('░', barLength - filledLength);

            return $"{emoji} {filled}{empty}";
        }

        private string GetMealEnding(int count)
        {
            if (count % 10 == 1 && count % 100 != 11) return "";
            if (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20)) return "а";
            return "ов";
        }


        private async Task ReportCommand(UpdateContext ctx)
        {
            // Показываем календарь для выбора даты
            var today = DateTime.UtcNow.Date;
            var keyboard = CreateCalendarKeyboard(today.Year, today.Month);

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                "📈 Выберите дату для отчёта:",
                replyMarkup: keyboard,
                cancellationToken: default);
        }

        private InlineKeyboardMarkup CreateCalendarKeyboard(int year, int month)
        {
            var firstDay = new DateTime(year, month, 1);
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var startDayOfWeek = (int)firstDay.DayOfWeek;
            if (startDayOfWeek == 0) startDayOfWeek = 7; // Воскресенье = 7

            var buttons = new List<InlineKeyboardButton[]>();

            // Заголовок: месяц и год
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("◀️", $"cal_prev:{year}:{month}"),
        InlineKeyboardButton.WithCallbackData($"{GetMonthName(month)} {year}", "cal_ignore"),
        InlineKeyboardButton.WithCallbackData("▶️", $"cal_next:{year}:{month}")
    });

            // Дни недели
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

            // Дни месяца
            var currentWeek = new List<InlineKeyboardButton>();

            // Пустые клетки до начала месяца
            for (int i = 1; i < startDayOfWeek; i++)
            {
                currentWeek.Add(InlineKeyboardButton.WithCallbackData(" ", "cal_ignore"));
            }

            // Дни месяца
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                var isToday = date == DateTime.UtcNow.Date;
                var isFuture = date > DateTime.UtcNow.Date;

                string buttonText = isToday ? $"[{day}]" : day.ToString();
                string callbackData = isFuture ? "cal_ignore" : $"report_date:{year}:{month}:{day}";

                currentWeek.Add(InlineKeyboardButton.WithCallbackData(buttonText, callbackData));

                // Если неделя заполнена (воскресенье)
                if ((startDayOfWeek + day - 1) % 7 == 0)
                {
                    buttons.Add(currentWeek.ToArray());
                    currentWeek = new List<InlineKeyboardButton>();
                }
            }

            // Добавляем последнюю неделю, если есть
            if (currentWeek.Count > 0)
            {
                // Заполняем пустыми клетками до конца недели
                while (currentWeek.Count < 7)
                {
                    currentWeek.Add(InlineKeyboardButton.WithCallbackData(" ", "cal_ignore"));
                }
                buttons.Add(currentWeek.ToArray());
            }

            // Кнопка "Сегодня"
            buttons.Add(new[]
            {
        InlineKeyboardButton.WithCallbackData("📅 Сегодня", $"report_today")
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


        private async Task ChartsMenuCommand(UpdateContext ctx)
        {
            await ctx.Bot.SendMessage(
                ctx.ChatId,
                "📊 **Меню графиков**\n\n" +
                "Доступные графики:\n" +
                "• /chart_calories — график калорий\n" +
                "• /chart_steps — график шагов\n" +
                "• /chart_macros — график БЖУ",
                cancellationToken: default);
        }

        private async Task StartMealTimeSetupAsync(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.MealTimeSetup,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                "🕐 **Настройка времени приёмов пищи**\n\n" +
                "Введите время завтрака в формате HH:mm\n" +
                "Например: 08:00",
                cancellationToken: default);
        }

        private async Task StartAddMealScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.AddMeal,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.AddMeal);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }

        private async Task ProfileCommand(UpdateContext ctx)
        {
            var user = ctx.User;

            // Получаем последний замер ИМТ
            var latestBmi = await _bmiService.GetLastAsync(user.Id);

            var bmiInfo = latestBmi != null
                ? $"📏 Рост: {latestBmi.HeightCm} см\n" +
                  $"⚖️ Вес: {latestBmi.WeightKg} кг\n" +
                  $"📊 ИМТ: {latestBmi.Bmi:F1} ({latestBmi.Category})\n\n"
                : "📏 Рост и вес: не указаны\n\n";

            // Формируем информацию о профиле
            var profileText =
                $"👤 **Ваш профиль**\n\n" +
                $"Имя: {user.Name}\n" +
                $"Возраст: {(user.Age.HasValue ? user.Age.ToString() : "не указан")}\n" +
                $"Город: {(string.IsNullOrEmpty(user.City) ? "не указан" : user.City)}\n" +
                $"Роль: {user.Role}\n" +
                $"TelegramId: `{user.TelegramId}`\n\n" +
                bmiInfo +
                $"🕐 **Время приёмов пищи:**\n" +
                $"Завтрак: {(user.BreakfastTime.HasValue ? user.BreakfastTime.Value.ToString(@"hh\:mm") : "не установлено")}\n" +
                $"Обед: {(user.LunchTime.HasValue ? user.LunchTime.Value.ToString(@"hh\:mm") : "не установлено")}\n" +
                $"Ужин: {(user.DinnerTime.HasValue ? user.DinnerTime.Value.ToString(@"hh\:mm") : "не установлено")}\n\n" +
                $"📅 Создан: {user.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                $"🕐 Последняя активность: {user.LastActivityAt:dd.MM.yyyy HH:mm}";

            // Создаём кнопки для редактирования
            var buttons = new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("✏️ Редактировать профиль", "profile_edit_menu")
        }
    };

            var keyboard = new InlineKeyboardMarkup(buttons);

            await ctx.Bot.SendMessage(
                ctx.ChatId,
                profileText,
                replyMarkup: keyboard,
                cancellationToken: default);
        }


        private async Task StartSetDailyGoalScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.SetDailyGoal,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.SetDailyGoal);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }

        private async Task StartManualActivityScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.ManualActivity,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.ManualActivity);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }

        private async Task StartActivityReminderSettingsScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.ActivityReminderSettings,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.ActivityReminderSettings);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }

        private async Task StartConnectGoogleFitScenario(UpdateContext ctx)
        {
            var context = new ScenarioContext
            {
                UserId = ctx.User.Id,
                CurrentScenario = ScenarioType.ConnectGoogleFit,
                CurrentStep = 0
            };

            await _contextRepository.SetContext(ctx.User.Id, context, default);

            var scenario = GetScenario(ScenarioType.ConnectGoogleFit);
            await scenario.HandleMessageAsync(ctx.Bot, context, ctx.Message, default);
        }
        private async Task StartFoodPhotoFlow(UpdateContext ctx)
        {
            await ctx.Bot.SendMessage(
                ctx.ChatId,
                "📷 Отправьте фото блюда одним сообщением.\n" +
                "Я попробую распознать его и, если сервис вернёт данные, подскажу калории и БЖУ.\n" +
                "Если калорий не будет, предложу добавить приём пищи вручную.",
                cancellationToken: default);
        }

        private IScenario GetScenario(ScenarioType type)
        {
            var scenario = _scenarios.FirstOrDefault(s => s.CanHandle(type));
            if (scenario == null)
                throw new InvalidOperationException($"Сценарий {type} не найден");
            return scenario;
        }
    }
}