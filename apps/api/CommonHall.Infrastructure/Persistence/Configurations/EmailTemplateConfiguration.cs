using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.Content)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(t => t.ThumbnailUrl)
            .HasMaxLength(500);

        builder.HasIndex(t => t.Name);
        builder.HasIndex(t => t.Category);
        builder.HasIndex(t => t.IsSystem);
        builder.HasIndex(t => t.IsDeleted);
    }
}
