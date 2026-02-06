using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class FileCollectionConfiguration : IEntityTypeConfiguration<FileCollection>
{
    public void Configure(EntityTypeBuilder<FileCollection> builder)
    {
        builder.ToTable("FileCollections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.HasOne(c => c.Space)
            .WithMany(s => s.FileCollections)
            .HasForeignKey(c => c.SpaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.SpaceId);
    }
}
