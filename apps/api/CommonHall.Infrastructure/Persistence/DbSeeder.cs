using CommonHall.Domain.Entities;
using CommonHall.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Persistence;

public sealed class DbSeeder
{
    private readonly CommonHallDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        CommonHallDbContext context,
        UserManager<User> userManager,
        ILogger<DbSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();

            await SeedSystemGroupsAsync();
            await SeedDefaultSpaceAsync();
            await SeedSuperAdminUserAsync();
            await SeedDefaultNewsChannelAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedSystemGroupsAsync()
    {
        var systemGroups = new[]
        {
            new { Name = "All Users", Description = "All registered users in the system" },
            new { Name = "Editors", Description = "Users with content editing permissions" },
            new { Name = "Admins", Description = "System administrators" }
        };

        foreach (var group in systemGroups)
        {
            if (!await _context.UserGroups.AnyAsync(g => g.Name == group.Name && g.IsSystem))
            {
                var userGroup = new UserGroup
                {
                    Name = group.Name,
                    Description = group.Description,
                    Type = GroupType.System,
                    IsSystem = true
                };

                _context.UserGroups.Add(userGroup);
                _logger.LogInformation("Created system group: {GroupName}", group.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDefaultSpaceAsync()
    {
        if (!await _context.Spaces.AnyAsync(s => s.IsDefault))
        {
            var globalSpace = new Space
            {
                Name = "Global",
                Slug = "global",
                Description = "The default global space for all users",
                IsDefault = true,
                SortOrder = 0
            };

            _context.Spaces.Add(globalSpace);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created default Global space");
        }
    }

    private async Task SeedSuperAdminUserAsync()
    {
        const string adminEmail = "admin@commonhall.local";
        const string adminPassword = "Admin123!";

        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "System Administrator",
                FirstName = "System",
                LastName = "Administrator",
                Role = UserRole.SuperAdmin,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                // Add to Admins group
                var adminsGroup = await _context.UserGroups
                    .FirstOrDefaultAsync(g => g.Name == "Admins" && g.IsSystem);

                if (adminsGroup is not null)
                {
                    _context.UserGroupMemberships.Add(new UserGroupMembership
                    {
                        UserId = adminUser.Id,
                        UserGroupId = adminsGroup.Id,
                        JoinedAt = DateTimeOffset.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created super admin user: {Email}", adminEmail);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create super admin user: {Errors}", errors);
            }
        }
    }

    private async Task SeedDefaultNewsChannelAsync()
    {
        var globalSpace = await _context.Spaces.FirstOrDefaultAsync(s => s.IsDefault);
        if (globalSpace is null) return;

        if (!await _context.NewsChannels.AnyAsync(c => c.SpaceId == globalSpace.Id && c.Slug == "general"))
        {
            var generalChannel = new NewsChannel
            {
                SpaceId = globalSpace.Id,
                Name = "General",
                Slug = "general",
                Description = "General news and announcements",
                Color = "#3B82F6",
                SortOrder = 0
            };

            _context.NewsChannels.Add(generalChannel);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created General news channel in Global space");
        }
    }
}
