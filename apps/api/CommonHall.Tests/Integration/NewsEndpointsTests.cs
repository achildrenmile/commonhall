using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using CommonHall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CommonHall.Tests.Integration;

public class NewsEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public NewsEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var context = scope.ServiceProvider.GetRequiredService<CommonHallDbContext>();

        // Create admin user if not exists
        var user = await userManager.FindByEmailAsync("admin@test.com");
        if (user == null)
        {
            user = new User
            {
                Email = "admin@test.com",
                UserName = "admin@test.com",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRole.Admin,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Password123!");
        }

        // Login
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "admin@test.com",
            Password = "Password123!"
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        return loginResult!.Data!.AccessToken;
    }

    private async Task<Guid> EnsureSpaceExistsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommonHallDbContext>();

        var space = context.Spaces.FirstOrDefault(s => s.Slug == "test-space");
        if (space == null)
        {
            space = new Space { Name = "Test Space", Slug = "test-space" };
            context.Spaces.Add(space);
            await context.SaveChangesAsync();
        }

        return space.Id;
    }

    [Fact]
    public async Task GetNewsFeed_WithoutAuth_ShouldReturnPublishedArticles()
    {
        // Arrange
        var spaceId = await EnsureSpaceExistsAsync();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommonHallDbContext>();

        var article = new NewsArticle
        {
            SpaceId = spaceId,
            Title = "Published Article",
            Slug = "published-article-" + Guid.NewGuid(),
            Content = "Content",
            Status = ArticleStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow,
            AuthorId = Guid.NewGuid()
        };
        context.NewsArticles.Add(article);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/news");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<NewsArticleListDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateNewsArticle_WithAuth_ShouldCreateArticle()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var spaceId = await EnsureSpaceExistsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            SpaceId = spaceId,
            Title = "New Article " + Guid.NewGuid(),
            Content = "Article content here",
            Tags = new[] { "tech", "news" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/news", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<NewsArticleDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be(request.Title);
        result.Data.Status.Should().Be(ArticleStatus.Draft);
    }

    [Fact]
    public async Task CreateNewsChannel_WithAuth_ShouldCreateChannel()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var spaceId = await EnsureSpaceExistsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            SpaceId = spaceId,
            Name = "Test Channel " + Guid.NewGuid(),
            Description = "Test channel description",
            Color = "#FF5733"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/news-channels", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<NewsChannelDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(request.Name);
        result.Data.Color.Should().Be(request.Color);
    }

    [Fact]
    public async Task PublishArticle_WithAuth_ShouldPublish()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var spaceId = await EnsureSpaceExistsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create article first
        var createRequest = new
        {
            SpaceId = spaceId,
            Title = "Article to Publish " + Guid.NewGuid(),
            Content = "Content"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/news", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<NewsArticleDto>>();
        var articleId = createResult!.Data!.Id;

        // Act
        var response = await _client.PostAsync($"/api/v1/news/{articleId}/publish", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify article is published
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommonHallDbContext>();
        var article = await context.NewsArticles.FindAsync(articleId);
        article.Should().NotBeNull();
        article!.Status.Should().Be(ArticleStatus.Published);
        article.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ToggleReaction_WithAuth_ShouldToggle()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var spaceId = await EnsureSpaceExistsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create and publish article
        var createRequest = new
        {
            SpaceId = spaceId,
            Title = "Article for Reactions " + Guid.NewGuid(),
            Content = "Content"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/news", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<NewsArticleDto>>();
        var articleId = createResult!.Data!.Id;

        await _client.PostAsync($"/api/v1/news/{articleId}/publish", null);

        // Act - Toggle reaction on
        var toggleResponse = await _client.PostAsJsonAsync($"/api/v1/news/{articleId}/reactions", new { Type = 0 });

        // Assert
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggleResult = await toggleResponse.Content.ReadFromJsonAsync<ApiResponse<ToggleReactionResultDto>>();
        toggleResult.Should().NotBeNull();
        toggleResult!.Data.Should().NotBeNull();
        toggleResult.Data!.IsReacted.Should().BeTrue();
        toggleResult.Data.TotalCount.Should().Be(1);

        // Act - Toggle reaction off
        var toggleOffResponse = await _client.PostAsJsonAsync($"/api/v1/news/{articleId}/reactions", new { Type = 0 });
        var toggleOffResult = await toggleOffResponse.Content.ReadFromJsonAsync<ApiResponse<ToggleReactionResultDto>>();
        toggleOffResult!.Data!.IsReacted.Should().BeFalse();
        toggleOffResult.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task AddComment_WithAuth_ShouldAddComment()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        var spaceId = await EnsureSpaceExistsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create and publish article with comments enabled
        var createRequest = new
        {
            SpaceId = spaceId,
            Title = "Article for Comments " + Guid.NewGuid(),
            Content = "Content",
            AllowComments = true
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/news", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<NewsArticleDto>>();
        var articleId = createResult!.Data!.Id;

        await _client.PostAsync($"/api/v1/news/{articleId}/publish", null);

        // Act
        var commentRequest = new { Body = "This is a test comment" };
        var response = await _client.PostAsJsonAsync($"/api/v1/news/{articleId}/comments", commentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CommentDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Body.Should().Be("This is a test comment");
    }

    [Fact]
    public async Task GetComments_WithCursor_ShouldPaginate()
    {
        // Arrange
        var spaceId = await EnsureSpaceExistsAsync();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommonHallDbContext>();

        var article = new NewsArticle
        {
            SpaceId = spaceId,
            Title = "Article with Comments",
            Slug = "article-with-comments-" + Guid.NewGuid(),
            Content = "Content",
            Status = ArticleStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow,
            AuthorId = Guid.NewGuid(),
            AllowComments = true
        };
        context.NewsArticles.Add(article);
        await context.SaveChangesAsync();

        // Add multiple comments
        for (int i = 0; i < 5; i++)
        {
            context.Comments.Add(new Comment
            {
                NewsArticleId = article.Id,
                AuthorId = Guid.NewGuid(),
                Body = $"Comment {i}",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/news/{article.Id}/comments?size=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CommentDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(2);
        result.Meta.Should().NotBeNull();
        result.Meta!.HasMore.Should().BeTrue();
        result.Meta.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SearchTags_ShouldReturnMatchingTags()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommonHallDbContext>();

        context.Tags.AddRange(
            new Tag { Name = "Technology", Slug = "technology" },
            new Tag { Name = "Tech News", Slug = "tech-news" },
            new Tag { Name = "Sports", Slug = "sports" }
        );
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/news/tags?search=tech");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TagDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Should().Contain(t => t.Slug == "technology");
        result.Data!.Should().Contain(t => t.Slug == "tech-news");
        result.Data!.Should().NotContain(t => t.Slug == "sports");
    }
}

public record AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}
