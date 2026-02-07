using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using FluentAssertions;

namespace CommonHall.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var request = new
        {
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "Password123!",
            DisplayName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.User.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "invalid-email",
            Password = "Password123!",
            DisplayName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "weak",
            DisplayName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange - First register a user
        var email = $"login_test_{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = password,
            DisplayName = "Test User"
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ShouldReturnUser()
    {
        // Arrange - Register and get token
        var email = $"me_test_{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = "Password123!",
            DisplayName = "Test User"
        });

        var authResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        var token = authResult!.Data!.AccessToken;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be(email);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange - Register and get tokens
        var email = $"refresh_test_{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = "Password123!",
            DisplayName = "Test User"
        });

        var authResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        var refreshToken = authResult!.Data!.RefreshToken;

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            RefreshToken = refreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBe(refreshToken); // Token rotation
    }

    [Fact]
    public async Task Logout_WithValidToken_ShouldRevokeRefreshToken()
    {
        // Arrange - Register and get tokens
        var email = $"logout_test_{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = "Password123!",
            DisplayName = "Test User"
        });

        var authResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        var accessToken = authResult!.Data!.AccessToken;
        var refreshToken = authResult.Data.RefreshToken;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { RefreshToken = refreshToken });
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify refresh token is revoked
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            RefreshToken = refreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
