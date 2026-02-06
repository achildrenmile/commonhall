using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class ArticleTagConfiguration : IEntityTypeConfiguration<ArticleTag>
{
    public void Configure(EntityTypeBuilder<ArticleTag> builder)
    {
        builder.ToTable("ArticleTags");

        builder.HasKey(at => new { at.NewsArticleId, at.TagId });

        builder.HasOne(at => at.NewsArticle)
            .WithMany(a => a.ArticleTags)
            .HasForeignKey(at => at.NewsArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(at => at.Tag)
            .WithMany(t => t.ArticleTags)
            .HasForeignKey(at => at.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(at => at.NewsArticleId);
        builder.HasIndex(at => at.TagId);
    }
}
