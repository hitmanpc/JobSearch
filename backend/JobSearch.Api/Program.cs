using JobSearch.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using JobSearch.Application.Abstractions;
using JobSearch.Application.Repositories;
using JobSearch.Application.Services;

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
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=jobsearch.db"));

builder.Services.AddScoped<IJobRepository, EfJobRepository>();
builder.Services.AddScoped<MockFitScoringService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IFitScoringService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var provider = configuration["FitScoringProvider"] ?? "Mock";

    if (provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
    {
        return serviceProvider.GetRequiredService<MockFitScoringService>();
    }

    if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
    {
        return CreateOpenAiFitScoringService(serviceProvider, configuration);
    }

    throw new InvalidOperationException("FitScoringProvider must be either Mock or OpenAI.");
});
builder.Services.AddScoped<IJobService, JobService>();

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
