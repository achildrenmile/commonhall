using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.ToTable("NewsArticles");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Slug)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.TeaserText)
            .HasMaxLength(1000);

        builder.Property(a => a.TeaserImageUrl)
            .HasMaxLength(500);

        builder.Property(a => a.Content)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(a => a.VisibilityRule)
            .HasColumnType("jsonb");

        builder.HasOne(a => a.Space)
            .WithMany(s => s.NewsArticles)
            .HasForeignKey(a => a.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Channel)
            .WithMany(c => c.Articles)
            .HasForeignKey(a => a.ChannelId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Author)
            .WithMany(u => u.AuthoredArticles)
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.GhostAuthor)
            .WithMany(u => u.GhostAuthoredArticles)
            .HasForeignKey(a => a.GhostAuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.Slug)
            .IsUnique();

        builder.HasIndex(a => a.SpaceId);
        builder.HasIndex(a => a.ChannelId);
        builder.HasIndex(a => a.AuthorId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.PublishedAt);
        builder.HasIndex(a => a.ScheduledAt);
        builder.HasIndex(a => a.IsPinned);
        builder.HasIndex(a => a.IsDeleted);
    }
}
