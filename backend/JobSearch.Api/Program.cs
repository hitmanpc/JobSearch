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
builder.Services.AddSingleton<IJobRepository, InMemoryJobRepository>();
builder.Services.AddScoped<IJobService, JobService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.MapControllers();

app.Run();
