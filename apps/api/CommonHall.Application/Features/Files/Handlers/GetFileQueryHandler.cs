using CommonHall.Application.Common;
using CommonHall.Application.DTOs;
using CommonHall.Application.Features.Files.Queries;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CommonHall.Application.Features.Files.Handlers;

public sealed class GetFileQueryHandler : IRequestHandler<GetFileQuery, StoredFileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly string _baseUrl;

    public GetFileQueryHandler(
        IApplicationDbContext context,
        UserManager<User> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _baseUrl = configuration["Application:BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<StoredFileDto> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        var file = await _context.StoredFiles
            .Include(f => f.Collection)
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (file is null)
        {
            throw new NotFoundException("StoredFile", request.Id);
        }

        var uploader = await _userManager.FindByIdAsync(file.UploadedBy.ToString());

        return StoredFileDto.FromEntity(file, uploader!, _baseUrl);
    }
}
