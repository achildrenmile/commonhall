using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("Reactions");

        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.NewsArticle)
            .WithMany(a => a.Reactions)
            .HasForeignKey(r => r.NewsArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reactions)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.NewsArticleId, r.UserId, r.Type })
            .IsUnique();

        builder.HasIndex(r => r.NewsArticleId);
        builder.HasIndex(r => r.UserId);
    }
}
