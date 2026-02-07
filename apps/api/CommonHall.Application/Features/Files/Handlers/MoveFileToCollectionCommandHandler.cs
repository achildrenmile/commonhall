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

public sealed class MoveFileToCollectionCommandHandler : IRequestHandler<MoveFileToCollectionCommand, StoredFileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;
    private readonly string _baseUrl;

    public MoveFileToCollectionCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        UserManager<User> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
        _baseUrl = configuration["Application:BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<StoredFileDto> Handle(MoveFileToCollectionCommand request, CancellationToken cancellationToken)
    {
        var file = await _context.StoredFiles
            .Include(f => f.Collection)
            .FirstOrDefaultAsync(f => f.Id == request.FileId, cancellationToken);

        if (file is null)
        {
            throw new NotFoundException("StoredFile", request.FileId);
        }

        // Validate new collection if provided
        if (request.CollectionId.HasValue)
        {
            var collection = await _context.FileCollections
                .FirstOrDefaultAsync(c => c.Id == request.CollectionId.Value, cancellationToken);

            if (collection is null)
            {
                throw new NotFoundException("FileCollection", request.CollectionId.Value);
            }

            file.CollectionId = request.CollectionId;
            file.Collection = collection;
        }
        else
        {
            file.CollectionId = null;
            file.Collection = null;
        }

        file.UpdatedBy = _currentUser.UserId;
        await _context.SaveChangesAsync(cancellationToken);

        var uploader = await _userManager.FindByIdAsync(file.UploadedBy.ToString());

        return StoredFileDto.FromEntity(file, uploader!, _baseUrl);
    }
}
