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

public sealed class UpdateFileCommandHandler : IRequestHandler<UpdateFileCommand, StoredFileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;
    private readonly string _baseUrl;

    public UpdateFileCommandHandler(
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

    public async Task<StoredFileDto> Handle(UpdateFileCommand request, CancellationToken cancellationToken)
    {
        var file = await _context.StoredFiles
            .Include(f => f.Collection)
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (file is null)
        {
            throw new NotFoundException("StoredFile", request.Id);
        }

        if (request.AltText != null)
        {
            file.AltText = request.AltText;
        }

        file.UpdatedBy = _currentUser.UserId;
        await _context.SaveChangesAsync(cancellationToken);

        var uploader = await _userManager.FindByIdAsync(file.UploadedBy.ToString());

        return StoredFileDto.FromEntity(file, uploader!, _baseUrl);
    }
}
