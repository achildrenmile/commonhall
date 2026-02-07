using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using CommonHall.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CommonHall.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly CommonHallDbContext _context;
    private readonly IConfiguration _configuration;

    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public AuthService(
        UserManager<User> userManager,
        CommonHallDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;

        var jwtSection = _configuration.GetSection("Jwt");
        _jwtSecret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        _jwtIssuer = jwtSection["Issuer"] ?? "CommonHall";
        _jwtAudience = jwtSection["Audience"] ?? "CommonHallUsers";
        _accessTokenExpirationMinutes = int.TryParse(jwtSection["AccessTokenExpirationMinutes"], out var atExp) ? atExp : 15;
        _refreshTokenExpirationDays = int.TryParse(jwtSection["RefreshTokenExpirationDays"], out var rtExp) ? rtExp : 7;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string displayName, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            Role = UserRole.Employee,
            IsActive = true,
            EmailConfirmed = true // For now, auto-confirm
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthenticationException("REGISTRATION_FAILED", errors);
        }

        // Add to "All Users" system group
        var allUsersGroup = await _context.UserGroups
            .FirstOrDefaultAsync(g => g.Name == "All Users" && g.IsSystem, cancellationToken);

        if (allUsersGroup is not null)
        {
            _context.UserGroupMemberships.Add(new UserGroupMembership
            {
                UserId = user.Id,
                UserGroupId = allUsersGroup.Id,
                JoinedAt = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        return await GenerateAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new AuthenticationException("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new AuthenticationException("ACCOUNT_DISABLED", "This account has been disabled.");
        }

        if (user.IsDeleted)
        {
            throw new AuthenticationException("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, password);
        if (!isValidPassword)
        {
            throw new AuthenticationException("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        // Update last login
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        return await GenerateAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (storedToken is null)
        {
            throw new AuthenticationException("INVALID_TOKEN", "Invalid refresh token.");
        }

        if (!storedToken.IsActive)
        {
            // Token has been revoked or expired - revoke all tokens for this user (token rotation security)
            await RevokeAllUserTokensAsync(storedToken.UserId, cancellationToken);
            throw new AuthenticationException("INVALID_TOKEN", "Refresh token is no longer valid.");
        }

        var user = storedToken.User;
        if (!user.IsActive || user.IsDeleted)
        {
            throw new AuthenticationException("ACCOUNT_DISABLED", "This account has been disabled.");
        }

        // Revoke current token and generate new one (token rotation)
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        var authResult = await GenerateAuthResultAsync(user, cancellationToken);

        storedToken.ReplacedByToken = authResult.RefreshToken;
        await _context.SaveChangesAsync(cancellationToken);

        return authResult;
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return; // Silently ignore invalid tokens
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResult> GenerateAuthResultAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user, cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_accessTokenExpirationMinutes);

        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = UserDto.FromEntity(user)
        };
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("displayName", user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken)
    {
        var tokenBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var tokenString = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = tokenString,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return tokenString;
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
