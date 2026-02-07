using CommonHall.Infrastructure.Services;
using FluentAssertions;

namespace CommonHall.Tests.Unit.Services;

public class FileValidationServiceTests
{
    private readonly FileValidationService _service;

    public FileValidationServiceTests()
    {
        _service = new FileValidationService();
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("image/png", true)]
    [InlineData("image/gif", true)]
    [InlineData("image/webp", true)]
    [InlineData("application/pdf", true)]
    [InlineData("video/mp4", true)]
    [InlineData("audio/mpeg", true)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", true)]
    [InlineData("application/x-executable", false)]
    [InlineData("application/x-msdownload", false)]
    [InlineData("text/html", false)]
    public void IsAllowedMimeType_ShouldValidateCorrectly(string mimeType, bool expected)
    {
        // Act
        var result = _service.IsAllowedMimeType(mimeType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("document.exe", true)]
    [InlineData("script.bat", true)]
    [InlineData("file.ps1", true)]
    [InlineData("code.js", true)]
    [InlineData("page.php", true)]
    [InlineData("script.sh", true)]
    [InlineData("image.jpg", false)]
    [InlineData("document.pdf", false)]
    [InlineData("video.mp4", false)]
    [InlineData("archive.zip", false)]
    public void IsDangerousExtension_ShouldIdentifyDangerousFiles(string fileName, bool expected)
    {
        // Act
        var result = _service.IsDangerousExtension(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(1024, true)]
    [InlineData(50 * 1024 * 1024, true)] // 50MB - exactly at limit
    [InlineData(50 * 1024 * 1024 + 1, false)] // 50MB + 1 byte - over limit
    [InlineData(100 * 1024 * 1024, false)] // 100MB
    public void ValidateFileSizeBytes_ShouldEnforceLimit(long sizeBytes, bool expected)
    {
        // Act
        var result = _service.ValidateFileSizeBytes(sizeBytes);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task DetectMimeTypeAsync_WithJpeg_ShouldDetectCorrectly()
    {
        // Arrange - JPEG magic bytes
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01 };
        using var stream = new MemoryStream(jpegBytes);

        // Act
        var result = await _service.DetectMimeTypeAsync(stream);

        // Assert
        result.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task DetectMimeTypeAsync_WithPng_ShouldDetectCorrectly()
    {
        // Arrange - PNG magic bytes
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 };
        using var stream = new MemoryStream(pngBytes);

        // Act
        var result = await _service.DetectMimeTypeAsync(stream);

        // Assert
        result.Should().Be("image/png");
    }

    [Fact]
    public async Task DetectMimeTypeAsync_WithPdf_ShouldDetectCorrectly()
    {
        // Arrange - PDF magic bytes
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A, 0x0A };
        using var stream = new MemoryStream(pdfBytes);

        // Act
        var result = await _service.DetectMimeTypeAsync(stream);

        // Assert
        result.Should().Be("application/pdf");
    }

    [Fact]
    public async Task DetectMimeTypeAsync_WithGif_ShouldDetectCorrectly()
    {
        // Arrange - GIF magic bytes (GIF89a)
        var gifBytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00, 0xFF, 0xFF, 0xFF };
        using var stream = new MemoryStream(gifBytes);

        // Act
        var result = await _service.DetectMimeTypeAsync(stream);

        // Assert
        result.Should().Be("image/gif");
    }

    [Fact]
    public async Task DetectMimeTypeAsync_WithMp3_ShouldDetectCorrectly()
    {
        // Arrange - MP3 with ID3 header
        var mp3Bytes = new byte[] { 0x49, 0x44, 0x33, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(mp3Bytes);

        // Act
        var result = await _service.DetectMimeTypeAsync(stream);

        // Assert
        result.Should().Be("audio/mpeg");
    }

    [Fact]
    public async Task DetectMimeTypeAsync_WithUnknownContent_ShouldReturnNull()
    {
        // Arrange - Random bytes that don't match any signature
        var randomBytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        using var stream = new MemoryStream(randomBytes);

        // Act
        var result = await _service.DetectMimeTypeAsync(stream);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DetectMimeTypeAsync_ShouldNotModifyStreamPosition()
    {
        // Arrange
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 };
        using var stream = new MemoryStream(pngBytes);
        stream.Position = 5; // Set position to middle

        // Act
        await _service.DetectMimeTypeAsync(stream);

        // Assert
        stream.Position.Should().Be(5); // Position should be restored
    }
}
