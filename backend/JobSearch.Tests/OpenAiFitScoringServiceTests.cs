using System.Net;
using System.Text;
using JobSearch.Application.Services;
using JobSearch.Domain.Entities;
using Xunit;

namespace JobSearch.Tests;

public sealed class OpenAiFitScoringServiceTests
{
    [Fact]
    public async Task ScoreAsync_ReturnsStructuredResultFromOpenAiResponse()
    {
        var responseJson = """
            {
              "output": [
                {
                  "content": [
                    {
                      "type": "output_text",
                      "text": "{\"fitScore\":82,\"matchingSkills\":[\"Angular\",\".NET\"],\"missingSkills\":[\"AWS\"],\"concerns\":[],\"recommendedAction\":\"Apply\"}"
                    }
                  ]
                }
              ]
            }
            """;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, responseJson));
        var service = new OpenAiFitScoringService(httpClient, "test-key");

        var result = await service.ScoreAsync(CreateJob(), string.Empty);

        Assert.Equal(82, result.FitScore);
        Assert.Contains("Angular", result.MatchingSkills);
        Assert.Contains(".NET", result.MatchingSkills);
        Assert.Contains("AWS", result.MissingSkills);
        Assert.Empty(result.Concerns);
        Assert.Equal("Apply", result.RecommendedAction);
    }

    [Fact]
    public async Task ScoreAsync_ThrowsNonSensitiveErrorWhenOpenAiFails()
    {
        var errorResponse = """
            {
              "error": {
                "message": "The requested model is not available for your organization.",
                "type": "invalid_request_error"
              }
            }
            """;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.BadRequest, errorResponse));
        var service = new OpenAiFitScoringService(httpClient, "test-key");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ScoreAsync(CreateJob(), string.Empty));

        Assert.Contains("OpenAI fit scoring request failed (400 Bad Request)", exception.Message);
        Assert.Contains("requested model is not available", exception.Message);
        Assert.DoesNotContain("test-key", exception.Message);
        Assert.DoesNotContain("Build Angular", exception.Message);
    }

    [Fact]
    public async Task ScoreAsync_ThrowsWhenResponseJsonIsInvalid()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, "{}"));
        var service = new OpenAiFitScoringService(httpClient, "test-key");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ScoreAsync(CreateJob(), string.Empty));

        Assert.DoesNotContain("test-key", exception.Message);
        Assert.DoesNotContain("Build Angular", exception.Message);
    }

    private static JobOpportunity CreateJob() =>
        new()
        {
            Company = "Acme",
            Title = "Senior Full Stack Engineer",
            Description = "Build Angular and .NET features."
        };

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;
        private readonly string responseJson;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string responseJson)
        {
            this.statusCode = statusCode;
            this.responseJson = responseJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
    }
}
