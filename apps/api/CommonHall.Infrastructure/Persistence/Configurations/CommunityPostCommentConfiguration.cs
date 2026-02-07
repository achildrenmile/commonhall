using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostCommentConfiguration : IEntityTypeConfiguration<CommunityPostComment>
{
    public void Configure(EntityTypeBuilder<CommunityPostComment> builder)
    {
        builder.ToTable("CommunityPostComments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Body)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.PostId);
        builder.HasIndex(c => c.AuthorId);
        builder.HasIndex(c => c.IsDeleted);
    }
}
