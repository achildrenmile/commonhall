using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class EmailNewsletterConfiguration : IEntityTypeConfiguration<EmailNewsletter>
{
    public void Configure(EntityTypeBuilder<EmailNewsletter> builder)
    {
        builder.ToTable("EmailNewsletters");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.PreviewText)
            .HasMaxLength(500);

        builder.Property(n => n.Content)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(n => n.TargetGroupIds)
            .HasColumnType("jsonb");

        builder.HasOne(n => n.Template)
            .WithMany(t => t.Newsletters)
            .HasForeignKey(n => n.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.ScheduledAt);
        builder.HasIndex(n => n.SentAt);
        builder.HasIndex(n => n.TemplateId);
        builder.HasIndex(n => n.IsDeleted);
    }
}
