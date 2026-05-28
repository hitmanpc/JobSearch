using System.Text.Json;
using JobSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JobSearch.Application.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<JobOpportunity> Jobs => Set<JobOpportunity>();
    public DbSet<CandidateProfileSettings> CandidateProfile => Set<CandidateProfileSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var listConverter = new ValueConverter<IReadOnlyCollection<string>, string>(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<string[]>(value, (JsonSerializerOptions?)null) ?? Array.Empty<string>());
        var listComparer = new ValueComparer<IReadOnlyCollection<string>>(
            (left, right) => (left == null && right == null) ||
                (left != null && right != null && left.SequenceEqual(right)),
            value => value == null
                ? 0
                : value.Aggregate(
                    0,
                    (hash, item) => HashCode.Combine(hash, item == null ? 0 : StringComparer.Ordinal.GetHashCode(item))),
            value => value == null ? Array.Empty<string>() : value.ToArray());

        modelBuilder.Entity<JobOpportunity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Company).IsRequired();
            entity.Property(x => x.Title).IsRequired();
            entity.Property(x => x.GeneratedRecruiterMessage);

            entity.OwnsOne(x => x.FitScoreResult, owned =>
            {
                owned.Property(x => x.RecommendedAction);

                var matchingSkills = owned.Property(x => x.MatchingSkills).HasConversion(listConverter);
                matchingSkills.Metadata.SetValueComparer(listComparer);

                var missingSkills = owned.Property(x => x.MissingSkills).HasConversion(listConverter);
                missingSkills.Metadata.SetValueComparer(listComparer);

                var concerns = owned.Property(x => x.Concerns).HasConversion(listConverter);
                concerns.Metadata.SetValueComparer(listComparer);
            });
        });
    }
}
