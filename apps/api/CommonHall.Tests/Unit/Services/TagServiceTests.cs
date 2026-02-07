using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Infrastructure.Persistence;
using CommonHall.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Tests.Unit.Services;

public class TagServiceTests
{
    private readonly CommonHallDbContext _context;
    private readonly ITagService _tagService;

    public TagServiceTests()
    {
        var options = new DbContextOptionsBuilder<CommonHallDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CommonHallDbContext(options);
        _tagService = new TagService(_context);
    }

    [Fact]
    public async Task GetOrCreateTagsAsync_WithNewTags_ShouldCreateTags()
    {
        // Arrange
        var tagNames = new List<string> { "Technology", "News", "Updates" };

        // Act
        var result = await _tagService.GetOrCreateTagsAsync(tagNames);

        // Assert
        result.Should().HaveCount(3);
        result.Select(t => t.Name).Should().BeEquivalentTo(tagNames);
        result.Select(t => t.Slug).Should().BeEquivalentTo(["technology", "news", "updates"]);
    }

    [Fact]
    public async Task GetOrCreateTagsAsync_WithExistingTags_ShouldReturnExisting()
    {
        // Arrange
        var existingTag = new Tag { Name = "Technology", Slug = "technology" };
        _context.Tags.Add(existingTag);
        await _context.SaveChangesAsync();

        var tagNames = new List<string> { "Technology", "News" };

        // Act
        var result = await _tagService.GetOrCreateTagsAsync(tagNames);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Id == existingTag.Id);

        var allTags = await _context.Tags.ToListAsync();
        allTags.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrCreateTagsAsync_WithDuplicates_ShouldDeduplicate()
    {
        // Arrange
        var tagNames = new List<string> { "Technology", "TECHNOLOGY", "technology" };

        // Act
        var result = await _tagService.GetOrCreateTagsAsync(tagNames);

        // Assert
        result.Should().HaveCount(1);
        result.First().Slug.Should().Be("technology");
    }

    [Fact]
    public async Task GetOrCreateTagsAsync_WithEmptyList_ShouldReturnEmpty()
    {
        // Act
        var result = await _tagService.GetOrCreateTagsAsync([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrCreateTagsAsync_WithWhitespace_ShouldIgnore()
    {
        // Arrange
        var tagNames = new List<string> { "  Valid Tag  ", "", "  ", "Another" };

        // Act
        var result = await _tagService.GetOrCreateTagsAsync(tagNames);

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.Name).Should().BeEquivalentTo(["Valid Tag", "Another"]);
    }

    [Fact]
    public async Task SyncTagsAsync_WithNewArticle_ShouldCreateAssociations()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var tagNames = new List<string> { "Tech", "News" };

        // Act
        var result = await _tagService.SyncTagsAsync(articleId, tagNames);

        // Assert
        result.Should().HaveCount(2);

        var articleTags = await _context.ArticleTags.Where(at => at.NewsArticleId == articleId).ToListAsync();
        articleTags.Should().HaveCount(2);
    }

    [Fact]
    public async Task SyncTagsAsync_WithUpdatedTags_ShouldAddAndRemove()
    {
        // Arrange
        var articleId = Guid.NewGuid();

        // Initial sync
        await _tagService.SyncTagsAsync(articleId, ["Tag1", "Tag2", "Tag3"]);

        // Act - Update to remove Tag1, keep Tag2, add Tag4
        var result = await _tagService.SyncTagsAsync(articleId, ["Tag2", "Tag4"]);

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.Slug).Should().BeEquivalentTo(["tag2", "tag4"]);

        var articleTags = await _context.ArticleTags
            .Where(at => at.NewsArticleId == articleId)
            .Include(at => at.Tag)
            .ToListAsync();

        articleTags.Should().HaveCount(2);
        articleTags.Select(at => at.Tag.Slug).Should().BeEquivalentTo(["tag2", "tag4"]);
    }

    [Fact]
    public async Task SyncTagsAsync_WithEmptyList_ShouldRemoveAllAssociations()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        await _tagService.SyncTagsAsync(articleId, ["Tag1", "Tag2"]);

        // Act
        var result = await _tagService.SyncTagsAsync(articleId, []);

        // Assert
        result.Should().BeEmpty();

        var articleTags = await _context.ArticleTags.Where(at => at.NewsArticleId == articleId).ToListAsync();
        articleTags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrCreateTagsAsync_WithSpecialCharacters_ShouldGenerateValidSlug()
    {
        // Arrange
        var tagNames = new List<string> { "C# Programming", ".NET Core", "AI & ML" };

        // Act
        var result = await _tagService.GetOrCreateTagsAsync(tagNames);

        // Assert
        result.Should().HaveCount(3);
        result.Select(t => t.Slug).Should().BeEquivalentTo(["c-programming", "net-core", "ai-ml"]);
    }
}
