using System;
using System.Collections.Generic;
using JobSearch.Application.Persistence;
using JobSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace JobSearch.Application.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("JobSearch.Domain.Entities.JobOpportunity", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("TEXT");

            entity.Property<string>("Company")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset?>("DateApplied")
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset>("DateFound")
                .HasColumnType("TEXT");

            entity.Property<string>("Description")
                .HasColumnType("TEXT");

            entity.Property<int?>("FitScore")
                .HasColumnType("INTEGER");

            entity.Property<string>("GeneratedRecruiterMessage")
                .HasColumnType("TEXT");

            entity.Property<string>("Location")
                .HasColumnType("TEXT");

            entity.Property<int>("RemoteType")
                .HasColumnType("INTEGER");

            entity.Property<int>("Status")
                .HasColumnType("INTEGER");

            entity.Property<string>("Title")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property<string>("Url")
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.ToTable("Jobs");

            entity.OwnsOne("JobSearch.Domain.Entities.FitScoreResult", "FitScoreResult", owned =>
            {
                owned.Property<Guid>("JobOpportunityId")
                    .HasColumnType("TEXT");

                owned.Property<IReadOnlyCollection<string>>("Concerns")
                    .HasColumnType("TEXT");

                owned.Property<int>("FitScore")
                    .HasColumnType("INTEGER");

                owned.Property<IReadOnlyCollection<string>>("MatchingSkills")
                    .HasColumnType("TEXT");

                owned.Property<IReadOnlyCollection<string>>("MissingSkills")
                    .HasColumnType("TEXT");

                owned.Property<string>("RecommendedAction")
                    .HasColumnType("TEXT");

                owned.HasKey("JobOpportunityId");

                owned.ToTable("Jobs");

                owned.WithOwner()
                    .HasForeignKey("JobOpportunityId");
            });

            entity.Navigation("FitScoreResult");
        });
    }
}
