using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Infrastructure.Persistence;
using CommonHall.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Tests.Unit.Services;

public class SlugServiceTests
{
    private readonly CommonHallDbContext _context;
    private readonly ISlugService _slugService;

    public SlugServiceTests()
    {
        var options = new DbContextOptionsBuilder<CommonHallDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CommonHallDbContext(options);
        _slugService = new SlugService(_context);
    }

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Multiple   Spaces  ", "multiple-spaces")]
    [InlineData("Special!@#$%Characters", "specialcharacters")]
    [InlineData("UPPERCASE", "uppercase")]
    [InlineData("Mixed Case Title", "mixed-case-title")]
    [InlineData("Numbers 123 Here", "numbers-123-here")]
    [InlineData("---Leading-Trailing---", "leading-trailing")]
    [InlineData("Café & Restaurant", "cafe-restaurant")]
    [InlineData("über cool", "uber-cool")]
    public void GenerateSlug_ShouldGenerateCorrectSlug(string input, string expected)
    {
        // Act
        var result = _slugService.GenerateSlug(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GenerateSlug_WithEmptyString_ShouldReturnEmpty()
    {
        // Act
        var result = _slugService.GenerateSlug("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSlug_WithWhitespaceOnly_ShouldReturnEmpty()
    {
        // Act
        var result = _slugService.GenerateSlug("   ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSlug_WithLongTitle_ShouldTruncateTo200Characters()
    {
        // Arrange
        var longTitle = new string('a', 300);

        // Act
        var result = _slugService.GenerateSlug(longTitle);

        // Assert
        result.Length.Should().BeLessOrEqualTo(200);
    }

    [Fact]
    public async Task GenerateUniqueSpaceSlugAsync_WithNoConflict_ShouldReturnBaseSlug()
    {
        // Act
        var result = await _slugService.GenerateUniqueSpaceSlugAsync("My Space");

        // Assert
        result.Should().Be("my-space");
    }

    [Fact]
    public async Task GenerateUniqueSpaceSlugAsync_WithConflict_ShouldAppendNumber()
    {
        // Arrange
        _context.Spaces.Add(new Space { Name = "Test", Slug = "my-space" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _slugService.GenerateUniqueSpaceSlugAsync("My Space");

        // Assert
        result.Should().Be("my-space-2");
    }

    [Fact]
    public async Task GenerateUniqueSpaceSlugAsync_WithMultipleConflicts_ShouldIncrementNumber()
    {
        // Arrange
        _context.Spaces.Add(new Space { Name = "Test 1", Slug = "my-space" });
        _context.Spaces.Add(new Space { Name = "Test 2", Slug = "my-space-2" });
        _context.Spaces.Add(new Space { Name = "Test 3", Slug = "my-space-3" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _slugService.GenerateUniqueSpaceSlugAsync("My Space");

        // Assert
        result.Should().Be("my-space-4");
    }

    [Fact]
    public async Task GenerateUniquePageSlugAsync_WithNoConflict_ShouldReturnBaseSlug()
    {
        // Arrange
        var spaceId = Guid.NewGuid();

        // Act
        var result = await _slugService.GenerateUniquePageSlugAsync(spaceId, "Getting Started");

        // Assert
        result.Should().Be("getting-started");
    }

    [Fact]
    public async Task GenerateUniquePageSlugAsync_WithConflictInSameSpace_ShouldAppendNumber()
    {
        // Arrange
        var spaceId = Guid.NewGuid();
        _context.Pages.Add(new Page { SpaceId = spaceId, Title = "Test", Slug = "getting-started", Content = "[]" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _slugService.GenerateUniquePageSlugAsync(spaceId, "Getting Started");

        // Assert
        result.Should().Be("getting-started-2");
    }

    [Fact]
    public async Task GenerateUniquePageSlugAsync_WithConflictInDifferentSpace_ShouldReturnBaseSlug()
    {
        // Arrange
        var spaceId1 = Guid.NewGuid();
        var spaceId2 = Guid.NewGuid();
        _context.Pages.Add(new Page { SpaceId = spaceId1, Title = "Test", Slug = "getting-started", Content = "[]" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _slugService.GenerateUniquePageSlugAsync(spaceId2, "Getting Started");

        // Assert
        result.Should().Be("getting-started");
    }

    [Fact]
    public async Task GenerateUniqueSpaceSlugAsync_WithEmptyTitle_ShouldReturnDefaultSlug()
    {
        // Act
        var result = await _slugService.GenerateUniqueSpaceSlugAsync("");

        // Assert
        result.Should().Be("space");
    }

    [Fact]
    public async Task GenerateUniquePageSlugAsync_WithEmptyTitle_ShouldReturnDefaultSlug()
    {
        // Act
        var result = await _slugService.GenerateUniquePageSlugAsync(Guid.NewGuid(), "");

        // Assert
        result.Should().Be("page");
    }
}
