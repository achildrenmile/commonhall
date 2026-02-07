using CommonHall.Application.Interfaces;

namespace CommonHall.Infrastructure.Services;

public sealed class FileValidationService : IFileValidationService
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp", "image/tiff", "image/svg+xml",

        // Documents
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "text/csv",

        // Video
        "video/mp4", "video/webm", "video/ogg", "video/quicktime", "video/x-msvideo",

        // Audio
        "audio/mpeg", "audio/wav", "audio/ogg", "audio/webm", "audio/aac", "audio/flac"
    };

    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs", ".vbe", ".js", ".jse",
        ".wsf", ".wsh", ".msc", ".msi", ".msp", ".scr", ".hta", ".cpl",
        ".jar", ".com", ".pif", ".application", ".gadget", ".msp", ".scr",
        ".php", ".asp", ".aspx", ".jsp", ".py", ".rb", ".pl", ".sh", ".bash"
    };

    // Magic bytes for file type detection
    private static readonly Dictionary<byte[], string> MagicBytes = new()
    {
        { new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg" },
        { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png" },
        { new byte[] { 0x47, 0x49, 0x46, 0x38 }, "image/gif" },
        { new byte[] { 0x52, 0x49, 0x46, 0x46 }, "image/webp" }, // RIFF header (also for WAV)
        { new byte[] { 0x42, 0x4D }, "image/bmp" },
        { new byte[] { 0x25, 0x50, 0x44, 0x46 }, "application/pdf" },
        { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "application/zip" }, // Also covers Office 2007+ formats
        { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, "application/msword" }, // OLE compound document
        { new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 }, "video/mp4" },
        { new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }, "video/mp4" },
        { new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, "video/webm" },
        { new byte[] { 0x4F, 0x67, 0x67, 0x53 }, "audio/ogg" },
        { new byte[] { 0x49, 0x44, 0x33 }, "audio/mpeg" }, // ID3 header for MP3
        { new byte[] { 0xFF, 0xFB }, "audio/mpeg" }, // MP3 frame sync
        { new byte[] { 0xFF, 0xFA }, "audio/mpeg" }, // MP3 frame sync
        { new byte[] { 0x66, 0x4C, 0x61, 0x43 }, "audio/flac" }
    };

    public bool IsAllowedMimeType(string mimeType)
    {
        return AllowedMimeTypes.Contains(mimeType);
    }

    public bool IsDangerousExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return DangerousExtensions.Contains(extension);
    }

    public async Task<string?> DetectMimeTypeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (!stream.CanSeek)
        {
            return null;
        }

        var originalPosition = stream.Position;
        var buffer = new byte[16];
        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
        stream.Position = originalPosition;

        if (bytesRead < 2)
        {
            return null;
        }

        foreach (var (magic, mimeType) in MagicBytes)
        {
            if (bytesRead >= magic.Length && buffer.AsSpan(0, magic.Length).SequenceEqual(magic))
            {
                // Special case for RIFF - could be WAV or WEBP
                if (magic.SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 }) && bytesRead >= 12)
                {
                    if (buffer[8] == 0x57 && buffer[9] == 0x41 && buffer[10] == 0x56 && buffer[11] == 0x45)
                    {
                        return "audio/wav";
                    }
                    if (buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                    {
                        return "image/webp";
                    }
                }

                // Special case for ZIP - could be Office document
                if (magic.SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
                {
                    return "application/zip"; // Will need extension to distinguish Office formats
                }

                return mimeType;
            }
        }

        return null;
    }

    public bool ValidateFileSizeBytes(long sizeBytes)
    {
        return sizeBytes > 0 && sizeBytes <= MaxFileSizeBytes;
    }
}
