using CommonHall.Application.Interfaces;
using CommonHall.Domain.Common;
using CommonHall.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Customize Identity table names
        modelBuilder.Entity<ApplicationUser>(b => b.ToTable("AspNetUsers"));
        modelBuilder.Entity<IdentityRole<Guid>>(b => b.ToTable("AspNetRoles"));
        modelBuilder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("AspNetUserRoles"));
        modelBuilder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("AspNetUserClaims"));
        modelBuilder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("AspNetUserLogins"));
        modelBuilder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("AspNetUserTokens"));
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("AspNetRoleClaims"));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedBy = _currentUserService.UserId;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
