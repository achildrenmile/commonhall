using CommonHall.Application.DTOs;
using CommonHall.Application.Features.News.Tags.Queries;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.News.Tags.Handlers;

public sealed class SearchTagsQueryHandler : IRequestHandler<SearchTagsQuery, List<TagDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchTagsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TagDto>> Handle(SearchTagsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tags.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(searchLower));
        }

        var tags = await query
            .OrderBy(t => t.Name)
            .Take(request.Limit)
            .Select(t => new
            {
                Tag = t,
                ArticleCount = t.ArticleTags.Count
            })
            .ToListAsync(cancellationToken);

        return tags.Select(x => TagDto.FromEntity(x.Tag, x.ArticleCount)).ToList();
    }
}
