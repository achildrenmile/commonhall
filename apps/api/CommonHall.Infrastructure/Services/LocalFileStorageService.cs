using CommonHall.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CommonHall.Infrastructure.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;
    private static readonly HashSet<string> ImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp", "image/tiff"
    };

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _basePath = configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _logger = logger;

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string mimeType, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        // Organize by year/month
        var relativePath = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"), uniqueFileName);
        var fullPath = Path.Combine(_basePath, relativePath);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Copy stream to file
        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await stream.CopyToAsync(fileStream, cancellationToken);
        var sizeBytes = fileStream.Length;

        _logger.LogInformation("File uploaded: {Path}, Size: {Size} bytes", relativePath, sizeBytes);

        return new FileUploadResult
        {
            StoragePath = relativePath.Replace('\\', '/'),
            SizeBytes = sizeBytes
        };
    }

    public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found for deletion: {Path}", storagePath);
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted: {Path}", storagePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Path}", storagePath);
            return Task.FromResult(false);
        }
    }

    public Task<Stream?> GetStreamAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {Path}", storagePath);
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult<Stream?>(stream);
    }

    public async Task<string?> GenerateThumbnailAsync(string storagePath, int width = 200, int height = 200, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Source file not found for thumbnail: {Path}", storagePath);
            return null;
        }

        // Check if this is an image
        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        if (!IsImageExtension(extension))
        {
            return null;
        }

        try
        {
            // Generate thumbnail path
            var directory = Path.GetDirectoryName(storagePath) ?? "";
            var fileName = Path.GetFileNameWithoutExtension(storagePath);
            var thumbnailFileName = $"{fileName}_thumb{extension}";
            var thumbnailRelativePath = Path.Combine(directory, thumbnailFileName).Replace('\\', '/');
            var thumbnailFullPath = GetFullPath(thumbnailRelativePath);

            using var image = await Image.LoadAsync(fullPath, cancellationToken);

            // Calculate resize dimensions maintaining aspect ratio
            var ratioX = (double)width / image.Width;
            var ratioY = (double)height / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            image.Mutate(x => x.Resize(newWidth, newHeight));
            await image.SaveAsync(thumbnailFullPath, cancellationToken);

            _logger.LogInformation("Thumbnail generated: {Path}", thumbnailRelativePath);
            return thumbnailRelativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for: {Path}", storagePath);
            return null;
        }
    }

    public string GetFullPath(string storagePath)
    {
        // Normalize path separators
        storagePath = storagePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_basePath, storagePath);
    }

    private static bool IsImageExtension(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".tiff" => true,
            _ => false
        };
    }
}
