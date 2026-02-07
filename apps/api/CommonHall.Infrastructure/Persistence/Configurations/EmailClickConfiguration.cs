using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class EmailClickConfiguration : IEntityTypeConfiguration<EmailClick>
{
    public void Configure(EntityTypeBuilder<EmailClick> builder)
    {
        builder.ToTable("EmailClicks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.UserAgent)
            .HasMaxLength(500);

        builder.Property(c => c.IpAddress)
            .HasMaxLength(45);

        builder.HasOne(c => c.Recipient)
            .WithMany(r => r.Clicks)
            .HasForeignKey(c => c.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.RecipientId);
        builder.HasIndex(c => c.ClickedAt);
        builder.HasIndex(c => c.Url);
    }
}
