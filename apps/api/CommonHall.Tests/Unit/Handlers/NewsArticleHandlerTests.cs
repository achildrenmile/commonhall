using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Articles.Commands;
using CommonHall.Application.Features.News.Articles.Handlers;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using CommonHall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CommonHall.Tests.Unit.Handlers;

public class NewsArticleHandlerTests
{
    private readonly CommonHallDbContext _context;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ISlugService> _slugServiceMock;
    private readonly Mock<ITagService> _tagServiceMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _spaceId = Guid.NewGuid();

    public NewsArticleHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CommonHallDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CommonHallDbContext(options);

        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.UserId).Returns(_currentUserId);
        _currentUserMock.Setup(x => x.Role).Returns(UserRole.Admin);

        _slugServiceMock = new Mock<ISlugService>();
        _slugServiceMock.Setup(x => x.GenerateSlug(It.IsAny<string>())).Returns((string s) => s.ToLower().Replace(" ", "-"));

        _tagServiceMock = new Mock<ITagService>();
        _tagServiceMock.Setup(x => x.GetOrCreateTagsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string> names, CancellationToken _) =>
                names.Select(n => new Tag { Name = n, Slug = n.ToLower() }).ToList());

        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Seed space
        _context.Spaces.Add(new Space { Id = _spaceId, Name = "Test Space", Slug = "test-space" });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateNewsArticleCommandHandler_WithGhostAuthor_ShouldSetGhostAuthorId()
    {
        // Arrange
        var ghostAuthorId = Guid.NewGuid();
        var ghostAuthor = new User { Id = ghostAuthorId, Email = "ghost@test.com", UserName = "ghostuser" };
        _userManagerMock.Setup(x => x.FindByIdAsync(ghostAuthorId.ToString())).ReturnsAsync(ghostAuthor);
        _userManagerMock.Setup(x => x.FindByIdAsync(_currentUserId.ToString())).ReturnsAsync(new User { Id = _currentUserId, Email = "current@test.com" });

        var handler = new CreateNewsArticleCommandHandler(
            _context,
            _currentUserMock.Object,
            _slugServiceMock.Object,
            _tagServiceMock.Object,
            _userManagerMock.Object);

        var command = new CreateNewsArticleCommand
        {
            SpaceId = _spaceId,
            Title = "Test Article",
            Content = "Test content",
            GhostAuthorId = ghostAuthorId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.GhostAuthorId.Should().Be(ghostAuthorId);
        result.AuthorId.Should().Be(_currentUserId);

        var article = await _context.NewsArticles.FirstOrDefaultAsync(a => a.Id == result.Id);
        article.Should().NotBeNull();
        article!.GhostAuthorId.Should().Be(ghostAuthorId);
        article.AuthorId.Should().Be(_currentUserId);
    }

    [Fact]
    public async Task CreateNewsArticleCommandHandler_WithScheduledAt_ShouldSetScheduledStatus()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByIdAsync(_currentUserId.ToString()))
            .ReturnsAsync(new User { Id = _currentUserId, Email = "current@test.com" });

        var handler = new CreateNewsArticleCommandHandler(
            _context,
            _currentUserMock.Object,
            _slugServiceMock.Object,
            _tagServiceMock.Object,
            _userManagerMock.Object);

        var scheduledTime = DateTimeOffset.UtcNow.AddDays(1);
        var command = new CreateNewsArticleCommand
        {
            SpaceId = _spaceId,
            Title = "Scheduled Article",
            Content = "Content",
            ScheduledAt = scheduledTime
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ArticleStatus.Scheduled);
        result.ScheduledAt.Should().Be(scheduledTime);

        var article = await _context.NewsArticles.FirstOrDefaultAsync(a => a.Id == result.Id);
        article.Should().NotBeNull();
        article!.Status.Should().Be(ArticleStatus.Scheduled);
        article.ScheduledAt.Should().Be(scheduledTime);
    }

    [Fact]
    public async Task CreateNewsArticleCommandHandler_WithoutScheduledAt_ShouldSetDraftStatus()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByIdAsync(_currentUserId.ToString()))
            .ReturnsAsync(new User { Id = _currentUserId, Email = "current@test.com" });

        var handler = new CreateNewsArticleCommandHandler(
            _context,
            _currentUserMock.Object,
            _slugServiceMock.Object,
            _tagServiceMock.Object,
            _userManagerMock.Object);

        var command = new CreateNewsArticleCommand
        {
            SpaceId = _spaceId,
            Title = "Draft Article",
            Content = "Content"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ArticleStatus.Draft);
        result.ScheduledAt.Should().BeNull();
    }

    [Fact]
    public async Task PublishNewsArticleCommandHandler_ShouldSetPublishedStatus()
    {
        // Arrange
        var article = new NewsArticle
        {
            SpaceId = _spaceId,
            Title = "Draft Article",
            Slug = "draft-article",
            Content = "Content",
            Status = ArticleStatus.Draft,
            AuthorId = _currentUserId
        };
        _context.NewsArticles.Add(article);
        await _context.SaveChangesAsync();

        var handler = new PublishNewsArticleCommandHandler(_context, _currentUserMock.Object);
        var command = new PublishNewsArticleCommand(article.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var publishedArticle = await _context.NewsArticles.FindAsync(article.Id);
        publishedArticle.Should().NotBeNull();
        publishedArticle!.Status.Should().Be(ArticleStatus.Published);
        publishedArticle.PublishedAt.Should().NotBeNull();
        publishedArticle.PublishedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ScheduleNewsArticleCommandHandler_ShouldSetScheduledStatus()
    {
        // Arrange
        var article = new NewsArticle
        {
            SpaceId = _spaceId,
            Title = "Draft Article",
            Slug = "draft-article-2",
            Content = "Content",
            Status = ArticleStatus.Draft,
            AuthorId = _currentUserId
        };
        _context.NewsArticles.Add(article);
        await _context.SaveChangesAsync();

        var handler = new ScheduleNewsArticleCommandHandler(_context, _currentUserMock.Object);
        var scheduledTime = DateTimeOffset.UtcNow.AddDays(7);
        var command = new ScheduleNewsArticleCommand
        {
            Id = article.Id,
            ScheduledAt = scheduledTime
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var scheduledArticle = await _context.NewsArticles.FindAsync(article.Id);
        scheduledArticle.Should().NotBeNull();
        scheduledArticle!.Status.Should().Be(ArticleStatus.Scheduled);
        scheduledArticle.ScheduledAt.Should().Be(scheduledTime);
    }
}
