using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using FluentAssertions;

namespace CommonHall.Tests.Integration;

public class SpacesEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SpacesEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var email = $"admin_{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = "Password123!",
            DisplayName = "Admin User"
        });

        var result = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        return result!.Data!.AccessToken;
    }

    [Fact]
    public async Task GetSpaces_WithoutAuth_ShouldReturnSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/spaces");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SpaceDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSpace_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            Name = "Test Space",
            Description = "A test space"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/spaces", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateSpace_WithAdminAuth_ShouldReturnCreated()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var request = new
        {
            Name = $"Test Space {Guid.NewGuid()}",
            Description = "A test space"
        };

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/spaces");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request);
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SpaceDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(request.Name);
        result.Data.Slug.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateSpace_WithInvalidName_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var request = new
        {
            Name = "A", // Too short
            Description = "A test space"
        };

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/spaces");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request);
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSpaceBySlug_WhenExists_ShouldReturnSpace()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var spaceName = $"Unique Space {Guid.NewGuid()}";

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/spaces");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createRequest.Content = JsonContent.Create(new { Name = spaceName });
        var createResponse = await _client.SendAsync(createRequest);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SpaceDto>>();
        var slug = createResult!.Data!.Slug;

        // Act
        var response = await _client.GetAsync($"/api/v1/spaces/{slug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SpaceDetailDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task GetSpaceBySlug_WhenNotExists_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/spaces/nonexistent-space");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSpace_AsAdmin_ShouldReturnUpdatedSpace()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var spaceName = $"Space To Update {Guid.NewGuid()}";

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/spaces");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createRequest.Content = JsonContent.Create(new { Name = spaceName });
        var createResponse = await _client.SendAsync(createRequest);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SpaceDto>>();
        var spaceId = createResult!.Data!.Id;

        // Act
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/spaces/{spaceId}");
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        updateRequest.Content = JsonContent.Create(new { Name = "Updated Name", Description = "Updated description" });
        var updateResponse = await _client.SendAsync(updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<SpaceDto>>();
        result!.Data!.Name.Should().Be("Updated Name");
        result.Data.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteSpace_AsAdmin_ShouldReturnNoContent()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var spaceName = $"Space To Delete {Guid.NewGuid()}";

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/spaces");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createRequest.Content = JsonContent.Create(new { Name = spaceName });
        var createResponse = await _client.SendAsync(createRequest);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SpaceDto>>();
        var spaceId = createResult!.Data!.Id;

        // Act
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/spaces/{spaceId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteResponse = await _client.SendAsync(deleteRequest);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
