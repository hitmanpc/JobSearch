using JobSearch.Application.Persistence;
using JobSearch.Application.Repositories;
using JobSearch.Domain.Entities;
using JobSearch.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JobSearch.Tests;

public sealed class EfJobRepositoryTests
{
    [Fact]
    public async Task UpsertImportedAsync_UpdatesExistingRemotiveJobByExternalId()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        var firstSeen = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
        var secondSeen = new DateTimeOffset(2026, 5, 21, 12, 0, 0, TimeSpan.Zero);
        var existingId = Guid.NewGuid();

        await using (var insertContext = new AppDbContext(options))
        {
            var repository = new EfJobRepository(insertContext);
            await repository.UpsertImportedAsync(new JobOpportunity
            {
                Id = existingId,
                Company = "Original Co",
                Title = "Original Role",
                Url = "https://remotive.com/original",
                Source = "Remotive",
                ExternalId = "123",
                LastSeenAt = firstSeen,
                Description = "Original description",
                DateFound = firstSeen,
                Status = ApplicationStatus.Interested,
                GeneratedRecruiterMessage = "Keep me",
                FitScore = 88
            });
        }

        await using (var updateContext = new AppDbContext(options))
        {
            var repository = new EfJobRepository(updateContext);
            var upserted = await repository.UpsertImportedAsync(new JobOpportunity
            {
                Company = "Updated Co",
                Title = "Updated Role",
                Url = "https://remotive.com/updated",
                Source = "Remotive",
                ExternalId = "123",
                LastSeenAt = secondSeen,
                Description = "Updated description",
                DateFound = secondSeen,
                Status = ApplicationStatus.Found
            });

            Assert.Equal(existingId, upserted.Id);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var jobs = await assertContext.Jobs.AsNoTracking().ToArrayAsync();
            var job = Assert.Single(jobs);
            Assert.Equal(existingId, job.Id);
            Assert.Equal("Updated Co", job.Company);
            Assert.Equal("Updated Role", job.Title);
            Assert.Equal("https://remotive.com/updated", job.Url);
            Assert.Equal(secondSeen, job.LastSeenAt);
            Assert.Equal("Updated description", job.Description);
            Assert.Equal(ApplicationStatus.Interested, job.Status);
            Assert.Equal("Keep me", job.GeneratedRecruiterMessage);
            Assert.Equal(88, job.FitScore);
            Assert.Equal(firstSeen, job.DateFound);
        }
    }

    [Fact]
    public async Task UpsertImportedAsync_UpdatesExistingRemotiveJobByUrlWhenExternalIdIsMissing()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        const string remotiveUrl = "https://remotive.com/remote-jobs/software-dev/no-id";

        await using (var insertContext = new AppDbContext(options))
        {
            var repository = new EfJobRepository(insertContext);
            await repository.UpsertImportedAsync(new JobOpportunity
            {
                Company = "Original Co",
                Title = "Original Role",
                Url = remotiveUrl,
                Source = "Remotive",
                LastSeenAt = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero)
            });
        }

        await using (var updateContext = new AppDbContext(options))
        {
            var repository = new EfJobRepository(updateContext);
            await repository.UpsertImportedAsync(new JobOpportunity
            {
                Company = "Updated Co",
                Title = "Updated Role",
                Url = remotiveUrl,
                Source = "Remotive",
                LastSeenAt = new DateTimeOffset(2026, 5, 21, 12, 0, 0, TimeSpan.Zero)
            });
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var job = Assert.Single(await assertContext.Jobs.AsNoTracking().ToArrayAsync());
            Assert.Equal("Updated Co", job.Company);
            Assert.Equal("Updated Role", job.Title);
            Assert.Null(job.ExternalId);
            Assert.Equal(remotiveUrl, job.Url);
        }
    }
}
