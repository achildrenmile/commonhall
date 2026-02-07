using CommonHall.Application.Interfaces;
using CommonHall.Domain.Common;
using CommonHall.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Infrastructure.Persistence;

public sealed class CommonHallDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;

    public CommonHallDbContext(
        DbContextOptions<CommonHallDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<UserGroupMembership> UserGroupMemberships => Set<UserGroupMembership>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<SpaceAdministrator> SpaceAdministrators => Set<SpaceAdministrator>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageVersion> PageVersions => Set<PageVersion>();
    public DbSet<NewsChannel> NewsChannels => Set<NewsChannel>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<FileCollection> FileCollections => Set<FileCollection>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailNewsletter> EmailNewsletters => Set<EmailNewsletter>();
    public DbSet<EmailRecipient> EmailRecipients => Set<EmailRecipient>();
    public DbSet<EmailClick> EmailClicks => Set<EmailClick>();
    public DbSet<Journey> Journeys => Set<Journey>();
    public DbSet<JourneyStep> JourneySteps => Set<JourneyStep>();
    public DbSet<JourneyEnrollment> JourneyEnrollments => Set<JourneyEnrollment>();
    public DbSet<JourneyStepCompletion> JourneyStepCompletions => Set<JourneyStepCompletion>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<SurveyAnswer> SurveyAnswers => Set<SurveyAnswer>();
    public DbSet<Form> Forms => Set<Form>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<CommunityMembership> CommunityMemberships => Set<CommunityMembership>();
    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<CommunityPostComment> CommunityPostComments => Set<CommunityPostComment>();
    public DbSet<CommunityPostReaction> CommunityPostReactions => Set<CommunityPostReaction>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(CommonHallDbContext).Assembly);

        // Global query filter for soft delete
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var falseConstant = System.Linq.Expressions.Expression.Constant(false);
                var comparison = System.Linq.Expressions.Expression.Equal(property, falseConstant);
                var lambda = System.Linq.Expressions.Expression.Lambda(comparison, parameter);

                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        Guid? currentUserId = null;

        if (Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            currentUserId = userId;
        }

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.CreatedBy = currentUserId;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
            }
        }

        // Handle User entity separately since it doesn't inherit from BaseEntity
        foreach (var entry in ChangeTracker.Entries<User>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.CreatedBy = currentUserId;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
