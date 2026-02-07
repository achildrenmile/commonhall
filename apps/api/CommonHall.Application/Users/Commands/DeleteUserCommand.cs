using CommonHall.Application.Common;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CommonHall.Application.Users.Commands;

public sealed record DeleteUserCommand(Guid Id) : IRequest;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IApplicationDbContext _context;

    public DeleteUserCommandHandler(UserManager<User> userManager, IApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user is null)
        {
            throw new NotFoundException("User", request.Id);
        }

        // Soft delete
        user.IsDeleted = true;
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.IsActive = false;

        await _userManager.UpdateAsync(user);
    }
}
