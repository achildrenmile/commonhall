using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CommonHall.Infrastructure.Persistence;

/// <summary>
/// Factory for creating DbContext at design time (migrations, scaffolding).
/// This bypasses the full DI container setup that requires Redis/Elasticsearch.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CommonHallDbContext>
{
    public CommonHallDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=commonhall;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<CommonHallDbContext>();
        optionsBuilder.UseNpgsql(connectionString, b =>
            b.MigrationsAssembly(typeof(CommonHallDbContext).Assembly.FullName));

        // Use a simple stub for design-time
        return new CommonHallDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }
}

/// <summary>
/// Stub implementation of ICurrentUserService for design-time operations.
/// </summary>
internal class DesignTimeCurrentUserService : Application.Interfaces.ICurrentUserService
{
    public Guid? UserId => null;
    public string? Email => null;
    public bool IsAuthenticated => false;
}
