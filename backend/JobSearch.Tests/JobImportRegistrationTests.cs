using JobSearch.Application.Abstractions;
using JobSearch.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobSearch.Tests;

public sealed class JobImportRegistrationTests
{
    [Fact]
    public void AddConfiguredJobImport_WhenConfiguredWithRemotive_RegistersOnlyRemotiveImporter()
    {
        var services = CreateServices(new Dictionary<string, string?>
        {
            ["JobImport:Provider"] = "Remotive",
            ["JobImport:RemotiveBaseUrl"] = "https://remotive.com"
        });

        using var provider = services.BuildServiceProvider();

        var importer = provider.GetRequiredService<IJobImportService>();

        Assert.IsType<RemotiveJobImportService>(importer);
        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IJobImportService)));
    }

    [Theory]
    [InlineData("LinkedIn")]
    [InlineData("Indeed")]
    [InlineData("Unknown")]
    public void AddConfiguredJobImport_WhenConfiguredWithUnsupportedProvider_RejectsProvider(string provider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobImport:Provider"] = provider
            })
            .Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddConfiguredJobImport(configuration));
        Assert.Equal("JobImport:Provider must be Remotive.", exception.Message);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IJobImportService));
    }

    private static ServiceCollection CreateServices(Dictionary<string, string?> configurationValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IJobRepository, EmptyJobRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddLogging();

        services.AddConfiguredJobImport(configuration);

        return services;
    }

    private sealed class EmptyJobRepository : IJobRepository
    {
        public Task<IReadOnlyCollection<JobSearch.Domain.Entities.JobOpportunity>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<JobSearch.Domain.Entities.JobOpportunity>>(Array.Empty<JobSearch.Domain.Entities.JobOpportunity>());

        public Task<JobSearch.Domain.Entities.JobOpportunity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<JobSearch.Domain.Entities.JobOpportunity?>(null);

        public Task<JobSearch.Domain.Entities.JobOpportunity> AddAsync(JobSearch.Domain.Entities.JobOpportunity job, CancellationToken cancellationToken = default) =>
            Task.FromResult(job);

        public Task UpdateAsync(JobSearch.Domain.Entities.JobOpportunity job, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
