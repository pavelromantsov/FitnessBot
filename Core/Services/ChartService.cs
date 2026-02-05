using System.Globalization;
using System.Text.Json;

namespace FitnessBot.Core.Services
{
    public class ChartService
    {
        private const string QuickChartBaseUrl = "https://quickchart.io/chart";

        /// <summary>
        /// Генерирует URL графика калорий за период
        /// </summary>
        public string GenerateCaloriesChartUrl(
            Dictionary<DateTime, double> caloriesIn,
            Dictionary<DateTime, double> caloriesOut,
            string title = "Калории: Потребление vs Расход")
        {
            // Сортируем данные по дате
            var sortedDates = caloriesIn.Keys
                .Union(caloriesOut.Keys)
                .OrderBy(d => d)
                .ToList();

            // Форматируем даты для меток оси X
            var labels = sortedDates
                .Select(d => d.ToString("dd.MM", CultureInfo.InvariantCulture))
                .ToList();

            // Формируем массивы данных
            var caloriesInData = sortedDates
                .Select(d => caloriesIn.ContainsKey(d) ? caloriesIn[d] : 0)
                .ToList();

            var caloriesOutData = sortedDates
                .Select(d => caloriesOut.ContainsKey(d) ? caloriesOut[d] : 0)
                .ToList();

            // Создаём упрощённую конфигурацию Chart.js
            var chartConfig = new
            {
                type = "line",
                data = new
                {
                    labels = labels,
                    datasets = new object[]
                    {
                    new
                    {
                        label = "Потреблено",
                        data = caloriesInData,
                        borderColor = "rgb(255,99,132)",
                        backgroundColor = "rgba(255,99,132,0.2)",
                        fill = true
                    },
                    new
                    {
                        label = "Потрачено",
                        data = caloriesOutData,
                        borderColor = "rgb(54,162,235)",
                        backgroundColor = "rgba(54,162,235,0.2)",
                        fill = true
                    }
                    }
                },
                options = new
                {
                    title = new
                    {
                        display = true,
                        text = title
                    },
                    scales = new
                    {
                        yAxes = new object[]
                        {
                        new
                        {
                            ticks = new
                            {
                                beginAtZero = true
                            }
                        }
                        }
                    }
                }
            };

            // Сериализуем в JSON без пробелов
            var json = JsonSerializer.Serialize(chartConfig, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // Кодируем для URL
            var encodedJson = Uri.EscapeDataString(json);

            // Формируем финальный URL
            return $"{QuickChartBaseUrl}?width=800&height=400&c={encodedJson}";
        }

        /// <summary>
        /// Генерирует URL графика шагов за период
        /// </summary>
        public string GenerateStepsChartUrl(
            Dictionary<DateTime, int> stepsData,
            int? goalSteps = null,
            string title = "Шаги по дням")
        {
            var sortedDates = stepsData.Keys.OrderBy(d => d).ToList();

            var labels = sortedDates
                .Select(d => d.ToString("dd.MM", CultureInfo.InvariantCulture))
                .ToList();

            var steps = sortedDates
                .Select(d => stepsData[d])
                .ToList();

            var datasets = new List<object>
        {
            new
            {
                label = "Шаги",
                data = steps,
                backgroundColor = "rgba(75,192,192,0.8)"
            }
        };

            // Добавляем линию цели, если указана
            if (goalSteps.HasValue)
            {
                var goalLine = Enumerable.Repeat(goalSteps.Value, labels.Count).ToList();
                datasets.Add(new
                {
                    type = "line",
                    label = "Цель",
                    data = goalLine,
                    borderColor = "rgb(255,159,64)",
                    borderWidth = 2,
                    borderDash = new[] { 5, 5 },
                    fill = false,
                    pointRadius = 0
                });
            }

            var chartConfig = new
            {
                type = "bar",
                data = new
                {
                    labels = labels,
                    datasets = datasets
                },
                options = new
                {
                    title = new
                    {
                        display = true,
                        text = title
                    },
                    scales = new
                    {
                        yAxes = new object[]
                        {
                        new
                        {
                            ticks = new
                            {
                                beginAtZero = true
                            }
                        }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(chartConfig, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var encodedJson = Uri.EscapeDataString(json);

            return $"{QuickChartBaseUrl}?width=800&height=400&c={encodedJson}";
        }

        /// <summary>
        /// Генерирует URL графика БЖУ (белки, жиры, углеводы)
        /// </summary>
        public string GenerateMacrosChartUrl(
            Dictionary<DateTime, (double protein, double fat, double carbs)> macrosData,
            string title = "Баланс БЖУ")
        {
            var sortedDates = macrosData.Keys.OrderBy(d => d).ToList();

            var labels = sortedDates
                .Select(d => d.ToString("dd.MM", CultureInfo.InvariantCulture))
                .ToList();

            var proteinData = sortedDates.Select(d => macrosData[d].protein).ToList();
            var fatData = sortedDates.Select(d => macrosData[d].fat).ToList();
            var carbsData = sortedDates.Select(d => macrosData[d].carbs).ToList();

            var chartConfig = new
            {
                type = "bar",
                data = new
                {
                    labels = labels,
                    datasets = new object[]
                    {
                    new
                    {
                        label = "Белки",
                        data = proteinData,
                        backgroundColor = "rgba(255,99,132,0.8)"
                    },
                    new
                    {
                        label = "Жиры",
                        data = fatData,
                        backgroundColor = "rgba(54,162,235,0.8)"
                    },
                    new
                    {
                        label = "Углеводы",
                        data = carbsData,
                        backgroundColor = "rgba(255,206,86,0.8)"
                    }
                    }
                },
                options = new
                {
                    title = new
                    {
                        display = true,
                        text = title
                    },
                    scales = new
                    {
                        xAxes = new object[]
                        {
                        new
                        {
                            stacked = true
                        }
                        },
                        yAxes = new object[]
                        {
                        new
                        {
                            stacked = true,
                            ticks = new
                            {
                                beginAtZero = true
                            }
                        }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(chartConfig, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var encodedJson = Uri.EscapeDataString(json);

            return $"{QuickChartBaseUrl}?width=800&height=400&c={encodedJson}";
        }
    }
}


