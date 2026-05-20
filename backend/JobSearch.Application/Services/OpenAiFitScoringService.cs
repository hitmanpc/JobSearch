using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JobSearch.Domain.Entities;

namespace JobSearch.Application.Services;

public sealed class OpenAiFitScoringService : IFitScoringService
{
    private const string DefaultModel = "gpt-4o-mini";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly string apiKey;
    private readonly string model;

    public OpenAiFitScoringService(HttpClient httpClient, string apiKey, string? model = null)
    {
        this.httpClient = httpClient;
        this.apiKey = string.IsNullOrWhiteSpace(apiKey)
            ? throw new ArgumentException("OpenAI API key is required.", nameof(apiKey))
            : apiKey;
        this.model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
    }

    public async Task<FitScoreResult> ScoreAsync(JobOpportunity job, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
        {
            Content = JsonContent.Create(BuildRequest(job), options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("OpenAI fit scoring request failed.");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var resultJson = ExtractOutputText(responseJson);
        var result = JsonSerializer.Deserialize<FitScoreResult>(resultJson, JsonOptions);

        if (!IsValidResult(result))
        {
            throw new InvalidOperationException("OpenAI fit scoring returned an invalid result.");
        }

        return result!;
    }

    private object BuildRequest(JobOpportunity job) =>
        new
        {
            model,
            instructions = """
                You score job opportunities for a senior full-stack software engineer.
                Return JSON only. Do not include markdown or explanatory text.
                Evaluate only the supplied job opportunity details.
                """,
            input = new[]
            {
                new
                {
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "input_text",
                            text = JsonSerializer.Serialize(
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
                    }
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "fit_score_result",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[]
                        {
                            "fitScore",
                            "matchingSkills",
                            "missingSkills",
                            "concerns",
                            "recommendedAction"
                        },
                        properties = new
                        {
                            fitScore = new
                            {
                                type = "integer",
                                minimum = 0,
                                maximum = 100
                            },
                            matchingSkills = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            },
                            missingSkills = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            },
                            concerns = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            },
                            recommendedAction = new
                            {
                                type = "string",
                                @enum = new[]
                                {
                                    "Apply",
                                    "Review before applying",
                                    "Deprioritize"
                                }
                            }
                        }
                    }
                }
            }
        };

    private static string ExtractOutputText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString()!;
        }

        if (!root.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("OpenAI fit scoring response did not include output JSON.");
        }

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) &&
                    text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString()!;
                }
            }
        }

        throw new InvalidOperationException("OpenAI fit scoring response did not include output JSON.");
    }

    private static bool IsValidResult(FitScoreResult? result) =>
        result is not null &&
        result.FitScore is >= 0 and <= 100 &&
        !string.IsNullOrWhiteSpace(result.RecommendedAction);
}
