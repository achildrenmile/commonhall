using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserGroup> UserGroups { get; }
    DbSet<UserGroupMembership> UserGroupMemberships { get; }
    DbSet<Space> Spaces { get; }
    DbSet<SpaceAdministrator> SpaceAdministrators { get; }
    DbSet<Page> Pages { get; }
    DbSet<PageVersion> PageVersions { get; }
    DbSet<NewsChannel> NewsChannels { get; }
    DbSet<NewsArticle> NewsArticles { get; }
    DbSet<Tag> Tags { get; }
    DbSet<ArticleTag> ArticleTags { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Reaction> Reactions { get; }
    DbSet<StoredFile> StoredFiles { get; }
    DbSet<FileCollection> FileCollections { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailNewsletter> EmailNewsletters { get; }
    DbSet<EmailRecipient> EmailRecipients { get; }
    DbSet<EmailClick> EmailClicks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
