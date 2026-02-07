using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommonHall.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Body)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(m => m.Attachments)
            .HasColumnType("jsonb");

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Author)
            .WithMany()
            .HasForeignKey(m => m.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.AuthorId);
        builder.HasIndex(m => m.CreatedAt);
        builder.HasIndex(m => m.IsDeleted);
    }
}
