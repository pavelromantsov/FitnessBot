using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FitnessBot.Core.Services
{
    public class GoogleFitClient
    {
        private readonly HttpClient _http;
        private readonly string _clientId;
        private readonly string _clientSecret;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public GoogleFitClient(HttpClient httpClient, string clientId, string clientSecret)
        {
            _http = httpClient;
            _http.Timeout = TimeSpan.FromSeconds(30);

            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        public async Task<(int steps, double calories)> GetDailyActivityAsync(
            string accessToken,
            DateTime dayUtc,
            CancellationToken ct)
        {
            var start = dayUtc.Date;
            var end = start.AddDays(1);

            long startMillis = new DateTimeOffset(start).ToUnixTimeMilliseconds();
            long endMillis = new DateTimeOffset(end).ToUnixTimeMilliseconds();

            var requestBody = new
            {
                aggregateBy = new[]
                {
                    new
                    {
                        dataTypeName = "com.google.step_count.delta",
                        dataSourceId = "derived:com.google.step_count.delta:com.google.android.gms:estimated_steps"
                    },
                    new
                    {
                        dataTypeName = "com.google.calories.expended",
                        dataSourceId = "derived:com.google.calories.expended:com.google.android.gms:platform_calories_expended"
                    }
                },
                bucketByTime = new { durationMillis = 86400000 },
                startTimeMillis = startMillis,
                endTimeMillis = endMillis
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = content;

            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            int totalSteps = 0;
            double totalCalories = 0.0;

            if (doc.RootElement.TryGetProperty("bucket", out var buckets) &&
                buckets.ValueKind == JsonValueKind.Array)
            {
                foreach (var bucket in buckets.EnumerateArray())
                {
                    if (!bucket.TryGetProperty("dataset", out var datasets) ||
                        datasets.ValueKind != JsonValueKind.Array)
                        continue;

                    int dataSetIndex = 0;
                    foreach (var dataset in datasets.EnumerateArray())
                    {
                        if (!dataset.TryGetProperty("point", out var points) ||
                            points.ValueKind != JsonValueKind.Array)
                        {
                            dataSetIndex++;
                            continue;
                        }

                        foreach (var point in points.EnumerateArray())
                        {
                            if (!point.TryGetProperty("value", out var values) ||
                                values.ValueKind != JsonValueKind.Array)
                                continue;

                            foreach (var value in values.EnumerateArray())
                            {
                                if (dataSetIndex == 0)
                                {
                                    if (value.TryGetProperty("intVal", out var intVal) &&
                                        intVal.ValueKind == JsonValueKind.Number &&
                                        intVal.TryGetInt32(out var steps))
                                    {
                                        totalSteps += steps;
                                    }
                                }
                                else if (dataSetIndex == 1)
                                {
                                    if (value.TryGetProperty("fpVal", out var fpVal) &&
                                        fpVal.ValueKind == JsonValueKind.Number &&
                                        fpVal.TryGetDouble(out var calories))
                                    {
                                        totalCalories += calories;
                                    }
                                }
                            }
                        }

                        dataSetIndex++;
                    }
                }
            }

            return (totalSteps, totalCalories);
        }

        public async Task<(string accessToken, DateTime expiresAtUtc)> RefreshTokenAsync(
            string refreshToken,
            CancellationToken ct)
        {
            var requestBody =
                $"client_id={Uri.EscapeDataString(_clientId)}" +
                $"&client_secret={Uri.EscapeDataString(_clientSecret)}" +
                $"&refresh_token={Uri.EscapeDataString(refreshToken)}" +
                "&grant_type=refresh_token";

            using var content = new StringContent(
                requestBody,
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://oauth2.googleapis.com/token"); 

            request.Content = content;

            using var response = await _http.SendAsync(request, ct);

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"[GoogleFit] Refresh failed: {(int)response.StatusCode} {response.ReasonPhrase}, body={body}");
                throw new InvalidOperationException($"GoogleFit refresh failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("access_token", out var accessTokenProp) ||
                accessTokenProp.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException("Google OAuth response does not contain access_token.");
            }

            var accessToken = accessTokenProp.GetString()!;

            int expiresIn = 3600;
            if (doc.RootElement.TryGetProperty("expires_in", out var expiresProp) &&
                expiresProp.ValueKind == JsonValueKind.Number &&
                expiresProp.TryGetInt32(out var exp))
            {
                expiresIn = exp;
            }

            var expiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);

            return (accessToken, expiresAtUtc);
        }
    }
}