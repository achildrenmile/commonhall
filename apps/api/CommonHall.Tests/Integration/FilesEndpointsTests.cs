using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using CommonHall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CommonHall.Tests.Integration;

public class FilesEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public FilesEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = await userManager.FindByEmailAsync("filetest@test.com");
        if (user == null)
        {
            user = new User
            {
                Email = "filetest@test.com",
                UserName = "filetest@test.com",
                FirstName = "File",
                LastName = "Tester",
                Role = UserRole.Admin,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Password123!");
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "filetest@test.com",
            Password = "Password123!"
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        return loginResult!.Data!.AccessToken;
    }

    [Fact]
    public async Task UploadFile_WithValidFile_ShouldSucceed()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Test file content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");

        // Act
        var response = await _client.PostAsync("/api/v1/files", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.OriginalName.Should().Be("test.txt");
        result.Data.MimeType.Should().Be("text/plain");
        result.Data.Url.Should().Contain("/api/v1/files/");
    }

    [Fact]
    public async Task UploadFile_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Test"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");

        // Act
        var response = await _client.PostAsync("/api/v1/files", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadFile_WithDangerousExtension_ShouldFail()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("malicious content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "file", "malware.exe");

        // Act
        var response = await _client.PostAsync("/api/v1/files", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFileMetadata_WithExistingFile_ShouldReturnMetadata()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Upload a file first
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Metadata test content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "metadata-test.txt");

        var uploadResponse = await _client.PostAsync("/api/v1/files", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        var fileId = uploadResult!.Data!.Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/files/{fileId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        result.Should().NotBeNull();
        result!.Data!.Id.Should().Be(fileId);
        result.Data.OriginalName.Should().Be("metadata-test.txt");
    }

    [Fact]
    public async Task DownloadFile_ShouldStreamContent()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var originalContent = "Download test content - with special chars: äöü";
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(originalContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "download-test.txt");

        var uploadResponse = await _client.PostAsync("/api/v1/files", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        var fileId = uploadResult!.Data!.Id;

        // Remove auth for anonymous download
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync($"/api/v1/files/{fileId}/download");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");

        var downloadedContent = await response.Content.ReadAsStringAsync();
        downloadedContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task DownloadFile_WithETag_ShouldReturn304WhenNotModified()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("ETag test content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "etag-test.txt");

        var uploadResponse = await _client.PostAsync("/api/v1/files", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        var fileId = uploadResult!.Data!.Id;

        _client.DefaultRequestHeaders.Authorization = null;

        // First request to get ETag
        var firstResponse = await _client.GetAsync($"/api/v1/files/{fileId}/download");
        var etag = firstResponse.Headers.ETag?.Tag;
        etag.Should().NotBeNullOrEmpty();

        // Act - Second request with If-None-Match
        _client.DefaultRequestHeaders.IfNoneMatch.Clear();
        _client.DefaultRequestHeaders.IfNoneMatch.Add(new EntityTagHeaderValue(etag!));
        var secondResponse = await _client.GetAsync($"/api/v1/files/{fileId}/download");

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task UpdateFile_ShouldUpdateAltText()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Alt text test"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "alttext-test.txt");

        var uploadResponse = await _client.PostAsync("/api/v1/files", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        var fileId = uploadResult!.Data!.Id;

        // Act
        var updateRequest = new { AltText = "New alt text description" };
        var response = await _client.PutAsJsonAsync($"/api/v1/files/{fileId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        result!.Data!.AltText.Should().Be("New alt text description");
    }

    [Fact]
    public async Task DeleteFile_ShouldRemoveFile()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Delete test"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "delete-test.txt");

        var uploadResponse = await _client.PostAsync("/api/v1/files", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        var fileId = uploadResult!.Data!.Id;

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/v1/files/{fileId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify file is gone
        var getResponse = await _client.GetAsync($"/api/v1/files/{fileId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListFiles_ShouldReturnPaginatedResults()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Upload a few files
        for (int i = 0; i < 3; i++)
        {
            var uploadContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes($"List test {i}"));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
            uploadContent.Add(fileContent, "file", $"list-test-{Guid.NewGuid()}.txt");
            await _client.PostAsync("/api/v1/files", uploadContent);
        }

        // Act
        var response = await _client.GetAsync("/api/v1/files?size=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<StoredFileDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeLessOrEqualTo(2);
    }

    [Fact]
    public async Task CreateCollection_ShouldSucceed()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Name = "Test Collection " + Guid.NewGuid(),
            Description = "A test collection"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/collections", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<FileCollectionDto>>();
        result!.Data!.Name.Should().Be(request.Name);
        result.Data.Description.Should().Be(request.Description);
        result.Data.FileCount.Should().Be(0);
    }

    [Fact]
    public async Task UploadFileToCollection_ShouldAssociateFile()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create collection
        var collectionRequest = new { Name = "Upload Collection " + Guid.NewGuid() };
        var collectionResponse = await _client.PostAsJsonAsync("/api/v1/collections", collectionRequest);
        var collectionResult = await collectionResponse.Content.ReadFromJsonAsync<ApiResponse<FileCollectionDto>>();
        var collectionId = collectionResult!.Data!.Id;

        // Upload file to collection
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Collection file content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "collection-file.txt");

        // Act
        var response = await _client.PostAsync($"/api/v1/files?collectionId={collectionId}", uploadContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        result!.Data!.CollectionId.Should().Be(collectionId);
        result.Data.CollectionName.Should().Be(collectionRequest.Name);
    }

    [Fact]
    public async Task MoveFileToCollection_ShouldChangeAssociation()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create collection
        var collectionRequest = new { Name = "Move Target Collection " + Guid.NewGuid() };
        var collectionResponse = await _client.PostAsJsonAsync("/api/v1/collections", collectionRequest);
        var collectionResult = await collectionResponse.Content.ReadFromJsonAsync<ApiResponse<FileCollectionDto>>();
        var collectionId = collectionResult!.Data!.Id;

        // Upload file without collection
        var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Move file content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        uploadContent.Add(fileContent, "file", "move-file.txt");

        var uploadResponse = await _client.PostAsync("/api/v1/files", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        var fileId = uploadResult!.Data!.Id;

        // Act - Move file to collection
        var moveRequest = new { CollectionId = collectionId };
        var response = await _client.PostAsJsonAsync($"/api/v1/files/{fileId}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StoredFileDto>>();
        result!.Data!.CollectionId.Should().Be(collectionId);
    }
}
