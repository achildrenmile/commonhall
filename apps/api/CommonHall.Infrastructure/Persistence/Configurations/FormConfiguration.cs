using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class FormConfiguration : IEntityTypeConfiguration<Form>
{
    public void Configure(EntityTypeBuilder<Form> builder)
    {
        builder.ToTable("Forms");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.Description)
            .HasMaxLength(2000);

        builder.Property(f => f.Fields)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(f => f.NotificationEmail)
            .HasMaxLength(255);

        builder.Property(f => f.ConfirmationMessage)
            .HasMaxLength(2000);

        builder.HasOne(f => f.Space)
            .WithMany()
            .HasForeignKey(f => f.SpaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(f => f.IsActive);
        builder.HasIndex(f => f.SpaceId);
        builder.HasIndex(f => f.IsDeleted);
    }
}
