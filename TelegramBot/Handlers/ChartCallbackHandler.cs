using FitnessBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FitnessBot.TelegramBot.Handlers
{
    public sealed class ChartCallbackHandler : ICallbackHandler
    {
        private readonly ChartService _chartService;
        private readonly ChartDataService _chartDataService;
        private readonly ChartImageService _chartImageService;

        public ChartCallbackHandler(
            ChartService chartService,
            ChartDataService chartDataService,
            ChartImageService chartImageService)
        {
            _chartService = chartService;
            _chartDataService = chartDataService;
            _chartImageService = chartImageService;
        }

        public async Task<bool> HandleAsync(UpdateContext context, string data)
        {
            if (!data.StartsWith("chart_", StringComparison.OrdinalIgnoreCase))
                return false;

            await context.Bot.AnswerCallbackQuery(context.CallbackQuery!.Id, 
                cancellationToken: default);

            switch (data)
            {
                case "chart_cal_7":
                    await ChartCaloriesCommand(context, 7);
                    break;
                case "chart_cal_14":
                    await ChartCaloriesCommand(context, 14);
                    break;
                case "chart_steps_7":
                    await ChartStepsCommand(context, 7);
                    break;
                case "chart_steps_14":
                    await ChartStepsCommand(context, 14);
                    break;
                case "chart_macros_7":
                    await ChartMacrosCommand(context, 7);
                    break;
                case "chart_macros_14":
                    await ChartMacrosCommand(context, 14);
                    break;
                default:
                    return false;
            }

            return true;
        }

        private async Task ChartCaloriesCommand(UpdateContext ctx, int days)
        {
            try
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "⏳ Генерирую график калорий...",
                    cancellationToken: default);

                var (caloriesIn, caloriesOut) = await _chartDataService.GetCaloriesDataAsync(
                    ctx.User.Id, days);

                if (!caloriesIn.Any() && !caloriesOut.Any())
                {
                    await ctx.Bot.SendMessage(
                        ctx.ChatId,
                        "📊 Недостаточно данных для построения графика.\n" +
                        "Добавьте записи о питании и активности.",
                        cancellationToken: default);
                    return;
                }

                var chartUrl = _chartService.GenerateCaloriesChartUrl(
                    caloriesIn,
                    caloriesOut,
                    $"Калории за последние {days} дней");

                using var imageStream = await _chartImageService.DownloadChartImageAsync(chartUrl);

                await ctx.Bot.SendPhoto(
                    ctx.ChatId,
                    InputFile.FromStream(imageStream, "chart.png"),
                    caption: $"📊 График калорий за последние {days} дней\n\n" +
                             $"🔴 Красная линия - потреблено\n" +
                             $"🔵 Синяя линия - потрачено\n\n" +
                             $"Средние значения:\n" +
                             $"• Потребление: {caloriesIn.Values.Average():F0} ккал/день\n" +
                             $"• Расход: {caloriesOut.Values.Average():F0} ккал/день",
                    cancellationToken: default);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации графика калорий: {ex}");
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "❌ Ошибка при генерации графика. Попробуйте позже.",
                    cancellationToken: default);
            }
        }

        private async Task ChartStepsCommand(UpdateContext ctx, int days)
        {
            try
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "⏳ Генерирую график шагов...",
                    cancellationToken: default);

                var stepsData = await _chartDataService.GetStepsDataAsync(ctx.User.Id, days);

                if (!stepsData.Any() || stepsData.Values.All(v => v == 0))
                {
                    await ctx.Bot.SendMessage(
                        ctx.ChatId,
                        "👣 Недостаточно данных для построения графика шагов.\n" +
                        "Добавьте записи об активности.",
                        cancellationToken: default);
                    return;
                }

                var chartUrl = _chartService.GenerateStepsChartUrl(
                    stepsData,
                    10000,
                    $"Шаги за последние {days} дней");

                using var imageStream = await _chartImageService.DownloadChartImageAsync(chartUrl);

                await ctx.Bot.SendPhoto(
                    ctx.ChatId,
                    InputFile.FromStream(imageStream, "chart.png"),
                    caption: $"👣 График шагов за последние {days} дней\n\n" +
                             $"Среднее: {stepsData.Values.Average():F0} шагов/день\n" +
                             $"Максимум: {stepsData.Values.Max()} шагов\n" +
                             $"Всего: {stepsData.Values.Sum()} шагов",
                    cancellationToken: default);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации графика шагов: {ex}");
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "❌ Ошибка при генерации графика. Попробуйте позже.",
                    cancellationToken: default);
            }
        }

        private async Task ChartMacrosCommand(UpdateContext ctx, int days)
        {
            try
            {
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "⏳ Генерирую график БЖУ...",
                    cancellationToken: default);

                var macrosData = await _chartDataService.GetMacrosDataAsync(ctx.User.Id, days);

                if (!macrosData.Any() || macrosData.Values.All(m => m.protein == 0 &&
                m.fat == 0 && m.carbs == 0))
                {
                    await ctx.Bot.SendMessage(
                        ctx.ChatId,
                        "🍖 Недостаточно данных для построения графика БЖУ.\n" +
                        "Добавьте записи о питании с указанием БЖУ.",
                        cancellationToken: default);
                    return;
                }

                var chartUrl = _chartService.GenerateMacrosChartUrl(
                    macrosData,
                    $"Баланс БЖУ за последние {days} дней");

                using var imageStream = await _chartImageService.DownloadChartImageAsync(chartUrl);

                var avgProtein = macrosData.Values.Average(m => m.protein);
                var avgFat = macrosData.Values.Average(m => m.fat);
                var avgCarbs = macrosData.Values.Average(m => m.carbs);

                await ctx.Bot.SendPhoto(
                    ctx.ChatId,
                    InputFile.FromStream(imageStream, "chart.png"),
                    caption: $"🍖 Баланс БЖУ за последние {days} дней\n\n" +
                             $"Среднее в день:\n" +
                             $"• Белки: {avgProtein:F0} г\n" +
                             $"• Жиры: {avgFat:F0} г\n" +
                             $"• Углеводы: {avgCarbs:F0} г",
                    cancellationToken: default);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации графика БЖУ: {ex}");
                await ctx.Bot.SendMessage(
                    ctx.ChatId,
                    "❌ Ошибка при генерации графика. Попробуйте позже.",
                    cancellationToken: default);
            }
        }
    }
}
