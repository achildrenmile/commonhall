using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class EmailRecipientConfiguration : IEntityTypeConfiguration<EmailRecipient>
{
    public void Configure(EntityTypeBuilder<EmailRecipient> builder)
    {
        builder.ToTable("EmailRecipients");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(r => r.TrackingToken)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(r => r.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasOne(r => r.Newsletter)
            .WithMany(n => n.Recipients)
            .HasForeignKey(r => r.NewsletterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.TrackingToken)
            .IsUnique();

        builder.HasIndex(r => r.NewsletterId);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.Email);
    }
}
