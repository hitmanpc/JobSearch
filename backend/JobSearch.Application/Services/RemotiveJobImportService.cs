using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JobSearch.Application.Abstractions;
using JobSearch.Domain.Entities;
using JobSearch.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobSearch.Application.Services;

public sealed class RemotiveJobImportService : IJobImportService
{
    private const string RemoteJobsEndpoint = "/api/remote-jobs";
    private const string SourceName = "Remotive";

    private readonly HttpClient httpClient;
    private readonly IJobRepository repository;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<RemotiveJobImportService> logger;
    private readonly RemotiveJobImportOptions options;
    private readonly ICandidateProfileService? candidateProfileService;

    public RemotiveJobImportService(
        HttpClient httpClient,
        IJobRepository repository,
        TimeProvider timeProvider,
        ILogger<RemotiveJobImportService> logger,
        IOptions<RemotiveJobImportOptions>? options = null,
        ICandidateProfileService? candidateProfileService = null)
    {
        this.httpClient = httpClient;
        this.repository = repository;
        this.timeProvider = timeProvider;
        this.logger = logger;
        this.options = (options ?? Options.Create(new RemotiveJobImportOptions())).Value;
        this.candidateProfileService = candidateProfileService;
        this.options.Validate();
    }

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        var effectiveOptions = await ResolveOptionsAsync(cancellationToken);
        var endpoint = BuildRemoteJobsEndpoint(effectiveOptions);
        var response = await httpClient.GetFromJsonAsync<RemotiveJobsResponse>(endpoint, cancellationToken)
            ?? new RemotiveJobsResponse();

        var seenCount = 0;
        foreach (var remotiveJob in response.Jobs ?? Array.Empty<RemotiveJob>())
        {
            if (!remotiveJob.HasRequiredFields)
            {
                continue;
            }

            var job = MapToJobOpportunity(remotiveJob, timeProvider.GetUtcNow());
            await repository.UpsertImportedAsync(job, cancellationToken);
            seenCount++;
        }

        logger.LogInformation("Upserted {SeenCount} Remotive jobs from {Endpoint}.", seenCount, endpoint);
    }

    internal async Task<RemotiveJobImportOptions> ResolveOptionsAsync(CancellationToken cancellationToken = default)
    {
        if (candidateProfileService is null)
        {
            return options;
        }

        var profile = await candidateProfileService.GetSettingsAsync(cancellationToken);
        var resolved = new RemotiveJobImportOptions
        {
            RemotiveCategory = string.IsNullOrWhiteSpace(profile.RemotiveCategory)
                ? options.RemotiveCategory
                : profile.RemotiveCategory.Trim(),
            RemotiveSearchText = string.IsNullOrWhiteSpace(profile.RemotiveSearchText)
                ? options.RemotiveSearchText
                : profile.RemotiveSearchText.Trim(),
            RemotiveLimit = profile.RemotiveLimit ?? options.RemotiveLimit
        };

        resolved.Validate("Candidate profile Remotive limit must be a positive integer when provided.");
        return resolved;
    }

    internal static string BuildRemoteJobsEndpoint(RemotiveJobImportOptions options)
    {
        var queryParameters = new List<KeyValuePair<string, string>>();

        if (!string.IsNullOrWhiteSpace(options.RemotiveCategory))
        {
            queryParameters.Add(new KeyValuePair<string, string>("category", options.RemotiveCategory.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(options.RemotiveSearchText))
        {
            queryParameters.Add(new KeyValuePair<string, string>("search", options.RemotiveSearchText.Trim()));
        }

        if (options.RemotiveLimit is > 0)
        {
            queryParameters.Add(new KeyValuePair<string, string>("limit", options.RemotiveLimit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (queryParameters.Count == 0)
        {
            return RemoteJobsEndpoint;
        }

        var queryString = string.Join("&", queryParameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));

        return $"{RemoteJobsEndpoint}?{queryString}";
    }

    internal static JobOpportunity MapToJobOpportunity(RemotiveJob remotiveJob, DateTimeOffset fallbackDateFound)
    {
        return new JobOpportunity
        {
            Company = remotiveJob.CompanyName.Trim(),
            Title = remotiveJob.Title.Trim(),
            Location = string.IsNullOrWhiteSpace(remotiveJob.CandidateRequiredLocation)
                ? null
                : remotiveJob.CandidateRequiredLocation.Trim(),
            RemoteType = RemoteType.Remote,
            Url = remotiveJob.Url.Trim(),
            Source = SourceName,
            ExternalId = remotiveJob.Id > 0 ? remotiveJob.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
            LastSeenAt = fallbackDateFound,
            Description = BuildDescription(remotiveJob),
            Status = ApplicationStatus.Found,
            DateFound = ParsePublicationDate(remotiveJob.PublicationDate) ?? fallbackDateFound,
            DateApplied = null,
            GeneratedRecruiterMessage = null
        };
    }

    private static DateTimeOffset? ParsePublicationDate(string? publicationDate)
    {
        if (DateTimeOffset.TryParse(publicationDate, out var parsedDate))
        {
            return parsedDate;
        }

        return null;
    }

    private static string? BuildDescription(RemotiveJob remotiveJob)
    {
        var description = StripHtml(remotiveJob.Description);
        var details = new[]
        {
            string.IsNullOrWhiteSpace(remotiveJob.Category) ? null : $"Category: {remotiveJob.Category.Trim()}",
            string.IsNullOrWhiteSpace(remotiveJob.JobType) ? null : $"Job type: {remotiveJob.JobType.Trim()}",
            string.IsNullOrWhiteSpace(remotiveJob.Salary) ? null : $"Salary: {remotiveJob.Salary.Trim()}",
            (remotiveJob.Tags?.Count ?? 0) == 0 ? null : $"Tags: {string.Join(", ", remotiveJob.Tags!.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()))}"
        }.Where(detail => !string.IsNullOrWhiteSpace(detail));

        var combined = string.Join(Environment.NewLine + Environment.NewLine, details.Append(description).Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var withoutTags = Regex.Replace(html, "<.*?>", " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return Regex.Replace(decoded, "\\s+", " ").Trim();
    }
}

public sealed class RemotiveJobsResponse
{
    [JsonPropertyName("jobs")]
    public IReadOnlyCollection<RemotiveJob>? Jobs { get; init; }
}

public sealed class RemotiveJob
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("company_name")]
    public string CompanyName { get; init; } = string.Empty;

    [JsonPropertyName("candidate_required_location")]
    public string? CandidateRequiredLocation { get; init; }

    [JsonPropertyName("job_type")]
    public string? JobType { get; init; }

    [JsonPropertyName("publication_date")]
    public string? PublicationDate { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyCollection<string>? Tags { get; init; }

    [JsonPropertyName("salary")]
    public string? Salary { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    public bool HasRequiredFields =>
        !string.IsNullOrWhiteSpace(Url) &&
        !string.IsNullOrWhiteSpace(Title) &&
        !string.IsNullOrWhiteSpace(CompanyName);
}
