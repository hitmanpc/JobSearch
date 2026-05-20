using System.Text.Json.Serialization;
using JobSearch.Application.Abstractions;
using JobSearch.Application.Repositories;
using JobSearch.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IJobRepository, InMemoryJobRepository>();
builder.Services.AddScoped<IJobService, JobService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
