using CommonHall.Application.Features.Pages.Commands;
using CommonHall.Application.Features.Pages.Handlers;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using CommonHall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CommonHall.Tests.Unit.Handlers;

public class PageVersioningTests
{
    private readonly CommonHallDbContext _context;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<UserManager<User>> _userManagerMock;

    public PageVersioningTests()
    {
        var options = new DbContextOptionsBuilder<CommonHallDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CommonHallDbContext(options);

        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task UpdatePageCommand_WithContentChange_ShouldCreateVersion()
    {
        // Arrange
        var space = new Space { Id = Guid.NewGuid(), Name = "Test Space", Slug = "test-space" };
        var page = new Page
        {
            Id = Guid.NewGuid(),
            SpaceId = space.Id,
            Space = space,
            Title = "Test Page",
            Slug = "test-page",
            Content = "[\"original content\"]",
            Status = ContentStatus.Draft
        };

        _context.Spaces.Add(space);
        _context.Pages.Add(page);
        await _context.SaveChangesAsync();

        var handler = new UpdatePageCommandHandler(_context, _currentUserMock.Object, _userManagerMock.Object);

        var command = new UpdatePageCommand
        {
            Id = page.Id,
            Content = "[\"updated content\"]"
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var versions = await _context.PageVersions.Where(v => v.PageId == page.Id).ToListAsync();
        versions.Should().HaveCount(1);
        versions[0].Content.Should().Be("[\"original content\"]");
        versions[0].VersionNumber.Should().Be(1);
        versions[0].ChangeDescription.Should().Be("Auto-saved before update");
    }

    [Fact]
    public async Task UpdatePageCommand_WithSameContent_ShouldNotCreateVersion()
    {
        // Arrange
        var space = new Space { Id = Guid.NewGuid(), Name = "Test Space", Slug = "test-space" };
        var page = new Page
        {
            Id = Guid.NewGuid(),
            SpaceId = space.Id,
            Space = space,
            Title = "Test Page",
            Slug = "test-page",
            Content = "[\"original content\"]",
            Status = ContentStatus.Draft
        };

        _context.Spaces.Add(space);
        _context.Pages.Add(page);
        await _context.SaveChangesAsync();

        var handler = new UpdatePageCommandHandler(_context, _currentUserMock.Object, _userManagerMock.Object);

        var command = new UpdatePageCommand
        {
            Id = page.Id,
            Title = "Updated Title" // Only title change, no content change
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var versions = await _context.PageVersions.Where(v => v.PageId == page.Id).ToListAsync();
        versions.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdatePageCommand_WithMultipleContentChanges_ShouldIncrementVersionNumber()
    {
        // Arrange
        var space = new Space { Id = Guid.NewGuid(), Name = "Test Space", Slug = "test-space" };
        var page = new Page
        {
            Id = Guid.NewGuid(),
            SpaceId = space.Id,
            Space = space,
            Title = "Test Page",
            Slug = "test-page",
            Content = "[\"v1\"]",
            Status = ContentStatus.Draft
        };

        _context.Spaces.Add(space);
        _context.Pages.Add(page);
        await _context.SaveChangesAsync();

        var handler = new UpdatePageCommandHandler(_context, _currentUserMock.Object, _userManagerMock.Object);

        // Act - First update
        await handler.Handle(new UpdatePageCommand { Id = page.Id, Content = "[\"v2\"]" }, CancellationToken.None);

        // Act - Second update
        await handler.Handle(new UpdatePageCommand { Id = page.Id, Content = "[\"v3\"]" }, CancellationToken.None);

        // Assert
        var versions = await _context.PageVersions
            .Where(v => v.PageId == page.Id)
            .OrderBy(v => v.VersionNumber)
            .ToListAsync();

        versions.Should().HaveCount(2);
        versions[0].VersionNumber.Should().Be(1);
        versions[0].Content.Should().Be("[\"v1\"]");
        versions[1].VersionNumber.Should().Be(2);
        versions[1].Content.Should().Be("[\"v2\"]");
    }

    [Fact]
    public async Task RestorePageVersionCommand_ShouldCreateBackupAndRestoreContent()
    {
        // Arrange
        var space = new Space { Id = Guid.NewGuid(), Name = "Test Space", Slug = "test-space" };
        var page = new Page
        {
            Id = Guid.NewGuid(),
            SpaceId = space.Id,
            Space = space,
            Title = "Test Page",
            Slug = "test-page",
            Content = "[\"current content\"]",
            Status = ContentStatus.Draft
        };

        var oldVersion = new PageVersion
        {
            Id = Guid.NewGuid(),
            PageId = page.Id,
            Content = "[\"old content\"]",
            VersionNumber = 1,
            ChangeDescription = "Original"
        };

        _context.Spaces.Add(space);
        _context.Pages.Add(page);
        _context.PageVersions.Add(oldVersion);
        await _context.SaveChangesAsync();

        var handler = new RestorePageVersionCommandHandler(_context, _currentUserMock.Object, _userManagerMock.Object);

        var command = new RestorePageVersionCommand
        {
            PageId = page.Id,
            VersionId = oldVersion.Id
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Content.Should().Be("[\"old content\"]");

        var versions = await _context.PageVersions
            .Where(v => v.PageId == page.Id)
            .OrderBy(v => v.VersionNumber)
            .ToListAsync();

        versions.Should().HaveCount(2);
        versions[1].VersionNumber.Should().Be(2);
        versions[1].Content.Should().Be("[\"current content\"]");
        versions[1].ChangeDescription.Should().Contain("restoring version 1");
    }
}
