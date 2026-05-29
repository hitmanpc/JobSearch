using JobSearch.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using JobSearch.Application.Abstractions;
using JobSearch.Application.Automation;
using JobSearch.Application.Repositories;
using JobSearch.Application.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(string.IsNullOrWhiteSpace(connectionString)
            ? "Data Source=jobsearch.db"
            : connectionString);

        return;
    }

    if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required when Database:Provider is Postgres.");
        }

        options.UseNpgsql(connectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        return;
    }

    throw new InvalidOperationException("Database:Provider must be Sqlite or Postgres.");
});

builder.Services.AddScoped<IJobRepository, EfJobRepository>();
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();
builder.Services.AddScoped<MockFitScoringService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var profile = configuration.GetSection("CandidateProfile").Get<CandidateProfile>() ?? new CandidateProfile();
    return new MockFitScoringService(profile);
});
builder.Services.AddHttpClient();
builder.Services.AddScoped<IFitScoringService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var provider = configuration["FitScoringProvider"] ?? "Mock"; // default also set in appsettings.json

    if (provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
    {
        return serviceProvider.GetRequiredService<MockFitScoringService>();
    }

    if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
    {
        return CreateOpenAiFitScoringService(serviceProvider, configuration);
    }

    if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
    {
        return CreateOllamaFitScoringService(serviceProvider, configuration);
    }

    throw new InvalidOperationException("FitScoringProvider must be Mock, OpenAI, or Ollama.");
});
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IJobImportService, NoOpJobImportService>();
builder.Services.AddHostedService<ScheduledJobImportWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.MapControllers();

app.Run();

static OpenAiFitScoringService CreateOpenAiFitScoringService(
    IServiceProvider serviceProvider,
    IConfiguration configuration)
{
    var apiKey = configuration["OPENAI_API_KEY"];
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException("OPENAI_API_KEY is required when FitScoringProvider is OpenAI.");
    }

    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var model = configuration["OpenAI:FitScoringModel"];

    return new OpenAiFitScoringService(httpClient, apiKey, model);
}

static OllamaFitScoringService CreateOllamaFitScoringService(
    IServiceProvider serviceProvider,
    IConfiguration configuration)
{
    var baseUrl = configuration["Ollama:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("Ollama:BaseUrl is required when FitScoringProvider is Ollama.");
    }

    var model = configuration["Ollama:Model"];
    if (string.IsNullOrWhiteSpace(model))
    {
        throw new InvalidOperationException("Ollama:Model is required when FitScoringProvider is Ollama.");
    }

    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();

    return new OllamaFitScoringService(httpClient, baseUrl, model);
}
