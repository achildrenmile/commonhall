using CommonHall.Application.Common;
using CommonHall.Application.Features.Files.Commands;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Files.Handlers;

public sealed class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorageService _fileStorage;

    public DeleteFileCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IFileStorageService fileStorage)
    {
        _context = context;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var file = await _context.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (file is null)
        {
            throw new NotFoundException("StoredFile", request.Id);
        }

        // Only owner or admin can delete
        var isOwner = _currentUser.UserId.HasValue && file.UploadedBy == _currentUser.UserId.Value;
        var isAdmin = _currentUser.Role == UserRole.Admin;

        if (!isOwner && !isAdmin)
        {
            throw new ForbiddenException("You can only delete your own files.");
        }

        // Delete physical file
        await _fileStorage.DeleteAsync(file.StoragePath, cancellationToken);

        // Delete thumbnail if exists
        if (!string.IsNullOrEmpty(file.ThumbnailPath))
        {
            await _fileStorage.DeleteAsync(file.ThumbnailPath, cancellationToken);
        }

        // Remove from database
        _context.StoredFiles.Remove(file);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
