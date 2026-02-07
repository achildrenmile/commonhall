using CommonHall.Application.Common;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using CommonHall.Infrastructure.Persistence;
using CommonHall.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace CommonHall.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly CommonHallDbContext _context;
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var options = new DbContextOptionsBuilder<CommonHallDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CommonHallDbContext(options);

        var configValues = new Dictionary<string, string?>
        {
            { "Jwt:Secret", "YourSuperSecretKeyForDevelopment12345!MustBeAtLeast32Characters" },
            { "Jwt:Issuer", "CommonHall" },
            { "Jwt:Audience", "CommonHallUsers" },
            { "Jwt:AccessTokenExpirationMinutes", "15" },
            { "Jwt:RefreshTokenExpirationDays", "7" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _authService = new AuthService(_userManagerMock.Object, _context, _configuration);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserAndReturnTokens()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123!";
        var displayName = "Test User";

        var createdUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = displayName,
            Role = UserRole.User,
            IsActive = true
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<User, string>((user, _) =>
            {
                user.Id = createdUser.Id;
            });

        // Act
        var result = await _authService.RegisterAsync(email, password, displayName);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be(email);
        result.User.DisplayName.Should().Be(displayName);

        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), password), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowConflictException()
    {
        // Arrange
        var email = "existing@example.com";
        var existingUser = new User { Id = Guid.NewGuid(), Email = email };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(existingUser);

        // Act
        var act = async () => await _authService.RegisterAsync(email, "Password123!", "Display Name");

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*email*already*");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = "Test User",
            Role = UserRole.User,
            IsActive = true
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldThrowAuthenticationException()
    {
        // Arrange
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _authService.LoginAsync("nonexistent@example.com", "Password123!");

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .Where(e => e.Code == "INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowAuthenticationException()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            IsActive = true
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _authService.LoginAsync(email, "WrongPassword!");

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .Where(e => e.Code == "INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ShouldThrowAuthenticationException()
    {
        // Arrange
        var email = "inactive@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            IsActive = false
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _authService.LoginAsync(email, "Password123!");

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .Where(e => e.Code == "ACCOUNT_DISABLED");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            DisplayName = "Test User",
            Role = UserRole.User,
            IsActive = true
        };

        var refreshTokenValue = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshTokenValue);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(refreshTokenValue);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldThrowAuthenticationException()
    {
        // Arrange
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-8)
        };

        await _context.RefreshTokens.AddAsync(expiredToken);
        await _context.SaveChangesAsync();

        // Act
        var act = async () => await _authService.RefreshTokenAsync(expiredToken.Token);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .Where(e => e.Code == "INVALID_TOKEN");
    }

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldRevokeToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshTokenValue = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        // Act
        await _authService.RevokeTokenAsync(refreshTokenValue);

        // Assert
        var revokedToken = await _context.RefreshTokens.FindAsync(refreshToken.Id);
        revokedToken.Should().NotBeNull();
        revokedToken!.RevokedAt.Should().NotBeNull();
        revokedToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeAllActiveTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var tokens = new List<RefreshToken>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        await _context.RefreshTokens.AddRangeAsync(tokens);
        await _context.SaveChangesAsync();

        // Act
        await _authService.RevokeAllUserTokensAsync(userId);

        // Assert
        var userTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();

        userTokens.Should().AllSatisfy(t =>
        {
            t.RevokedAt.Should().NotBeNull();
            t.IsRevoked.Should().BeTrue();
        });
    }
}
