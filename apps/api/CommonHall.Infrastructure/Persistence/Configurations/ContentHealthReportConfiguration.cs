using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public class ContentHealthReportConfiguration : IEntityTypeConfiguration<ContentHealthReport>
{
    public void Configure(EntityTypeBuilder<ContentHealthReport> builder)
    {
        builder.ToTable("ContentHealthReports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasMaxLength(2000);

        builder.HasMany(x => x.Issues)
            .WithOne(x => x.Report)
            .HasForeignKey(x => x.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ScanStartedAt);
        builder.HasIndex(x => x.Status);
    }
}

public class ContentHealthIssueConfiguration : IEntityTypeConfiguration<ContentHealthIssue>
{
    public void Configure(EntityTypeBuilder<ContentHealthIssue> builder)
    {
        builder.ToTable("ContentHealthIssues");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ContentType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ContentTitle)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ContentUrl)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.IssueType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Recommendation)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.ReportId);
        builder.HasIndex(x => x.ContentType);
        builder.HasIndex(x => x.IssueType);
        builder.HasIndex(x => x.IsResolved);
    }
}
