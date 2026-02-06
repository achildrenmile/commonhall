using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(u => u.Department)
            .HasMaxLength(200);

        builder.Property(u => u.Location)
            .HasMaxLength(200);

        builder.Property(u => u.JobTitle)
            .HasMaxLength(200);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(u => u.Bio)
            .HasMaxLength(2000);

        builder.Property(u => u.PreferredLanguage)
            .HasMaxLength(10);

        builder.Property(u => u.ExternalId)
            .HasMaxLength(200);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.ExternalId)
            .HasFilter("\"ExternalId\" IS NOT NULL");

        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => u.IsDeleted);
    }
}
