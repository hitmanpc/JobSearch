using JobSearch.Application.Automation;
using JobSearch.Application.Persistence;
using JobSearch.Application.Services;
using JobSearch.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace JobSearch.Tests;

public sealed class ScheduledJobImportWorkerTests
{
    [Fact]
    public async Task StartAsync_WhenEnabledWithoutRegisteredImporter_TracksNoImporterConfigured()
    {
        await using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["JobImport:Enabled"] = "true",
                ["JobImport:IntervalMinutes"] = "1"
            },
            registerDefaultImporter: false);
        var worker = CreateWorker(provider);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        var status = await ReadStatusAsync(provider);
        Assert.Equal(ScheduledJobRunStatus.SingletonId, status.Id);
        Assert.False(status.LastRunSucceeded);
        Assert.Equal(ScheduledJobRunStatusService.NoImporterConfiguredResult, status.LastResult);
        Assert.Null(status.NextExpectedRunAt);
    }

    [Fact]
    public async Task StartAsync_WhenJobImportDisabled_DoesNotRunImporterAndTracksDisabledStatus()
    {
        CountingJobImportService.Reset();
        await using var provider = CreateProvider(new Dictionary<string, string?>
        {
            ["JobImport:Enabled"] = "false",
            ["JobImport:IntervalMinutes"] = "1"
        });
        var worker = CreateWorker(provider);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        var status = await ReadStatusAsync(provider);
        Assert.Equal(0, CountingJobImportService.ImportCount);
        Assert.False(status.LastRunSucceeded);
        Assert.Equal(ScheduledJobRunStatusService.DisabledResult, status.LastResult);
        Assert.Null(status.NextExpectedRunAt);
    }

    [Fact]
    public async Task StartAsync_WhenEnabled_RunsImmediatelyAndThenWaitsForConfiguredInterval()
    {
        SynchronizationContext.SetSynchronizationContext(null);
        CountingJobImportService.Reset();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero));
        await using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["JobImport:Enabled"] = "true",
                ["JobImport:IntervalMinutes"] = "5"
            },
            timeProvider: timeProvider);
        var worker = CreateWorker(provider);

        await worker.StartAsync(CancellationToken.None);
        await CountingJobImportService.WaitForImportCountAsync(1);
        await Task.Delay(100); // wait for Task.Run worker thread to register its Task.Delay timer

        timeProvider.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(1, CountingJobImportService.ImportCount);

        timeProvider.Advance(TimeSpan.FromMinutes(2)); // total 6 min exceeds the 5 min interval
        await CountingJobImportService.WaitForImportCountAsync(2);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(2, CountingJobImportService.ImportCount);
        Assert.Equal(2, CountingJobImportService.InstanceCount);
    }

    [Fact]
    public async Task StartAsync_WhenImportSucceeds_PersistsSuccessfulLifecycleStatus()
    {
        CountingJobImportService.Reset();
        var now = new DateTimeOffset(2026, 5, 29, 14, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        await using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["JobImport:Enabled"] = "true",
                ["JobImport:IntervalMinutes"] = "10"
            },
            timeProvider: timeProvider);
        var worker = CreateWorker(provider);

        await worker.StartAsync(CancellationToken.None);
        await CountingJobImportService.WaitForImportCountAsync(1);
        var status = await WaitForStatusAsync(
            provider,
            status => status.LastRunSucceeded == true && status.NextExpectedRunAt == now.AddMinutes(10));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(now, status.LastRunStartedAt);
        Assert.Equal(now, status.LastRunCompletedAt);
        Assert.True(status.LastRunSucceeded);
        Assert.Equal(ScheduledJobRunStatusService.SucceededResult, status.LastResult);
        Assert.Null(status.LastErrorMessage);
        Assert.Equal(now.AddMinutes(10), status.NextExpectedRunAt);
    }

    [Fact]
    public async Task StartAsync_WhenImportFails_PersistsFailedLifecycleStatusAndSchedulesNextRun()
    {
        ThrowingJobImportService.Reset();
        var now = new DateTimeOffset(2026, 5, 29, 15, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        await using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["JobImport:Enabled"] = "true",
                ["JobImport:IntervalMinutes"] = "7"
            },
            services => services.AddScoped<IJobImportService, ThrowingJobImportService>(),
            timeProvider: timeProvider);
        var worker = CreateWorker(provider);

        await worker.StartAsync(CancellationToken.None);
        await ThrowingJobImportService.WaitForImportAsync();
        var status = await WaitForStatusAsync(
            provider,
            status => status.LastRunSucceeded == false && status.NextExpectedRunAt == now.AddMinutes(7));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(now, status.LastRunStartedAt);
        Assert.Equal(now, status.LastRunCompletedAt);
        Assert.False(status.LastRunSucceeded);
        Assert.Equal(ScheduledJobRunStatusService.FailedResult, status.LastResult);
        Assert.Equal("simulated import failure", status.LastErrorMessage);
        Assert.Equal(now.AddMinutes(7), status.NextExpectedRunAt);
    }

    [Fact]
    public async Task StopAsync_WhenImporterIsRunning_CancelsCurrentRunWithoutMarkingSuccessOrFailure()
    {
        BlockingJobImportService.Reset();
        var now = new DateTimeOffset(2026, 5, 29, 16, 0, 0, TimeSpan.Zero);
        await using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["JobImport:Enabled"] = "true",
                ["JobImport:IntervalMinutes"] = "1"
            },
            services => services.AddScoped<IJobImportService, BlockingJobImportService>(),
            timeProvider: new FakeTimeProvider(now));
        var worker = CreateWorker(provider);

        await worker.StartAsync(CancellationToken.None);
        await BlockingJobImportService.WaitForStartAsync();
        await worker.StopAsync(CancellationToken.None);

        var status = await ReadStatusAsync(provider);
        Assert.True(BlockingJobImportService.WasCanceled);
        Assert.Equal(now, status.LastRunStartedAt);
        Assert.Null(status.LastRunCompletedAt);
        Assert.Null(status.LastRunSucceeded);
        Assert.Equal(ScheduledJobRunStatusService.StartedResult, status.LastResult);
        Assert.Null(status.LastErrorMessage);
        Assert.Null(status.NextExpectedRunAt);
    }

    [Theory]
    [InlineData(null, 60)]
    [InlineData("0", 60)]
    [InlineData("-5", 60)]
    [InlineData("15", 15)]
    public void GetInterval_ReturnsConfiguredPositiveIntervalOrDefault(string? configuredMinutes, int expectedMinutes)
    {
        var configurationValues = new Dictionary<string, string?>
        {
            ["JobImport:IntervalMinutes"] = configuredMinutes
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var interval = ScheduledJobImportWorker.GetInterval(configuration);

        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), interval);
    }

    private static ServiceProvider CreateProvider(
        Dictionary<string, string?> configurationValues,
        Action<IServiceCollection>? configureServices = null,
        bool registerDefaultImporter = true,
        TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<TimeProvider>(timeProvider ?? TimeProvider.System);
        services.AddSingleton(connection);
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IScheduledJobRunStatusService, ScheduledJobRunStatusService>();
        services.AddLogging();
        configureServices?.Invoke(services);
        if (registerDefaultImporter && !services.Any(descriptor => descriptor.ServiceType == typeof(IJobImportService)))
        {
            services.AddScoped<IJobImportService, CountingJobImportService>();
        }

        var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return provider;
    }

    private static ScheduledJobImportWorker CreateWorker(IServiceProvider provider)
    {
        return new ScheduledJobImportWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<TimeProvider>(),
            NullLogger<ScheduledJobImportWorker>.Instance);
    }

    private static async Task<ScheduledJobRunStatus> ReadStatusAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .ScheduledJobRunStatuses
            .AsNoTracking()
            .SingleAsync();
    }

    private static async Task<ScheduledJobRunStatus> WaitForStatusAsync(
        IServiceProvider provider,
        Func<ScheduledJobRunStatus, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!timeout.IsCancellationRequested)
        {
            using var scope = provider.CreateScope();
            var status = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
                .ScheduledJobRunStatuses
                .AsNoTracking()
                .SingleOrDefaultAsync(timeout.Token);

            if (status is not null && predicate(status))
            {
                return status;
            }

            await Task.Delay(25, timeout.Token);
        }

        throw new TimeoutException("Scheduled job import status did not reach the expected state.");
    }

    private sealed class CountingJobImportService : IJobImportService
    {
        private static TaskCompletionSource firstImport = CreateCompletionSource();
        private static TaskCompletionSource secondImport = CreateCompletionSource();
        private static int importCount;
        private static int instanceCount;

        public CountingJobImportService()
        {
            Interlocked.Increment(ref instanceCount);
        }

        public static int ImportCount => Volatile.Read(ref importCount);
        public static int InstanceCount => Volatile.Read(ref instanceCount);

        public static void Reset()
        {
            Interlocked.Exchange(ref importCount, 0);
            Interlocked.Exchange(ref instanceCount, 0);
            firstImport = CreateCompletionSource();
            secondImport = CreateCompletionSource();
        }

        public static Task WaitForImportCountAsync(int count)
        {
            return count switch
            {
                1 => firstImport.Task.WaitAsync(TimeSpan.FromSeconds(5)),
                2 => secondImport.Task.WaitAsync(TimeSpan.FromSeconds(5)),
                _ => throw new ArgumentOutOfRangeException(nameof(count), count, "Only counts 1 and 2 are used by these tests.")
            };
        }

        public Task ImportAsync(CancellationToken cancellationToken = default)
        {
            var count = Interlocked.Increment(ref importCount);
            if (count == 1)
            {
                firstImport.TrySetResult();
            }
            else if (count == 2)
            {
                secondImport.TrySetResult();
                return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return Task.CompletedTask;
        }

        private static TaskCompletionSource CreateCompletionSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ThrowingJobImportService : IJobImportService
    {
        private static TaskCompletionSource imported = CreateCompletionSource();

        public static void Reset()
        {
            imported = CreateCompletionSource();
        }

        public static Task WaitForImportAsync() => imported.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public Task ImportAsync(CancellationToken cancellationToken = default)
        {
            imported.TrySetResult();
            throw new InvalidOperationException("simulated import failure");
        }

        private static TaskCompletionSource CreateCompletionSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class BlockingJobImportService : IJobImportService
    {
        private static TaskCompletionSource started = CreateCompletionSource();
        private static bool wasCanceled;

        public static bool WasCanceled => Volatile.Read(ref wasCanceled);

        public static void Reset()
        {
            started = CreateCompletionSource();
            Volatile.Write(ref wasCanceled, false);
        }

        public static Task WaitForStartAsync() => started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public async Task ImportAsync(CancellationToken cancellationToken = default)
        {
            started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Volatile.Write(ref wasCanceled, true);
                throw;
            }
        }

        private static TaskCompletionSource CreateCompletionSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
