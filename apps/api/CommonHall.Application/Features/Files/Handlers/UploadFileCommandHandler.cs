using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Files.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CommonHall.Application.Features.Files.Handlers;

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, StoredFileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IFileValidationService _fileValidation;
    private readonly UserManager<User> _userManager;
    private readonly string _baseUrl;

    public UploadFileCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IFileStorageService fileStorage,
        IFileValidationService fileValidation,
        UserManager<User> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _fileValidation = fileValidation;
        _userManager = userManager;
        _baseUrl = configuration["Application:BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<StoredFileDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AuthenticationException("UNAUTHORIZED", "User is not authenticated.");
        }

        // Validate file extension
        if (_fileValidation.IsDangerousExtension(request.OriginalName))
        {
            throw new ValidationException("File type is not allowed for security reasons.");
        }

        // Validate MIME type
        if (!_fileValidation.IsAllowedMimeType(request.MimeType))
        {
            throw new ValidationException($"MIME type '{request.MimeType}' is not allowed.");
        }

        // Verify MIME type via magic bytes
        var detectedMime = await _fileValidation.DetectMimeTypeAsync(request.Stream, cancellationToken);
        if (detectedMime != null && !_fileValidation.IsAllowedMimeType(detectedMime))
        {
            throw new ValidationException("File content does not match an allowed type.");
        }

        // Validate collection if provided
        if (request.CollectionId.HasValue)
        {
            var collectionExists = await _context.FileCollections
                .AnyAsync(c => c.Id == request.CollectionId.Value, cancellationToken);

            if (!collectionExists)
            {
                throw new NotFoundException("FileCollection", request.CollectionId.Value);
            }
        }

        // Upload file
        var uploadResult = await _fileStorage.UploadAsync(
            request.Stream,
            request.OriginalName,
            request.MimeType,
            cancellationToken);

        // Validate file size
        if (!_fileValidation.ValidateFileSizeBytes(uploadResult.SizeBytes))
        {
            await _fileStorage.DeleteAsync(uploadResult.StoragePath, cancellationToken);
            throw new ValidationException("File size exceeds the maximum allowed size of 50MB.");
        }

        // Generate thumbnail for images
        string? thumbnailPath = null;
        if (request.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            thumbnailPath = await _fileStorage.GenerateThumbnailAsync(uploadResult.StoragePath, 200, 200, cancellationToken);
        }

        // Create stored file record
        var storedFile = new StoredFile
        {
            FileName = Path.GetFileName(uploadResult.StoragePath),
            OriginalName = request.OriginalName,
            MimeType = request.MimeType,
            SizeBytes = uploadResult.SizeBytes,
            StoragePath = uploadResult.StoragePath,
            ThumbnailPath = thumbnailPath,
            CollectionId = request.CollectionId,
            UploadedBy = _currentUser.UserId.Value,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };

        _context.StoredFiles.Add(storedFile);
        await _context.SaveChangesAsync(cancellationToken);

        var user = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString());

        // Load collection if exists
        if (request.CollectionId.HasValue)
        {
            storedFile.Collection = await _context.FileCollections
                .FirstOrDefaultAsync(c => c.Id == request.CollectionId.Value, cancellationToken);
        }

        return StoredFileDto.FromEntity(storedFile, user!, _baseUrl);
    }
}
