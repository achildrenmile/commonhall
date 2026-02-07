using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Infrastructure.BackgroundServices;
using CommonHall.Infrastructure.Persistence;
using CommonHall.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CommonHall.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<CommonHallDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(CommonHallDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<CommonHallDbContext>());

        // Identity
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<CommonHallDbContext>()
            .AddDefaultTokenProviders();

        // Redis
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConnection));
            services.AddScoped<ICacheService, RedisCacheService>();
        }

        // Seeder
        services.AddScoped<DbSeeder>();

        // Auth Service
        services.AddScoped<IAuthService, AuthService>();

        // Slug Service
        services.AddScoped<ISlugService, SlugService>();

        // Content Authorization Service
        services.AddScoped<IContentAuthorizationService, ContentAuthorizationService>();

        // Tag Service
        services.AddScoped<ITagService, TagService>();

        // View Count Service
        services.AddScoped<IViewCountService, ViewCountService>();

        // File Storage Service
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // File Validation Service
        services.AddScoped<IFileValidationService, FileValidationService>();

        // Background Services
        services.AddHostedService<ScheduledPublishingService>();

        return services;
    }
}
