using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostReactionConfiguration : IEntityTypeConfiguration<CommunityPostReaction>
{
    public void Configure(EntityTypeBuilder<CommunityPostReaction> builder)
    {
        builder.ToTable("CommunityPostReactions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(r => r.Post)
            .WithMany(p => p.Reactions)
            .HasForeignKey(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.PostId, r.UserId, r.Type }).IsUnique();
        builder.HasIndex(r => r.PostId);
        builder.HasIndex(r => r.UserId);
    }
}
