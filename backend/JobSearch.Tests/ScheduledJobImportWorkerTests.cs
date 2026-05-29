using JobSearch.Application.Automation;
using JobSearch.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace JobSearch.Tests;

public sealed class ScheduledJobImportWorkerTests
{
    [Fact]
    public async Task StartAsync_WhenEnabledWithoutRegisteredImporter_DoesNotThrow()
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
    }

    [Fact]
    public async Task StartAsync_WhenJobImportDisabled_DoesNotRunImporter()
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

        Assert.Equal(0, CountingJobImportService.ImportCount);
    }

    [Fact]
    public async Task StartAsync_WhenEnabled_RunsImmediatelyAndThenWaitsForConfiguredInterval()
    {
        SynchronizationContext.SetSynchronizationContext(null);
        CountingJobImportService.Reset();
        var timeProvider = new FakeTimeProvider();
        await using var provider = CreateProvider(new Dictionary<string, string?>
        {
            ["JobImport:Enabled"] = "true",
            ["JobImport:IntervalMinutes"] = "5"
        });
        var worker = CreateWorker(provider, timeProvider);

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
    public async Task StopAsync_WhenImporterIsRunning_CancelsCurrentRun()
    {
        BlockingJobImportService.Reset();
        await using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["JobImport:Enabled"] = "true",
                ["JobImport:IntervalMinutes"] = "1"
            },
            services => services.AddScoped<IJobImportService, BlockingJobImportService>());
        var worker = CreateWorker(provider);

        await worker.StartAsync(CancellationToken.None);
        await BlockingJobImportService.WaitForStartAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.True(BlockingJobImportService.WasCanceled);
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
        bool registerDefaultImporter = true)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddLogging();
        configureServices?.Invoke(services);
        if (registerDefaultImporter && !services.Any(descriptor => descriptor.ServiceType == typeof(IJobImportService)))
        {
            services.AddScoped<IJobImportService, CountingJobImportService>();
        }

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static ScheduledJobImportWorker CreateWorker(IServiceProvider provider, TimeProvider? timeProvider = null)
    {
        return new ScheduledJobImportWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IConfiguration>(),
            timeProvider ?? provider.GetRequiredService<TimeProvider>(),
            NullLogger<ScheduledJobImportWorker>.Instance);
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
