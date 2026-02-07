using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using CommonHall.Infrastructure.Persistence;
using CommonHall.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CommonHall.Tests.Unit.Services;

public class ContentAuthorizationServiceTests
{
    private readonly CommonHallDbContext _context;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly ContentAuthorizationService _authorizationService;

    public ContentAuthorizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<CommonHallDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CommonHallDbContext(options);

        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _authorizationService = new ContentAuthorizationService(_context, _userManagerMock.Object);
    }

    [Fact]
    public async Task CanManageSpaceAsync_AdminUser_ShouldReturnTrue()
    {
        // Arrange
        var adminUser = new User { Id = Guid.NewGuid(), Role = UserRole.Admin };
        var spaceId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(adminUser.Id.ToString()))
            .ReturnsAsync(adminUser);

        // Act
        var result = await _authorizationService.CanManageSpaceAsync(adminUser.Id, spaceId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanManageSpaceAsync_SpaceAdmin_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spaceId = Guid.NewGuid();
        var user = new User { Id = userId, Role = UserRole.User };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _context.SpaceAdministrators.Add(new SpaceAdministrator { SpaceId = spaceId, UserId = userId });
        await _context.SaveChangesAsync();

        // Act
        var result = await _authorizationService.CanManageSpaceAsync(userId, spaceId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanManageSpaceAsync_RegularUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spaceId = Guid.NewGuid();
        var user = new User { Id = userId, Role = UserRole.User };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _authorizationService.CanManageSpaceAsync(userId, spaceId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanEditContentAsync_AdminUser_ShouldReturnTrue()
    {
        // Arrange
        var adminUser = new User { Id = Guid.NewGuid(), Role = UserRole.Admin };
        var spaceId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(adminUser.Id.ToString()))
            .ReturnsAsync(adminUser);

        // Act
        var result = await _authorizationService.CanEditContentAsync(adminUser.Id, spaceId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanEditContentAsync_EditorUser_ShouldReturnTrue()
    {
        // Arrange
        var editorUser = new User { Id = Guid.NewGuid(), Role = UserRole.Editor };
        var spaceId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(editorUser.Id.ToString()))
            .ReturnsAsync(editorUser);

        // Act
        var result = await _authorizationService.CanEditContentAsync(editorUser.Id, spaceId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanEditContentAsync_SpaceAdmin_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spaceId = Guid.NewGuid();
        var user = new User { Id = userId, Role = UserRole.User };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _context.SpaceAdministrators.Add(new SpaceAdministrator { SpaceId = spaceId, UserId = userId });
        await _context.SaveChangesAsync();

        // Act
        var result = await _authorizationService.CanEditContentAsync(userId, spaceId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanEditContentAsync_RegularUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spaceId = Guid.NewGuid();
        var user = new User { Id = userId, Role = UserRole.User };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _authorizationService.CanEditContentAsync(userId, spaceId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSpaceAdminAsync_WhenUserIsSpaceAdmin_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spaceId = Guid.NewGuid();

        _context.SpaceAdministrators.Add(new SpaceAdministrator { SpaceId = spaceId, UserId = userId });
        await _context.SaveChangesAsync();

        // Act
        var result = await _authorizationService.IsSpaceAdminAsync(userId, spaceId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSpaceAdminAsync_WhenUserIsNotSpaceAdmin_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spaceId = Guid.NewGuid();

        // Act
        var result = await _authorizationService.IsSpaceAdminAsync(userId, spaceId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsPageCreatorAsync_WhenUserCreatedPage_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pageId = Guid.NewGuid();

        _context.Pages.Add(new Page
        {
            Id = pageId,
            SpaceId = Guid.NewGuid(),
            Title = "Test",
            Slug = "test",
            Content = "[]",
            CreatedBy = userId
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _authorizationService.IsPageCreatorAsync(userId, pageId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsPageCreatorAsync_WhenUserDidNotCreatePage_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var pageId = Guid.NewGuid();

        _context.Pages.Add(new Page
        {
            Id = pageId,
            SpaceId = Guid.NewGuid(),
            Title = "Test",
            Slug = "test",
            Content = "[]",
            CreatedBy = creatorId
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _authorizationService.IsPageCreatorAsync(userId, pageId);

        // Assert
        result.Should().BeFalse();
    }
}
