using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Domain.Enums;
using FluentAssertions;

namespace CommonHall.Tests.Integration;

public class PagesEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PagesEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid SpaceId, string SpaceSlug)> CreateTestSpaceAsync()
    {
        var email = $"admin_{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = "Password123!",
            DisplayName = "Admin User"
        });

        var authResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        var token = authResult!.Data!.AccessToken;

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/spaces");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createRequest.Content = JsonContent.Create(new { Name = $"Test Space {Guid.NewGuid()}" });
        var createResponse = await _client.SendAsync(createRequest);

        var spaceResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SpaceDto>>();
        return (token, spaceResult!.Data!.Id, spaceResult.Data.Slug);
    }

    [Fact]
    public async Task CreatePage_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var (token, spaceId, _) = await CreateTestSpaceAsync();
        var request = new
        {
            SpaceId = spaceId,
            Title = "Getting Started",
            Content = "[{\"type\":\"paragraph\",\"content\":\"Hello World\"}]",
            Status = ContentStatus.Draft
        };

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pages");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request);
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PageDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Getting Started");
        result.Data.Slug.Should().Be("getting-started");
        result.Data.Status.Should().Be(ContentStatus.Draft);
    }

    [Fact]
    public async Task CreatePage_WithInvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var (token, spaceId, _) = await CreateTestSpaceAsync();
        var request = new
        {
            SpaceId = spaceId,
            Title = "Test Page",
            Content = "not valid json"
        };

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pages");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request);
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPageBySlug_WhenExists_ShouldReturnPage()
    {
        // Arrange
        var (token, spaceId, spaceSlug) = await CreateTestSpaceAsync();
        var createPageRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pages");
        createPageRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createPageRequest.Content = JsonContent.Create(new
        {
            SpaceId = spaceId,
            Title = "My Page",
            Content = "[]"
        });
        var createResponse = await _client.SendAsync(createPageRequest);
        var pageResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PageDto>>();
        var pageSlug = pageResult!.Data!.Slug;

        // Act
        var response = await _client.GetAsync($"/api/v1/pages/{spaceSlug}/{pageSlug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PageDetailDto>>();
        result!.Data!.Title.Should().Be("My Page");
        result.Data.SpaceSlug.Should().Be(spaceSlug);
    }

    [Fact]
    public async Task GetPagesBySpace_ShouldReturnPages()
    {
        // Arrange
        var (token, spaceId, _) = await CreateTestSpaceAsync();

        for (int i = 1; i <= 3; i++)
        {
            var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pages");
            createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            createRequest.Content = JsonContent.Create(new
            {
                SpaceId = spaceId,
                Title = $"Page {i}",
                Content = "[]"
            });
            await _client.SendAsync(createRequest);
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/pages/space/{spaceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PageListDto>>>();
        result!.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task PublishPage_ShouldUpdateStatus()
    {
        // Arrange
        var (token, spaceId, _) = await CreateTestSpaceAsync();
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pages");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createRequest.Content = JsonContent.Create(new
        {
            SpaceId = spaceId,
            Title = "Draft Page",
            Content = "[]",
            Status = ContentStatus.Draft
        });
        var createResponse = await _client.SendAsync(createRequest);
        var pageResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PageDto>>();
        var pageId = pageResult!.Data!.Id;

        // Act
        var publishRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pages/{pageId}/publish");
        publishRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(publishRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PageDto>>();
        result!.Data!.Status.Should().Be(ContentStatus.Published);
        result.Data.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UnpublishPage_ShouldUpdateStatus()
    {
        // Arrange
        var (token, spaceId, _) = await CreateTestSpaceAsync();
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pages");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createRequest.Content = JsonContent.Create(new
        {
            SpaceId = spaceId,
            Title = "Published Page",
            Content = "[]",
            Status = ContentStatus.Published
        });
        var createResponse = await _client.SendAsync(createRequest);
        var pageResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PageDto>>();
        var pageId = pageResult!.Data!.Id;

        // Act
        var unpublishRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pages/{pageId}/unpublish");
        unpublishRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(unpublishRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PageDto>>();
        result!.Data!.Status.Should().Be(ContentStatus.Draft);
    }

    [Fact]
    public async Task UpdatePage_WithContentChange_ShouldCreateVersion()
    {
        // Arrange
        var (token, spaceId, _) = await CreateTestSpaceAsync();
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pages");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createRequest.Content = JsonContent.Create(new
        {
            SpaceId = spaceId,
            Title = "Versioned Page",
            Content = "[\"v1\"]"
        });
        var createResponse = await _client.SendAsync(createRequest);
        var pageResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PageDto>>();
        var pageId = pageResult!.Data!.Id;

        // Act - Update content
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/pages/{pageId}");
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        updateRequest.Content = JsonContent.Create(new { Content = "[\"v2\"]" });
        await _client.SendAsync(updateRequest);

        // Get versions
        var versionsRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/pages/{pageId}/versions");
        versionsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var versionsResponse = await _client.SendAsync(versionsRequest);

        // Assert
        versionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var versionsResult = await versionsResponse.Content.ReadFromJsonAsync<ApiResponse<List<PageVersionDto>>>();
        versionsResult!.Data.Should().HaveCount(1);
        versionsResult.Data![0].VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task SetHomePage_ShouldUpdateHomePageFlag()
    {
        // Arrange
        var (token, spaceId, _) = await CreateTestSpaceAsync();

        // Create two pages
        var createPage1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pages");
        createPage1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createPage1.Content = JsonContent.Create(new { SpaceId = spaceId, Title = "Page 1", Content = "[]" });
        var page1Response = await _client.SendAsync(createPage1);
        var page1 = (await page1Response.Content.ReadFromJsonAsync<ApiResponse<PageDto>>())!.Data!;

        var createPage2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pages");
        createPage2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createPage2.Content = JsonContent.Create(new { SpaceId = spaceId, Title = "Page 2", Content = "[]" });
        var page2Response = await _client.SendAsync(createPage2);
        var page2 = (await page2Response.Content.ReadFromJsonAsync<ApiResponse<PageDto>>())!.Data!;

        // Act - Set page 2 as home page
        var setHomeRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pages/space/{spaceId}/homepage");
        setHomeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        setHomeRequest.Content = JsonContent.Create(new { PageId = page2.Id });
        var response = await _client.SendAsync(setHomeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify pages
        var pagesResponse = await _client.GetAsync($"/api/v1/pages/space/{spaceId}");
        var pages = (await pagesResponse.Content.ReadFromJsonAsync<ApiResponse<List<PageListDto>>>())!.Data!;

        var homePage = pages.FirstOrDefault(p => p.IsHomePage);
        homePage.Should().NotBeNull();
        homePage!.Id.Should().Be(page2.Id);
    }

    [Fact]
    public async Task DeletePage_ShouldReturnNoContent()
    {
        // Arrange
        var (token, spaceId, _) = await CreateTestSpaceAsync();
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pages");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createRequest.Content = JsonContent.Create(new
        {
            SpaceId = spaceId,
            Title = "Page to Delete",
            Content = "[]"
        });
        var createResponse = await _client.SendAsync(createRequest);
        var pageResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PageDto>>();
        var pageId = pageResult!.Data!.Id;

        // Act
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/pages/{pageId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(deleteRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
