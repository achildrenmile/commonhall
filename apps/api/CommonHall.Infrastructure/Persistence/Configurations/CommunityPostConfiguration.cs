using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostConfiguration : IEntityTypeConfiguration<CommunityPost>
{
    public void Configure(EntityTypeBuilder<CommunityPost> builder)
    {
        builder.ToTable("CommunityPosts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Body)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        builder.HasOne(p => p.Community)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Author)
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.CommunityId);
        builder.HasIndex(p => p.AuthorId);
        builder.HasIndex(p => p.IsPinned);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.IsDeleted);
    }
}
