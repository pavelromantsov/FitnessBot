using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FitnessBot.Core.Services.LogMeal
{
    public class LogMealClient
    {
        private readonly HttpClient http;
        private readonly string apiToken;

        public LogMealClient(HttpClient http, string apiToken)
        {
            this.http = http;
            this.apiToken = apiToken ?? throw new ArgumentNullException(nameof(apiToken));
        }

        public async Task<LogMealSegmentationResult?> AnalyzeSegmentationAsync(
            Stream imageStream,
            string fileName,
            CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "image", fileName);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.logmeal.es/v2/image/segmentation/complete"); // твой разрешённый метод[web:455]

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiToken);

            request.Content = content;

            using var response = await http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            Console.WriteLine($"LogMeal segmentation status: {(int)response.StatusCode}, body: {body}");

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"LogMeal segmentation error {(int)response.StatusCode}: {body}");

            return JsonSerializer.Deserialize<LogMealSegmentationResult>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
