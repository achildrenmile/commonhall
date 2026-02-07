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

public sealed class ListFilesQueryHandler : IRequestHandler<ListFilesQuery, CursorPaginatedResult<StoredFileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly string _baseUrl;

    public ListFilesQueryHandler(
        IApplicationDbContext context,
        UserManager<User> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _baseUrl = configuration["Application:BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<CursorPaginatedResult<StoredFileDto>> Handle(ListFilesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.StoredFiles
            .Include(f => f.Collection)
            .AsQueryable();

        // Filter by collection
        if (request.CollectionId.HasValue)
        {
            query = query.Where(f => f.CollectionId == request.CollectionId.Value);
        }

        // Filter by search term (original name)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(f => f.OriginalName.ToLower().Contains(searchLower));
        }

        // Filter by MIME type (prefix match for categories like "image/", "video/")
        if (!string.IsNullOrWhiteSpace(request.MimeTypeFilter))
        {
            var filter = request.MimeTypeFilter.ToLower();
            if (filter.EndsWith('/'))
            {
                // Category filter (e.g., "image/")
                query = query.Where(f => f.MimeType.ToLower().StartsWith(filter));
            }
            else
            {
                // Exact MIME type
                query = query.Where(f => f.MimeType.ToLower() == filter);
            }
        }

        // Order by creation date (newest first)
        query = query.OrderByDescending(f => f.CreatedAt);

        // Apply cursor
        if (!string.IsNullOrWhiteSpace(request.Cursor) && Guid.TryParse(request.Cursor, out var cursorId))
        {
            var cursorFile = await _context.StoredFiles
                .Where(f => f.Id == cursorId)
                .Select(f => f.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (cursorFile != default)
            {
                query = query.Where(f => f.CreatedAt < cursorFile);
            }
        }

        var files = await query
            .Take(request.Size + 1)
            .ToListAsync(cancellationToken);

        var dtos = new List<StoredFileDto>();
        var uploaderCache = new Dictionary<Guid, User>();

        foreach (var file in files.Take(request.Size))
        {
            if (!uploaderCache.TryGetValue(file.UploadedBy, out var uploader))
            {
                uploader = await _userManager.FindByIdAsync(file.UploadedBy.ToString());
                if (uploader != null)
                {
                    uploaderCache[file.UploadedBy] = uploader;
                }
            }

            if (uploader != null)
            {
                dtos.Add(StoredFileDto.FromEntity(file, uploader, _baseUrl));
            }
        }

        return CursorPaginatedResult<StoredFileDto>.Create(
            dtos,
            request.Size,
            dto => dto.Id.ToString());
    }
}
