using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class CommunityMembershipConfiguration : IEntityTypeConfiguration<CommunityMembership>
{
    public void Configure(EntityTypeBuilder<CommunityMembership> builder)
    {
        builder.ToTable("CommunityMemberships");

        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.Community)
            .WithMany(c => c.Memberships)
            .HasForeignKey(m => m.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.CommunityId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => m.Role);
    }
}
