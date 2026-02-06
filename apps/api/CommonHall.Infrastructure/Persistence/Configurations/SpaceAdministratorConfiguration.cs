using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class SpaceAdministratorConfiguration : IEntityTypeConfiguration<SpaceAdministrator>
{
    public void Configure(EntityTypeBuilder<SpaceAdministrator> builder)
    {
        builder.ToTable("SpaceAdministrators");

        builder.HasKey(sa => new { sa.SpaceId, sa.UserId });

        builder.HasOne(sa => sa.Space)
            .WithMany(s => s.Administrators)
            .HasForeignKey(sa => sa.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sa => sa.User)
            .WithMany(u => u.AdministeredSpaces)
            .HasForeignKey(sa => sa.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sa => sa.SpaceId);
        builder.HasIndex(sa => sa.UserId);
    }
}
