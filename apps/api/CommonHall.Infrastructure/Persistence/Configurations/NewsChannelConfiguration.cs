using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class NewsChannelConfiguration : IEntityTypeConfiguration<NewsChannel>
{
    public void Configure(EntityTypeBuilder<NewsChannel> builder)
    {
        builder.ToTable("NewsChannels");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.Color)
            .HasMaxLength(7);

        builder.HasOne(c => c.Space)
            .WithMany(s => s.NewsChannels)
            .HasForeignKey(c => c.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.HasIndex(c => c.SpaceId);
    }
}
