using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
