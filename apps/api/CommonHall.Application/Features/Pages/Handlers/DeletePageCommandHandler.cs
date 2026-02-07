using CommonHall.Application.Common;
using CommonHall.Application.Features.Pages.Commands;
using CommonHall.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Features.Pages.Handlers;

public sealed class DeletePageCommandHandler : IRequestHandler<DeletePageCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeletePageCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _context.Pages
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (page is null)
        {
            throw new NotFoundException("Page", request.Id);
        }

        // Soft delete
        page.IsDeleted = true;
        page.DeletedAt = DateTimeOffset.UtcNow;
        page.DeletedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
