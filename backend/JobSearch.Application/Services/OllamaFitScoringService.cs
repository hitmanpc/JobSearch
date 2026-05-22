using System.Net.Http.Json;
using System.Text.Json;
using JobSearch.Domain.Entities;

namespace JobSearch.Application.Services;

public sealed class OllamaFitScoringService : IFitScoringService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly string baseUrl;
    private readonly string model;

    public OllamaFitScoringService(HttpClient httpClient, string baseUrl, string model)
    {
        this.httpClient = httpClient;
        this.baseUrl = baseUrl.TrimEnd('/');
        this.model = model;
    }

    public async Task<FitScoreResult> ScoreAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                        You score job opportunities for a senior full-stack software engineer.
                        Return JSON only. Do not include markdown or explanatory text.
                        The JSON must have these exact fields:
                        - fitScore: integer between 0 and 100
                        - matchingSkills: array of strings
                        - missingSkills: array of strings
                        - concerns: array of strings
                        - recommendedAction: one of "Apply", "Review before applying", or "Deprioritize"
                        """
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(
                        new
                        {
                            job.Company,
                            job.Title,
                            job.Location,
                            RemoteType = job.RemoteType.ToString(),
                            job.Description
                        },
                        JsonOptions)
                }
            },
            format = "json",
            stream = false
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"{baseUrl}/chat/completions",
            requestBody,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Ollama request failed ({(int)response.StatusCode} {response.StatusCode}): {body}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var content = ExtractContent(responseJson);
        var result = JsonSerializer.Deserialize<FitScoreResult>(content, JsonOptions);

        if (result is null || string.IsNullOrWhiteSpace(result.RecommendedAction))
        {
            throw new InvalidOperationException("Ollama fit scoring returned an invalid result.");
        }

        return result;
    }

    private static string ExtractContent(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array)
        {
            foreach (var choice in choices.EnumerateArray())
            {
                if (choice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content) &&
                    content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString()!;
                }
            }
        }

        throw new InvalidOperationException("Ollama fit scoring response did not contain expected content.");
    }
}
