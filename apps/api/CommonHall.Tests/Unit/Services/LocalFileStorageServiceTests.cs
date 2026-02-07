using CommonHall.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace CommonHall.Tests.Unit.Services;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly LocalFileStorageService _service;
    private readonly string _testBasePath;

    public LocalFileStorageServiceTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "commonhall-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_testBasePath);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:BasePath"] = _testBasePath
            })
            .Build();

        var logger = Mock.Of<ILogger<LocalFileStorageService>>();
        _service = new LocalFileStorageService(configuration, logger);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateFileWithCorrectPath()
    {
        // Arrange
        var content = "Test file content"u8.ToArray();
        using var stream = new MemoryStream(content);

        // Act
        var result = await _service.UploadAsync(stream, "test-file.txt", "text/plain");

        // Assert
        result.Should().NotBeNull();
        result.StoragePath.Should().NotBeNullOrEmpty();
        result.SizeBytes.Should().Be(content.Length);

        // Verify path format: {year}/{month}/{guid}.{ext}
        var parts = result.StoragePath.Split('/');
        parts.Should().HaveCount(3);
        parts[0].Should().Be(DateTime.UtcNow.Year.ToString());
        parts[1].Should().Be(DateTime.UtcNow.Month.ToString("D2"));
        parts[2].Should().EndWith(".txt");

        // Verify file exists
        var fullPath = _service.GetFullPath(result.StoragePath);
        File.Exists(fullPath).Should().BeTrue();
    }

    [Fact]
    public async Task GetStreamAsync_WithExistingFile_ShouldReturnStream()
    {
        // Arrange
        var content = "Test content for reading"u8.ToArray();
        using var uploadStream = new MemoryStream(content);
        var uploadResult = await _service.UploadAsync(uploadStream, "readable.txt", "text/plain");

        // Act
        var stream = await _service.GetStreamAsync(uploadResult.StoragePath);

        // Assert
        stream.Should().NotBeNull();
        using var reader = new StreamReader(stream!);
        var readContent = await reader.ReadToEndAsync();
        readContent.Should().Be("Test content for reading");

        stream.Dispose();
    }

    [Fact]
    public async Task GetStreamAsync_WithNonExistingFile_ShouldReturnNull()
    {
        // Act
        var stream = await _service.GetStreamAsync("non-existing/path/file.txt");

        // Assert
        stream.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingFile_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var content = "File to delete"u8.ToArray();
        using var stream = new MemoryStream(content);
        var uploadResult = await _service.UploadAsync(stream, "deletable.txt", "text/plain");
        var fullPath = _service.GetFullPath(uploadResult.StoragePath);

        // Act
        var result = await _service.DeleteAsync(uploadResult.StoragePath);

        // Assert
        result.Should().BeTrue();
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingFile_ShouldReturnFalse()
    {
        // Act
        var result = await _service.DeleteAsync("non-existing/path/file.txt");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetFullPath_ShouldCombineCorrectly()
    {
        // Arrange
        var storagePath = "2024/01/test-file.txt";

        // Act
        var fullPath = _service.GetFullPath(storagePath);

        // Assert
        fullPath.Should().Be(Path.Combine(_testBasePath, "2024", "01", "test-file.txt"));
    }

    [Fact]
    public void GetFullPath_WithForwardSlashes_ShouldNormalize()
    {
        // Arrange
        var storagePath = "2024/02/another-file.pdf";

        // Act
        var fullPath = _service.GetFullPath(storagePath);

        // Assert
        fullPath.Should().Contain(_testBasePath);
        fullPath.Should().EndWith($"2024{Path.DirectorySeparatorChar}02{Path.DirectorySeparatorChar}another-file.pdf");
    }

    [Fact]
    public async Task UploadAsync_WithDifferentExtensions_ShouldPreserveExtension()
    {
        // Arrange
        var content = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        using var stream = new MemoryStream(content);

        // Act
        var result = await _service.UploadAsync(stream, "image.PNG", "image/png");

        // Assert
        result.StoragePath.Should().EndWith(".png"); // Should be lowercase
    }
}
