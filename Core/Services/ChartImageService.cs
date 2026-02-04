using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Core.Services
{
    public class ChartImageService
    {
        private readonly HttpClient _httpClient;

        public ChartImageService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Скачивает изображение по URL и возвращает поток
        /// </summary>
        public async Task<Stream> DownloadChartImageAsync(string chartUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(chartUrl);
                response.EnsureSuccessStatusCode();

                var memoryStream = new MemoryStream();
                await response.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Сбрасываем позицию на начало

                return memoryStream;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка скачивания графика: {ex.Message}");
                throw;
            }
        }
    }
}
